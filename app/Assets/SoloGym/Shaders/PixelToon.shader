// Sprite-like look for imported 3D props: lighting in hard steps (no smooth gradients) and a dark outline,
// to sit next to the PixelLab characters. Pair with a small, point-filtered, palette-reduced texture.
Shader "SoloGym/PixelToon"
{
    Properties
    {
        _MainTex ("Texture (small, point filter)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Steps ("Light steps", Range(1,6)) = 3
        _Shadow ("Shadow brightness", Range(0,1)) = 0.45
        _OutlineColor ("Outline colour", Color) = (0.07,0.05,0.1,1)
        _OutlineWidth ("Outline width (object units)", Float) = 0.004
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _OutlineWidth;
            fixed4 _OutlineColor;
            struct v2f { float4 pos : SV_POSITION; };
            v2f vert (appdata_base v)
            {
                v2f o;
                float3 p = v.vertex.xyz + normalize(v.normal) * _OutlineWidth;
                o.pos = UnityObjectToClipPos(float4(p, 1));
                return o;
            }
            fixed4 frag (v2f i) : SV_Target { return _OutlineColor; }
            ENDCG
        }

        CGPROGRAM
        #pragma surface surf Steps fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        float _Steps;
        float _Shadow;

        struct Input { float2 uv_MainTex; };

        half4 LightingSteps (SurfaceOutput s, half3 lightDir, half atten)
        {
            half nl = saturate(dot(s.Normal, lightDir)) * atten;
            half q = floor(nl * _Steps + 0.5) / _Steps;
        #ifdef UNITY_PASS_FORWARDADD
            half l = q;
        #else
            half l = lerp(_Shadow, 1, q);
        #endif
            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * l;
            c.a = s.Alpha;
            return c;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
