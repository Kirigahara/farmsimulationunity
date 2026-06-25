Shader "Custom/GrassShadowShader"
{
    Properties
    {
        _ShadowColor ("Shadow Color", Color) = (0.1, 0.15, 0.05, 0.4)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha  // Alpha blend — thấy xuyên qua màu đất bên dưới

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _ShadowColor;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _ShadowColor;
            }
            ENDCG
        }
    }
}
