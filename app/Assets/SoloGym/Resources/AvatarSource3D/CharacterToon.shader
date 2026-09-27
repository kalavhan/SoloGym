Shader "SoloGym/CharacterSourceToon"
{
    Properties
    {
        _Color ("Source material color", Color) = (1,1,1,1)
        _Contour ("Fine silhouette shading", Range(0,1)) = .5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        Pass
        {
            // The interchange proof supports both winding conventions while its exporter is reviewed.
            // Depth testing still determines equipment/body and near/far occlusion.
            Cull Off
            ZWrite On
            ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _Contour;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f
            {
                float4 position : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 worldPosition : TEXCOORD1;
            };
            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.normal = UnityObjectToWorldNormal(input.normal);
                output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                return output;
            }
            fixed4 frag(v2f input) : SV_Target
            {
                float3 normal = normalize(input.normal);
                float3 view = normalize(_WorldSpaceCameraPos.xyz - input.worldPosition);
                float diffuse = dot(normal, normalize(float3(-.42, .72, .68)));
                float lowBand = smoothstep(-.12, .03, diffuse);
                float highBand = smoothstep(.52, .63, diffuse);
                float light = .52 + lowBand * .30 + highBand * .18;
                float rim = pow(saturate(1 - dot(normal, view)), 3.5) * .13;
                float3 shadowTint = lerp(float3(.78, .85, 1), float3(1, .99, .96), saturate(diffuse));
                float3 color = _Color.rgb * light * shadowTint;
                color += float3(.31, .56, .67) * rim;
                float contour = 1 - smoothstep(.015, .10, saturate(dot(normal, view)));
                color *= 1 - contour * _Contour * .3;
                return fixed4(color, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
