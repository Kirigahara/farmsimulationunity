Shader "Custom/GrassShader"
{
    Properties
    {
        _ColorBottom ("Color Bottom", Color) = (0.1, 0.4, 0.1, 1)
        _ColorTop ("Color Top", Color) = (0.6, 0.9, 0.2, 1)

        // Wind
        _WindSpeed ("Wind Speed", Float) = 1.0
        _WindFrequency ("Wind Frequency", Float) = 1.0
        _WindStrength ("Wind Strength", Float) = 0.1

        //Height
        _GrassHeight ("Grass Height", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        // Render cả 2 mặt
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing  // Bắt buộc cho GPU Instancing

            #include "UnityCG.cginc"

            // ─── Properties ───────────────────────────────────────────
            fixed4 _ColorBottom;
            fixed4 _ColorTop;

            float _WindSpeed;
            float _WindFrequency;
            float _WindStrength;

            float _GrassHeight; // chiều cao thực tế của mesh

            // ─── Structs ───────────────────────────────────────────────
            struct appdata
            {
                float4 vertex   : POSITION;
                float3 normal   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos          : SV_POSITION;
                float  heightFactor : TEXCOORD0; // 0 = gốc, 1 = ngọn
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // ─── Vertex Shader ─────────────────────────────────────────
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                // Tính heightFactor từ local Y position
                // Giả sử mesh có gốc ở Y=0, ngọn ở Y=1 (hoặc cao hơn)
                // Clamp để an toàn
                float heightFactor = saturate(v.vertex.y / _GrassHeight);
                o.heightFactor = heightFactor;

                // ── Wind ──────────────────────────────────────────────
                // Lấy world position để các instance không đồng bộ với nhau
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                // Sin wave dựa trên world X + Z và thời gian
                float wave = sin(
                    _Time.y * _WindSpeed
                    + worldPos.x * _WindFrequency
                    + worldPos.z * _WindFrequency * 0.7  // offset Z để tự nhiên hơn
                );

                // Chỉ phần ngọn mới dao động (nhân với heightFactor)
                // Gốc cỏ (heightFactor=0) đứng yên hoàn toàn
                float3 windOffset = float3(wave, 0, wave * 0.5) * _WindStrength * heightFactor;

                v.vertex.xyz += mul((float3x3)unity_WorldToObject, windOffset);

                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            // ─── Fragment Shader ────────────────────────────────────────
            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // Gradient từ gốc (bottom) lên ngọn (top)
                fixed4 col = lerp(_ColorBottom, _ColorTop, i.heightFactor);
                return col;
            }

            ENDCG
        }
    }

    Fallback "Diffuse"
}
