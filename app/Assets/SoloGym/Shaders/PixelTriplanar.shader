// Pixel-art texturing for simple 3D props (boxes, beams, railings) in the summoning hall.
// The texture is projected in world space along the face's main axis, so every surface keeps the same
// pixel density no matter how a primitive is scaled. Use point-filtered, repeat-wrapped textures.
Shader "SoloGym/PixelTriplanar"
{
    Properties
    {
        _MainTex ("Texture (point filter, repeat)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _TileSize ("World units per texture repeat", Float) = 2
        _Offset ("World offset", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        float _TileSize;
        float4 _Offset;

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
