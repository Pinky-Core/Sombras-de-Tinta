Shader "SombrasDeTinta/WorldSpaceInk"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _Color("Tint Color", Color) = (1,1,1,1)
        _TextureScale("Texture World Scale", Float) = 0.5
        _Smoothness("Smoothness", Range(0,1)) = 0.2
        _Metallic("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 300

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_fwdbase
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            sampler2D _NormalMap;
            float _NormalStrength;
            float _TextureScale;
            float _Smoothness;
            float _Metallic;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            float3 SampleTriplanar(float3 worldPos, float3 normal, sampler2D tex, float scale)
            {
                float3 blending = pow(abs(normal), 4);
                blending /= (blending.x + blending.y + blending.z + 1e-5f);

                float2 xUV = worldPos.yz * scale;
                float2 yUV = worldPos.xz * scale;
                float2 zUV = worldPos.xy * scale;

                float3 xTex = tex2D(tex, xUV).rgb;
                float3 yTex = tex2D(tex, yUV).rgb;
                float3 zTex = tex2D(tex, zUV).rgb;

                return xTex * blending.x + yTex * blending.y + zTex * blending.z;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float scale = max(0.0001f, _TextureScale);
                float3 sampled = SampleTriplanar(i.worldPos, i.worldNormal, _MainTex, scale);
                float4 albedo = float4(sampled, 1) * _Color;

                float3 n = normalize(i.worldNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = saturate(dot(n, lightDir));
                float3 diffuse = albedo.rgb * NdotL;

                float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * albedo.rgb;
                float3 color = diffuse + ambient;
                UNITY_APPLY_FOG(i.fogCoord, color);
                return float4(color, albedo.a);
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
