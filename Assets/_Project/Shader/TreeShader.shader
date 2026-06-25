Shader "Custom/TreeShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        // Wind
        _WindSpeed ("Wind Speed", Float) = 1.0
        _WindFrequency ("Wind Frequency", Float) = 1.0
        _WindStrength ("Wind Strength", Float) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Cull Back

        // ── Pass 1: Render chính + nhận shadow ───────────────────────────
        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fwdbase       // Compile các variant shadow

            #include "UnityCG.cginc"
            #include "AutoLight.cginc"           // Cần thiết để nhận shadow

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _WindSpeed;
            float _WindFrequency;
            float _WindStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                SHADOW_COORDS(1)                // Texcoord slot 1 cho shadow
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                // ── Wind ─────────────────────────────────────────────────
                // Lấy bounds Y của mesh để tính ngưỡng 1/3 tự động
                // unity_ObjectToWorld row 1 (scale Y) không đủ nên dùng cách đơn giản:
                // giả sử pivot ở gốc, windMask = 0 khi y < 1/3 max, tăng dần lên 1
                // Ta không biết maxY lúc runtime nên dùng saturate với threshold cố định
                // Threshold = 0.33 nghĩa là 1/3 dưới đứng yên, 2/3 trên đung đưa
                float windMask = saturate((v.vertex.y - 0.33) / (1.0 - 0.33));

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                float wave = sin(
                    _Time.y * _WindSpeed
                    + worldPos.x * _WindFrequency
                    + worldPos.z * _WindFrequency * 0.7
                );

                float3 windOffset = float3(wave, 0, wave * 0.5) * _WindStrength * windMask;
                v.vertex.xyz += mul((float3x3)unity_WorldToObject, windOffset);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                TRANSFER_SHADOW(o);             // Tính shadow coords
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                fixed4 col = tex2D(_MainTex, i.uv);

                // Nhận shadow — shadow = 1 sáng, shadow = 0 tối
                fixed shadow = SHADOW_ATTENUATION(i);

                // Unlit nhưng vẫn nhận shadow: nhân texture với shadow
                col.rgb *= shadow;

                return col;
            }

            ENDCG
        }

        // ── Pass 2: Cast shadow (để cây đổ bóng xuống đất) ──────────────
        Pass
        {
            Tags { "LightMode"="ShadowCaster" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_shadowcaster

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                V2F_SHADOW_CASTER;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                // Wind trong shadow pass để bóng khớp với mesh đang đung đưa
                float windMask = saturate((v.vertex.y - 0.33) / (1.0 - 0.33));

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float wave = sin(
                    _Time.y * 1.0        // WindSpeed mặc định — property không dùng được ở đây
                    + worldPos.x * 1.0
                    + worldPos.z * 0.7
                );

                v.vertex.xyz += mul((float3x3)unity_WorldToObject,
                    float3(wave, 0, wave * 0.5) * 0.1 * windMask);

                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }

            ENDCG
        }
    }

    Fallback "Diffuse"
}
