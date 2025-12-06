Shader "SombrasDeTinta/PencilSketchSprite"
{
    Properties
    {
        [MainColor] _BaseColor ("Base Color", Color) = (1,1,1,1)
        [PerRendererData][MainTexture] _SpriteA ("Sprite A", 2D) = "white" {}
        [PerRendererData] _SpriteB ("Sprite B", 2D) = "white" {}
        _SpriteFlipSpeed ("Sprite Flip Speed (frames/sec)", Float) = 6
        _LineTex1 ("Line Tex 1 (optional sprite/texture)", 2D) = "white" {}
        _LineTex2 ("Line Tex 2 (optional sprite/texture)", 2D) = "white" {}
        _NoiseTex ("Noise", 2D) = "gray" {}
        _Tiling ("Line Tiling", Float) = 1
        _Speed ("Wiggle Speed", Float) = 0.4
        _Wiggle ("Wiggle Amount", Float) = 0.05
        _FlipSpeed ("Flip Speed (frames/sec)", Float) = 6
        _FrameOffset ("Frame Noise Offset", Float) = 0.37
        _Contrast ("Line Contrast", Float) = 2.0
        _EdgeNoise ("Edge Noise Strength", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

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
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
                float2 uv2        : TEXCOORD2;
                float4 color      : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Tiling;
                float _Speed;
                float _Wiggle;
                float _FlipSpeed;
                float _FrameOffset;
                float _Contrast;
                float _EdgeNoise;
            CBUFFER_END

            TEXTURE2D(_SpriteA); SAMPLER(sampler_SpriteA);
            TEXTURE2D(_SpriteB); SAMPLER(sampler_SpriteB);
            TEXTURE2D(_LineTex1); SAMPLER(sampler_LineTex1);
            TEXTURE2D(_LineTex2); SAMPLER(sampler_LineTex2);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            float4 _SpriteA_ST;
            float4 _SpriteB_ST;
            float4 _LineTex1_ST;
            float4 _LineTex2_ST;

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _SpriteA);
                o.uv1 = TRANSFORM_TEX(input.uv, _LineTex1) * _Tiling;
                o.uv2 = TRANSFORM_TEX(input.uv, _LineTex2) * _Tiling;
                o.color = input.color;
                return o;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 uvMain = input.uv;
                float2 uvBase1 = input.uv1;
                float2 uvBase2 = input.uv2;

                float timeWiggle = _Time.y * _Speed;
                float timeFlip = _Time.y * _FlipSpeed;
                float timeSpriteFlip = _Time.y * _SpriteFlipSpeed;

                float2 noiseUVA = uvBase1 * 0.8 + timeWiggle;
                float2 noiseUVB = uvBase1 * 0.8 + timeWiggle + _FrameOffset;
                float2 wiggleA = (SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUVA).rg - 0.5f) * _Wiggle;
                float2 wiggleB = (SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUVB).rg - 0.5f) * _Wiggle;

                float2 uv1A = uvBase1 + wiggleA + float2(timeWiggle * 0.1f, 0);
                float2 uv2A = uvBase2 + wiggleA + float2(0, timeWiggle * 0.1f);

                float2 uv1B = uvBase1 + wiggleB + float2(timeWiggle * -0.12f, 0.04f);
                float2 uv2B = uvBase2 + wiggleB + float2(0.04f, timeWiggle * -0.12f);

                float4 tex1A = SAMPLE_TEXTURE2D(_LineTex1, sampler_LineTex1, uv1A);
                float4 tex2A = SAMPLE_TEXTURE2D(_LineTex2, sampler_LineTex2, uv2A);
                float linesA = pow(saturate(tex1A.r * tex2A.r), _Contrast);

                float4 tex1B = SAMPLE_TEXTURE2D(_LineTex1, sampler_LineTex1, uv1B);
                float4 tex2B = SAMPLE_TEXTURE2D(_LineTex2, sampler_LineTex2, uv2B);
                float linesB = pow(saturate(tex1B.r * tex2B.r), _Contrast);

                float frame = step(0.0f, sin(timeFlip * 6.283185f));
                float lines = lerp(linesA, linesB, frame);

                float edgeMask = lerp(1.0f, SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uvBase1 * 0.5f).r, _EdgeNoise);
                lines *= edgeMask;

                // Muestra el sprite base
                float4 spriteA = SAMPLE_TEXTURE2D(_SpriteA, sampler_SpriteA, uvMain);
                float4 spriteB = SAMPLE_TEXTURE2D(_SpriteB, sampler_SpriteB, uvMain);

                float spriteFrame = step(0.0f, sin(timeSpriteFlip * 6.283185f));
                float4 spriteSample = lerp(spriteA, spriteB, spriteFrame) * input.color * _BaseColor;

                float baseAlpha = spriteSample.a;
                float3 col = spriteSample.rgb * lines;
                return float4(col, baseAlpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
