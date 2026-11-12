// Тилкайо run — 2D-спрайт персонажа в 3D-мире с перекраской скина:
// поворот оттенка (_Hue), насыщенность, яркость, множитель цвета, заливка силуэта.
Shader "Tilkayo/Sprite"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _Hue ("Hue Shift (deg)", Float) = 0
        _Sat ("Saturation", Float) = 1
        _Bright ("Brightness", Float) = 1
        _Solid ("Solid Mix", Range(0,1)) = 0
        _SolidColor ("Solid Color", Color) = (1,1,1,1)
        _Alpha ("Alpha", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Tint;
            float _Hue;
            float _Sat;
            float _Bright;
            float _Solid;
            fixed4 _SolidColor;
            float _Alpha;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float3 HueRotate(float3 c, float deg)
            {
                float a = deg * 0.0174532925;
                float cs = cos(a);
                float sn = sin(a);
                // вращение в пространстве YIQ
                float3x3 m = float3x3(
                    0.299 + 0.701 * cs + 0.168 * sn, 0.587 - 0.587 * cs + 0.330 * sn, 0.114 - 0.114 * cs - 0.497 * sn,
                    0.299 - 0.299 * cs - 0.328 * sn, 0.587 + 0.413 * cs + 0.035 * sn, 0.114 - 0.114 * cs + 0.292 * sn,
                    0.299 - 0.300 * cs + 1.250 * sn, 0.587 - 0.588 * cs - 1.050 * sn, 0.114 + 0.886 * cs - 0.203 * sn);
                return mul(m, c);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 t = tex2D(_MainTex, i.uv);
                float3 c = HueRotate(t.rgb, _Hue);
                float l = dot(c, float3(0.299, 0.587, 0.114));
                c = lerp(float3(l, l, l), c, _Sat);
                c = saturate(c * _Bright) * _Tint.rgb;
                c = lerp(c, _SolidColor.rgb, _Solid);
                float a = t.a * _Tint.a * _Alpha;
                return fixed4(c, a);
            }
            ENDCG
        }
    }
}
