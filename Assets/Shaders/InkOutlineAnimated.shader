Shader "SombrasDeTinta/InkOutlineAnimated"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0,0,0,1)
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth("Outline Width", Range(0.0, 0.03)) = 0.012

        _SketchNoise("Sketch Noise", 2D) = "white" {}
        _NoiseTiling("Noise Tiling", Float) = 3.0
        _NoiseSpeed("Noise Speed", Float) = 1.2
        _ShadeSteps("Shade Steps", Range(1, 8)) = 4
        _LightIntensity("Light Intensity", Float) = 1.0

        _WobbleAmplitude("Outline Wobble Amplitude", Float) = 0.004
        _WobbleFrequency("Outline Wobble Frequency", Float) = 6.0
        _SurfaceWiggle("Surface Wiggle Strength", Float) = 0.06
        _SurfaceSpeed("Surface Wiggle Speed", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 350

        Pass
        {
            Name "AnimatedOutline"
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
            float _WobbleAmplitude;
            float _WobbleFrequency;

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
                float wobble = sin(_Time.y * _WobbleFrequency + v.vertex.x + v.vertex.z) * _WobbleAmplitude;
                float3 offset = normalize(v.normal) * (_OutlineWidth + wobble);
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
            Name "AnimatedCel"
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
            float4 _BaseColor;
            float _NoiseTiling;
            float _NoiseSpeed;
            float _ShadeSteps;
            float _LightIntensity;
            float _SurfaceWiggle;
            float _SurfaceSpeed;

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
                float2 noiseUV = (v.uv + _Time.y * _SurfaceSpeed) * _NoiseTiling;
                float wiggle = (tex2Dlod(_SketchNoise, float4(noiseUV, 0, 0)).r * 2 - 1) * _SurfaceWiggle;
                float3 displaced = v.vertex.xyz + v.normal * wiggle;

                o.pos = UnityObjectToClipPos(float4(displaced, 1));
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, float4(displaced, 1)).xyz;
                o.uv = TRANSFORM_TEX(v.uv, _SketchNoise);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
#if defined(UNITY_LIGHTMODEL_AMBIENT)
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
#else
                float3 lightDir = normalize(float3(0, 1, 0));
#endif
                float NdotL = saturate(dot(normal, lightDir)) * _LightIntensity;
                float steps = max(1, _ShadeSteps - 1);
                float stepped = floor(NdotL * steps) / steps;

                float2 animatedUV = i.uv * _NoiseTiling + _Time.y * _NoiseSpeed;
                float noise = tex2D(_SketchNoise, animatedUV).r;
                float modulated = lerp(0.7, 1.3, noise);
                float3 col = _BaseColor.rgb * stepped * modulated;

                UNITY_APPLY_FOG(i.fogCoord, col);
                return float4(col, _BaseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack "Diffuse"
}
