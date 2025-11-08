Shader "UI/SketchPaper"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Paper Properties)]
        _PaperColor ("Paper Color", Color) = (0.98, 0.97, 0.95, 1)
        _LineColor ("Line Color", Color) = (0.92, 0.91, 0.89, 1)
        _GridSize ("Grid Size", Float) = 80.0
        _LineWidth ("Line Width", Range(0.001, 0.02)) = 0.003
        
        [Header(Sketch Animation)]
        _SketchTex ("Sketch Texture", 2D) = "white" {}
        _AnimationSpeed ("Animation Speed", Range(0, 10)) = 0.5
        _SketchIntensity ("Sketch Intensity", Range(0, 1)) = 0.08
        _SketchScale ("Sketch Scale", Float) = 3.0
        
        [Header(Paper Wrinkles)]
        _WrinkleScale ("Wrinkle Scale", Float) = 15.0
        _WrinkleIntensity ("Wrinkle Intensity", Range(0, 1)) = 0.12
        _WrinkleSpeed ("Wrinkle Animation Speed", Range(0, 2)) = 0.1
        
        [Header(Noise)]
        _NoiseScale ("Noise Scale", Float) = 200.0
        _NoiseIntensity ("Noise Intensity", Range(0, 1)) = 0.03
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            sampler2D _SketchTex;
            fixed4 _Color;
            fixed4 _PaperColor;
            fixed4 _LineColor;
            float _GridSize;
            float _LineWidth;
            float _AnimationSpeed;
            float _SketchIntensity;
            float _SketchScale;
            float _WrinkleScale;
            float _WrinkleIntensity;
            float _WrinkleSpeed;
            float _NoiseScale;
            float _NoiseIntensity;

            // Función para generar ruido simple
            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }
            
            // Función para ruido Perlin simplificado
            float noise(float2 st)
            {
                float2 i = floor(st);
                float2 f = frac(st);
                float a = random(i);
                float b = random(i + float2(1.0, 0.0));
                float c = random(i + float2(0.0, 1.0));
                float d = random(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }
            
            // Función para múltiples octavas de ruido
            float fbm(float2 st)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                for (int i = 0; i < 3; i++)
                {
                    value += amplitude * noise(st * frequency);
                    st *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            // Función para líneas de cuadrícula más sutiles
            float grid(float2 uv, float size, float width)
            {
                float2 r = 1.0 - smoothstep(width * 0.5, width, abs(sin(uv * 3.14159 * size)));
                return r.x * r.y;
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Color base del papel
                fixed4 paperCol = tex2D(_MainTex, IN.texcoord) * _PaperColor;
                
                // Líneas de cuadrícula muy sutiles
                float2 gridUV = IN.texcoord * _GridSize;
                float gridPattern = 1.0 - grid(gridUV, 1.0, _LineWidth) * 0.03;
                paperCol.rgb *= gridPattern;
                paperCol.rgb = lerp(paperCol.rgb, _LineColor.rgb, (1.0 - gridPattern) * 0.1);
                
                // Ruido sutil para textura de papel
                float paperNoise = random(IN.texcoord * _NoiseScale);
                paperCol.rgb += (paperNoise - 0.5) * _NoiseIntensity;
                
                // Efecto de papel arrugado
                float2 wrinkleUV = IN.texcoord * _WrinkleScale + _Time.y * _WrinkleSpeed * 0.1;
                float wrinkles = fbm(wrinkleUV) * 2.0 - 1.0;
                
                // Aplicar arrugas como variaciones sutiles de luminosidad
                paperCol.rgb += wrinkles * _WrinkleIntensity * 0.1;
                
                // Arrugas más finas para detalles
                float fineWrinkles = fbm(IN.texcoord * _WrinkleScale * 3.0 + _Time.y * _WrinkleSpeed * 0.05);
                paperCol.rgb += (fineWrinkles - 0.5) * _WrinkleIntensity * 0.05;
                
                // Animación de boceto muy sutil
                float2 sketchUV = IN.texcoord * _SketchScale;
                sketchUV.x += sin(_Time.y * _AnimationSpeed * 0.2) * 0.002;
                sketchUV.y += cos(_Time.y * _AnimationSpeed * 0.15) * 0.001;
                
                // Múltiples capas de boceto muy sutiles
                fixed4 sketch1 = tex2D(_SketchTex, sketchUV + _Time.y * _AnimationSpeed * 0.005);
                fixed4 sketch2 = tex2D(_SketchTex, sketchUV * 2.1 - _Time.y * _AnimationSpeed * 0.003);
                fixed4 sketch3 = tex2D(_SketchTex, sketchUV * 0.8 + _Time.y * _AnimationSpeed * 0.002);
                
                // Combinar bocetos de forma más sutil
                float sketchPattern = (sketch1.r + sketch2.r * 0.3 + sketch3.r * 0.2) / 1.5;
                
                // Aplicar boceto con intensidad muy reducida
                paperCol.rgb = lerp(paperCol.rgb, paperCol.rgb * (1.0 - sketchPattern * 0.5), _SketchIntensity);
                
                // Aplicar color de tinte del UI
                paperCol *= IN.color;
                
                return paperCol;
            }
            ENDCG
        }
    }
}