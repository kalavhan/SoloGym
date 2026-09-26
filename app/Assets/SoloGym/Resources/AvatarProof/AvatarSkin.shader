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
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex; float4 _PartUV,_SkinBounds; fixed4 _SkinTint;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv);float2 p=(i.uv-_PartUV.xy)/_PartUV.zw;
                float mask=step(_SkinBounds.x,p.x)*step(_SkinBounds.y,p.y)*step(p.x,_SkinBounds.z)*step(p.y,_SkinBounds.w);
                c.rgb=lerp(c.rgb,c.rgb*_SkinTint.rgb,mask);return c*i.color;
            }
            ENDCG
        }
    }
}
