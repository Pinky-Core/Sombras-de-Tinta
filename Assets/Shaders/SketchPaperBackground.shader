Shader "Custom/SketchPaperBackground"
{
    Properties
    {
        _MainTex ("Paper Texture", 2D) = "white" {}
        _PaperColor ("Paper Color", Color) = (0.98, 0.97, 0.95, 1)
        _LineColor ("Line Color", Color) = (0.92, 0.91, 0.89, 1)
        _GridSize ("Grid Size", Float) = 80.0
        _LineWidth ("Line Width", Range(0.001, 0.02)) = 0.003
        
        [Header(Sketch Animation)]
        _SketchTex ("Sketch Texture", 2D) = "black" {}
        _AnimationSpeed ("Animation Speed", Range(0, 10)) = 1.0
        _SketchIntensity ("Sketch Intensity", Range(0, 1)) = 0.1
        _SketchScale ("Sketch Scale", Float) = 3.0
        
        [Header(Paper Wrinkles)]
        _WrinkleScale ("Wrinkle Scale", Float) = 15.0
        _WrinkleIntensity ("Wrinkle Intensity", Range(0, 1)) = 0.15
        _WrinkleSpeed ("Wrinkle Animation Speed", Range(0, 2)) = 0.1
        
        [Header(Noise)]
        _NoiseScale ("Noise Scale", Float) = 200.0
        _NoiseIntensity ("Noise Intensity", Range(0, 1)) = 0.05
    }
    
    SubShader
    {
        Tags {"Queue"="Background" "RenderType"="Opaque"}
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float2 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _SketchTex;
            float4 _MainTex_ST;
            float4 _SketchTex_ST;
            fixed4 _PaperColor;
            fixed4 _LineColor;
            float _GridSize;
            float _LineWidth;
            float _AnimationSpeed;
            float _SketchIntensity;
            float _SketchScale;
            float _NoiseScale;
            float _NoiseIntensity;
            float _WrinkleScale;
            float _WrinkleIntensity;
            float _WrinkleSpeed;

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
            
            // Función para múltiples octavas de ruido (para arrugas)
            float fbm(float2 st)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                for (int i = 0; i < 4; i++)
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Color base del papel
                fixed4 paperCol = tex2D(_MainTex, i.uv) * _PaperColor;
                
                // Líneas de cuadrícula muy sutiles
                float gridPattern = 1.0 - grid(i.worldPos, _GridSize, _LineWidth) * 0.03;
                paperCol.rgb *= gridPattern;
                paperCol.rgb = lerp(paperCol.rgb, _LineColor.rgb, (1.0 - gridPattern) * 0.1);
                
                // Ruido sutil para textura de papel
                float paperNoise = random(i.uv * _NoiseScale);
                paperCol.rgb += (paperNoise - 0.5) * _NoiseIntensity;
                
                // Efecto de papel arrugado
                float2 wrinkleUV = i.uv * _WrinkleScale + _Time.y * _WrinkleSpeed * 0.1;
                float wrinkles = fbm(wrinkleUV) * 2.0 - 1.0;
                
                // Aplicar arrugas como variaciones sutiles de luminosidad
                paperCol.rgb += wrinkles * _WrinkleIntensity * 0.1;
                
                // Arrugas más finas para detalles
                float fineWrinkles = fbm(i.uv * _WrinkleScale * 3.0 + _Time.y * _WrinkleSpeed * 0.05);
                paperCol.rgb += (fineWrinkles - 0.5) * _WrinkleIntensity * 0.05;
                
                // Animación de boceto muy sutil
                float2 sketchUV = i.uv * _SketchScale;
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
                
                return paperCol;
            }
            ENDCG
        }
    }
}