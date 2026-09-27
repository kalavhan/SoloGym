Shader "SoloGym/Avatar Skin Region"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _SkinTint ("Skin palette", Color) = (1,0.75,0.55,1)
        _PartUV ("Atlas part rectangle", Vector) = (0,0,1,1)
        _SkinBounds ("Skin region in part UV", Vector) = (0,0,1,1)
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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
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
            struct v2f
            {
                float4 vertex:SV_POSITION;
                float2 uv:TEXCOORD0;
                fixed4 color:COLOR;
                float4 clippingMask:TEXCOORD1;
            };
            sampler2D _MainTex; float4 _PartUV,_SkinBounds; fixed4 _SkinTint;
            float4 _ClipRect;
            float _UIMaskSoftnessX, _UIMaskSoftnessY;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv;
                o.color=v.color;
                o.clippingMask=0;
                #ifdef UNITY_UI_CLIP_RECT
                    // Match Unity's canvas clipping coordinates and pixel-based mask softness.
                    float2 pixelSize=o.vertex.w;
                    pixelSize/=abs(mul((float2x2)UNITY_MATRIX_P,_ScreenParams.xy));
                    float4 clampedRect=clamp(_ClipRect,-2e10,2e10);
                    o.clippingMask=float4(v.vertex.xy*2-clampedRect.xy-clampedRect.zw,
                        0.25/(0.25*float2(_UIMaskSoftnessX,_UIMaskSoftnessY)+abs(pixelSize)));
                #endif
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv);float2 p=(i.uv-_PartUV.xy)/_PartUV.zw;
                float mask=step(_SkinBounds.x,p.x)*step(_SkinBounds.y,p.y)*step(p.x,_SkinBounds.z)*step(p.y,_SkinBounds.w);
                c.rgb=lerp(c.rgb,c.rgb*_SkinTint.rgb,mask);
                c*=i.color;
                #ifdef UNITY_UI_CLIP_RECT
                    float2 clipping=saturate((_ClipRect.zw-_ClipRect.xy-abs(i.clippingMask.xy))*i.clippingMask.zw);
                    c.a*=clipping.x*clipping.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                    clip(c.a-0.001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
