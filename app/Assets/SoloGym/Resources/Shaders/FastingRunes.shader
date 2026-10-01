Shader "SoloGym/FastingRunes"
{
 Properties { [PerRendererData] _MainTex ("Sprite", 2D) = "white" {} _Color("Tint", Color)=(1,1,1,1) }
 SubShader
 {
  Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="False"}
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
   struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
   sampler2D _MainTex; fixed4 _Color;
   v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o; }
   fixed4 frag(v2f i):SV_Target
   {
    // Metadata is top-left based; UVs are bottom-left based. Never animate the casing or timer face.
    float radius=length((i.uv-float2(.5,.515625))*256);
    clip(radius-61);clip(82-radius);
    fixed4 c=tex2D(_MainTex,i.uv);
    float l=dot(c.rgb,float3(.2126,.7152,.0722));
    c.rgb=lerp(l.xxx,c.rgb,.22)*.78*i.color.rgb;
    c.a*=i.color.a; return c;
   }
   ENDCG
  }
 }
}
