Shader "Custom/SketchCellShader"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _SketchTex ("Sketch Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness", Range(0.001, 0.03)) = 0.01
        _SketchSpeed ("Sketch Movement Speed", Range(0,2)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 250

        // Outline pass
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _OutlineThickness;
            fixed4 _OutlineColor;

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
                float3 offset = normalize(v.normal) * _OutlineThickness;
                o.pos = UnityObjectToClipPos(v.vertex + float4(offset, 0));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        // Cel shaded body
        Pass
        {
            Name "SketchBody"
            Tags { "LightMode"="ForwardBase" }
            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _SketchTex;
            float4 _SketchTex_ST;
            fixed4 _Color;
            float _SketchSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uvMain : TEXCOORD0;
                float2 uvSketch : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uvMain = TRANSFORM_TEX(v.uv, _MainTex);
                o.uvSketch = TRANSFORM_TEX(v.uv, _SketchTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float ndotl = saturate(dot(normal, lightDir));
                float shade = lerp(0.35, 1.0, step(0.5, ndotl));

                float2 animatedUV = i.uvSketch + float2(_Time.y * _SketchSpeed, _Time.y * _SketchSpeed);
                float sketchSample = tex2D(_SketchTex, animatedUV).r;

                fixed4 baseCol = tex2D(_MainTex, i.uvMain) * _Color;
                float3 finalColor = baseCol.rgb * shade * sketchSample;

                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                return fixed4(finalColor, baseCol.a);
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
