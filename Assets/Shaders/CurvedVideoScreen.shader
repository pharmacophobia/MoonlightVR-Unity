Shader "MoonlightVR/CurvedVideoScreen"
{
    Properties
    {
        _MainTex ("Desktop Stream Texture", 2D) = "black" {}
        _BorderColor ("Border Glow Color", Color) = (0.2, 0.6, 1.0, 1.0)
        _BorderThickness ("Border Thickness", Range(0.001, 0.05)) = 0.008
        _Brightness ("Brightness", Range(0.5, 2.0)) = 1.0
        _Contrast ("Contrast", Range(0.5, 2.0)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        Cull Back

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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _BorderColor;
            float _BorderThickness;
            float _Brightness;
            float _Contrast;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Edge border detection
                float2 borderDist = min(i.uv, 1.0 - i.uv);
                float minEdge = min(borderDist.x, borderDist.y);

                if (minEdge < _BorderThickness)
                {
                    float factor = 1.0 - (minEdge / _BorderThickness);
                    return lerp(tex2D(_MainTex, i.uv), _BorderColor, factor * 0.9);
                }

                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Color grading: contrast & brightness
                col.rgb = ((col.rgb - 0.5) * _Contrast) + 0.5;
                col.rgb *= _Brightness;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Unlit/Texture"
}
