Shader "SoloGym/Illustrated Registered Tint"
{
    Properties
    {
        [PerRendererData] _MainTex ("Registered illustration", 2D) = "white" {}
        _MaskTex ("Registered skin R / hair G", 2D) = "black" {}
        _RegionTex ("Anatomical mesh ownership", 2D) = "white" {}
        _UseRegionMask ("Use anatomical ownership", Float) = 0
        _UnderlapTex ("Same-source shoulder continuation", 2D) = "black" {}
        _UnderlapEnabled ("Show animated shoulder underlap", Float) = 0
        _CellUV ("Registered cell", Vector) = (0,0,1,1)
        _AtlasMask ("Mask covers atlas", Float) = 0
        _SkinSource ("Source skin", Color) = (.63,.43,.32,1)
        _SkinTint ("Target skin", Color) = (.63,.43,.32,1)
        _HairSource ("Source hair", Color) = (.025,.032,.055,1)
        _HairTint ("Target hair", Color) = (.025,.032,.055,1)
        _SkinEnabled ("Recolor skin", Float) = 0
        _HairEnabled ("Recolor hair", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; float4 clipping:TEXCOORD1; };
            sampler2D _MainTex, _MaskTex, _RegionTex, _UnderlapTex;
            float4 _CellUV, _ClipRect;
            float4 _SkinSource, _SkinTint, _HairSource, _HairTint;
            float _AtlasMask, _SkinEnabled, _HairEnabled, _UseRegionMask, _UnderlapEnabled, _UIMaskSoftnessX, _UIMaskSoftnessY;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.clipping = 0;
                #ifdef UNITY_UI_CLIP_RECT
                    float2 pixelSize = o.vertex.w;
                    pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                    float4 rectangle = clamp(_ClipRect, -2e10, 2e10);
                    o.clipping = float4(v.vertex.xy * 2 - rectangle.xy - rectangle.zw,
                        .25 / (.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize)));
                #endif
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float2 localUV = (i.uv - _CellUV.xy) / _CellUV.zw;
                float ownership = max(tex2D(_RegionTex, localUV).a, tex2D(_UnderlapTex, localUV).a * _UnderlapEnabled);
                c.a *= lerp(1, ownership, _UseRegionMask);
                float2 maskUV = lerp(localUV, i.uv, _AtlasMask);
                float2 regions = tex2D(_MaskTex, maskUV).rg;
                // Region authoring is authoritative. The hue guard protects cyan gear, white
                // eyes, black contours and neutral highlights inside broad skin polygons.
                float warmPixel = smoothstep(.005, .045, c.r - c.g) * smoothstep(-.005, .025, c.g - c.b);
                float skinMask = regions.r * warmPixel * _SkinEnabled;
                float sourceLuma = dot(c.rgb, float3(.2126, .7152, .0722));
                float coolHair = 1 - smoothstep(.005, .055, c.r - c.b);
                float hairMask = regions.g * _HairEnabled * coolHair;
                float3 skin = c.rgb * _SkinTint.rgb / max(.02, _SkinSource.rgb);
                // A tonal ramp leaves ink dark and reserves the light end for painted highlights.
                // Direct division by near-black source hair would clip silver to flat white.
                float tone = saturate(sourceLuma / .36);
                float ink = smoothstep(.012, .06, sourceLuma);
                float3 hair = lerp(c.rgb, _HairTint.rgb * (.35 + tone * .9), ink);
                c.rgb = lerp(c.rgb, saturate(skin), skinMask);
                c.rgb = lerp(c.rgb, saturate(hair), hairMask);
                c *= i.color;
                #ifdef UNITY_UI_CLIP_RECT
                    float2 clipping = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.clipping.xy)) * i.clipping.zw);
                    c.a *= clipping.x * clipping.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                    clip(c.a - .001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
