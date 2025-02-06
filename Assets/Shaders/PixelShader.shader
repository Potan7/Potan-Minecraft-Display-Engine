Shader "Custom/Pixel3DShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Depth ("Depth", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Depth;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // 픽셀 높이를 이미지의 밝기에 따라 조정 (Displacement Mapping)
                float height = tex2Dlod(_MainTex, float4(v.uv, 0, 0)).r * _Depth;
                o.vertex.y += height; // Y축 방향으로 이동
                
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                col.rgb *= col.a; // 투명 픽셀 처리
                return col;
            }
            ENDCG
        }
    }
}
