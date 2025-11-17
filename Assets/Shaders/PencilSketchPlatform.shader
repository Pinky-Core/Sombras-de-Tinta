Shader "SombrasDeTinta/PencilSketchPlatform"
    {
        Properties
        {
            _BaseColor ("Base Color", Color) = (0.9,0.9,0.9,1)
            _LineTex1 ("Line Tex 1", 2D) = "white" {}
            _LineTex2 ("Line Tex 2", 2D) = "white" {}
            _NoiseTex ("Noise", 2D) = "gray" {}
            _Tiling ("Line Tiling (lines per meter)", Float) = 6
            _UVOffset ("UV Offset", Vector) = (0.083, 0.083, 0, 0)
            _Speed ("Wiggle Speed", Float) = 0.4
            _Wiggle ("Wiggle Amount", Float) = 0.05
            _FlipSpeed ("Flip Speed (frames/sec)", Float) = 6
            _FrameOffset ("Frame Noise Offset", Float) = 0.37
        _Contrast ("Line Contrast", Float) = 2.0
        _EdgeNoise ("Edge Noise Strength", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Tiling;
                float2 _UVOffset;
                float _Speed;
                float _Wiggle;
                float _FlipSpeed;
                float _FrameOffset;
                float _Contrast;
                float _EdgeNoise;
            CBUFFER_END

            TEXTURE2D(_LineTex1); SAMPLER(sampler_LineTex1);
            TEXTURE2D(_LineTex2); SAMPLER(sampler_LineTex2);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = float2(0,0); // not used for mapping anymore
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 worldUV = input.positionWS.xz * _Tiling + _UVOffset.xy;

                float timeWiggle = _Time.y * _Speed;
                float timeFlip = _Time.y * _FlipSpeed;

                // Dos estados de wiggle para simular flipbook de 2 frames
                float2 noiseUVA = worldUV * 0.8 + timeWiggle;
                float2 noiseUVB = worldUV * 0.8 + timeWiggle + _FrameOffset;
                float2 wiggleA = (SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUVA).rg - 0.5f) * _Wiggle;
                float2 wiggleB = (SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUVB).rg - 0.5f) * _Wiggle;

                float2 uv1A = worldUV + wiggleA + float2(timeWiggle * 0.1f, 0);
                float2 uv2A = worldUV + wiggleA + float2(0, timeWiggle * 0.1f);

                float2 uv1B = worldUV + wiggleB + float2(timeWiggle * -0.12f, 0.04f);
                float2 uv2B = worldUV + wiggleB + float2(0.04f, timeWiggle * -0.12f);

                float l1A = SAMPLE_TEXTURE2D(_LineTex1, sampler_LineTex1, uv1A).r;
                float l2A = SAMPLE_TEXTURE2D(_LineTex2, sampler_LineTex2, uv2A).r;
                float linesA = pow(saturate(l1A * l2A), _Contrast);

                float l1B = SAMPLE_TEXTURE2D(_LineTex1, sampler_LineTex1, uv1B).r;
                float l2B = SAMPLE_TEXTURE2D(_LineTex2, sampler_LineTex2, uv2B).r;
                float linesB = pow(saturate(l1B * l2B), _Contrast);

                // Flip de 2 frames
                float frame = step(0.0f, sin(timeFlip * 6.283185f));
                float lines = lerp(linesA, linesB, frame);

                // Borde algo irregular para que no se vea perfecto
                float edgeMask = lerp(1.0f, SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, worldUV * 0.5f).r, _EdgeNoise);
                lines *= edgeMask;

                float3 color = _BaseColor.rgb * lines;
                return float4(color, 1.0f);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
