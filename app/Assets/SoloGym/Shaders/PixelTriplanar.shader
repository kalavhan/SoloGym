// Pixel-art texturing for simple 3D props (boxes, beams, railings) in the summoning hall.
// The texture is projected in world space along the face's main axis, so every surface keeps the same
// pixel density no matter how a primitive is scaled. Use point-filtered, repeat-wrapped textures.
// Lighting is quantised into _Steps bands so torch pools read as stepped pixel-art shading.
Shader "SoloGym/PixelTriplanar"
{
    Properties
    {
        _MainTex ("Texture (point filter, repeat)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _TileSize ("World units per texture repeat", Float) = 2
        _Offset ("World offset", Vector) = (0,0,0,0)
        _Steps ("Light steps (0 = smooth)", Range(0,8)) = 4
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Stepped fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        float _TileSize;
        float4 _Offset;
        float _Steps;

        // Lambert, but light (including point-light falloff) comes in hard bands: pixel-art pools of torchlight.
        half4 LightingStepped (SurfaceOutput s, half3 lightDir, half atten)
        {
            half l = saturate(dot(s.Normal, lightDir)) * atten;
            if (_Steps >= 1) l = floor(l * _Steps + 0.5) / _Steps;
            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * l;
            c.a = s.Alpha;
            return c;
        }

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            float3 p = (IN.worldPos + _Offset.xyz) / max(_TileSize, 0.001);
            float3 n = abs(IN.worldNormal);
            // Hard axis choice (no blending) keeps the pixels crisp.
            float2 uv = (n.x >= n.y && n.x >= n.z) ? p.zy : ((n.y >= n.z) ? p.xz : p.xy);
            fixed4 c = tex2D(_MainTex, uv) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
