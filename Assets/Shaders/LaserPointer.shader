Shader "MoonlightVR/LaserPointer"
{
    Properties
    {
        _Color ("Laser Core Color", Color) = (0.3, 0.8, 1.0, 1.0)
        _GlowColor ("Laser Glow Color", Color) = (0.0, 0.4, 1.0, 0.5)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

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

            fixed4 _Color;
            fixed4 _GlowColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float distFromCenter = abs(i.uv.y - 0.5) * 2.0;
                float core = saturate(1.0 - distFromCenter * 3.0);
                float glow = saturate(1.0 - distFromCenter);

                fixed4 col = lerp(_GlowColor, _Color, core);
                col.a = glow * _GlowColor.a;

                return col;
            }
            ENDCG
        }
    }
}
