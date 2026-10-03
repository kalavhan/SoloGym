// Lit pixel sprite for the 3D hall (built-in pipeline). Like Sprites/Diffuse, but:
// - light, including point-light falloff, comes in hard bands (_Steps) so torchlight pools read as pixel art;
// - _Wrap lets light from the side still reach a flat, camera-facing sprite;
// - a light behind the sprite paints only its outermost opaque pixels (a one-texel rim), so the portal
//   outlines the fighters in blue;
// - pixels brighter than _EmitThreshold glow on their own (portal light, gems), regardless of lighting.
Shader "SoloGym/PixelSpriteLit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Steps ("Light steps (0 = smooth)", Range(0,8)) = 4
        _Wrap ("Light wrap", Range(0,1)) = 0.5
        _Rim ("Back-light rim strength", Range(0,4)) = 0
        _EmitThreshold ("Self-lit above brightness", Range(0,1.1)) = 1.1
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting On
        ZWrite Off
        Blend One OneMinusSrcAlpha

        CGPROGRAM
        #pragma surface surf Stepped vertex:vert nofog nolightmap nodynlightmap keepalpha noinstancing
        #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
        #pragma target 3.0
        #include "UnitySprites.cginc"

        float4 _MainTex_TexelSize;
        float _Steps, _Wrap, _Rim, _EmitThreshold;

        struct Input
        {
            float2 uv_MainTex;
            fixed4 color;
        };

        void vert (inout appdata_full v, out Input o)
        {
            v.vertex = UnityFlipSprite(v.vertex, _Flip);
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.color = v.color * _Color * _RendererColor;
        }

        half4 LightingStepped (SurfaceOutput s, half3 lightDir, half atten)
        {
            half d = dot(s.Normal, lightDir);
            // Sprites are double sided: whichever face the camera sees is the front.
            half front = saturate((abs(d) + _Wrap) / (1 + _Wrap));
            half l = front * atten;
            if (_Steps >= 1) l = floor(l * _Steps + 0.5) / _Steps;
            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * l;
            // Rim: light coming from behind (away from the camera) on edge pixels only.
            half3 toCam = normalize(UNITY_MATRIX_V[2].xyz);
            half back = saturate(-dot(lightDir, toCam)) * atten;
            c.rgb += _LightColor0.rgb * s.Gloss * _Rim * step(0.08, back) * s.Alpha;
            c.a = s.Alpha;
            return c;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = SampleSpriteTexture(IN.uv_MainTex) * IN.color;
            o.Albedo = c.rgb * c.a;
            o.Alpha = c.a;
            // Edge mask: opaque pixel next to a transparent one.
            float2 t = _MainTex_TexelSize.xy;
            half n = min(min(SampleSpriteTexture(IN.uv_MainTex + float2(t.x, 0)).a, SampleSpriteTexture(IN.uv_MainTex - float2(t.x, 0)).a),
                         min(SampleSpriteTexture(IN.uv_MainTex + float2(0, t.y)).a, SampleSpriteTexture(IN.uv_MainTex - float2(0, t.y)).a));
            o.Gloss = step(0.5, c.a) * (1 - step(0.5, n));
            half bright = max(c.r, max(c.g, c.b));
            o.Emission = o.Albedo * smoothstep(_EmitThreshold, min(_EmitThreshold + 0.15, 1.2), bright);
        }
        ENDCG
    }
    Fallback "Sprites/Diffuse"
}
