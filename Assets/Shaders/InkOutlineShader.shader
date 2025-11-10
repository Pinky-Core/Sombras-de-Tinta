Shader "SombrasDeTinta/InkOutline"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0,0,0,1)
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth("Outline Width", Range(0.0,0.02)) = 0.01
        _SketchNoise("Sketch Noise", 2D) = "white" {}
        _NoiseTiling("Noise Tiling", Float) = 2.0
        _NoiseSpeed("Noise Speed", Float) = 1.0
        _ShadeSteps("Shade Steps", Range(1,6)) = 3
        _LightIntensity("Light Intensity", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _OutlineWidth;
            float4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 norm = normalize(v.normal);
                float3 offset = norm * _OutlineWidth;
                o.pos = UnityObjectToClipPos(v.vertex + float4(offset, 0));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "CelShaded"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            sampler2D _SketchNoise;
            float4 _SketchNoise_ST;
            float _NoiseTiling;
            float _NoiseSpeed;
            float _ShadeSteps;
            float4 _BaseColor;
            float _LightIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float2 uv : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = TRANSFORM_TEX(v.uv, _SketchNoise);
                o.pos = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = saturate(dot(normal, lightDir)) * _LightIntensity;

                float stepped = floor(NdotL * (_ShadeSteps - 1)) / max(1, (_ShadeSteps - 1));
                float2 noiseUV = i.uv * _NoiseTiling + _Time.y * _NoiseSpeed;
                float noise = tex2D(_SketchNoise, noiseUV).r;
                noise = lerp(0.8, 1.2, noise);
                fixed3 color = _BaseColor.rgb * stepped * noise;
                UNITY_APPLY_FOG(i.fogCoord, color);
                return fixed4(color, _BaseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack "Diffuse"
}
