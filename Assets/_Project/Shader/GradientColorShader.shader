Shader "Custom/GradientColorShader"
{
    Properties
    {
        _ColorBottom ("Color Bottom", Color) = (0.1, 0.4, 0.1, 1)
        _ColorTop ("Color Top", Color) = (0.6, 0.9, 0.2, 1)

        //Height
        _GrassHeight ("Height", Float) = 1.0
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

            float _GrassHeight; // chiều cao thực tế của mesh

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos          : SV_POSITION;
                float  heightFactor : TEXCOORD0; // 0 = gốc, 1 = ngọn
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.heightFactor = saturate(v.vertex.y / _GrassHeight);
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
