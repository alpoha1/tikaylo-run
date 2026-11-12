// Тилкайо run — шейдер мира: вертexные цвета, плоское освещение, туман,
// изгиб мира (curve), покачивание деревьев (uv.x = вес качания).
Shader "Tilkayo/World"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _Glow ("Glow (self light)", Range(0,1)) = 0
        _Curve ("World Curve", Float) = 0.0012
        _Sway ("Sway Amount", Float) = 0.25
        _FogOn ("Fog On", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Glow;
            float _Curve;
            float _Sway;
            float _FogOn;
            float4 _TFogColor;   // rgb = цвет тумана
            float4 _TFogParams;  // x = start, y = end
            float4 _TSunDir;     // направление на солнце (мировое)
            float4 _TAmbient;    // окружающий свет
            float4 _TSunColor;   // цвет солнца

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 col : COLOR0;
                float fog : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                float4 wp = mul(unity_ObjectToWorld, v.vertex);
                float3 wn = UnityObjectToWorldNormal(v.normal);
                // покачивание (фаза зависит от позиции объекта)
                float phase = unity_ObjectToWorld._m03 * 0.7 + unity_ObjectToWorld._m23 * 0.45;
                float sway = sin(_Time.y * 1.6 + phase + wp.y * 0.35) * _Sway * v.uv.x;
                wp.x += sway;
                wp.z += sway * 0.35;
                // изгиб мира вниз по мере удаления от камеры
                float3 rel = wp.xyz - _WorldSpaceCameraPos.xyz;
                float d2 = rel.z * rel.z;
                wp.y -= d2 * _Curve;
                o.pos = mul(UNITY_MATRIX_VP, wp);
                // свет
                float ndl = abs(dot(normalize(wn), normalize(_TSunDir.xyz)));
                float3 light = _TAmbient.rgb + _TSunColor.rgb * ndl;
                light = lerp(light, float3(1,1,1), _Glow);
                o.col = fixed4(v.color.rgb * _Color.rgb * light, 1);
                float dist = length(rel);
                float f = saturate((dist - _TFogParams.x) / max(0.001, (_TFogParams.y - _TFogParams.x)));
                o.fog = f * _FogOn;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed3 c = lerp(i.col.rgb, _TFogColor.rgb, i.fog);
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
}
