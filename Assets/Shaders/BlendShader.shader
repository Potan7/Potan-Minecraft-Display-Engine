Shader "Hidden/BlendShader"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _OverlayTex ("Overlay Texture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;

            v2f vert (float4 pos : POSITION, float2 uv : TEXCOORD0)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(pos);
                o.uv = uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }

        // 새로운 패스 추가 (Pass 1) - 오버레이 적용
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_overlay
            #include "UnityCG.cginc"

            sampler2D _OverlayTex;

            fixed4 frag_overlay (v2f i) : SV_Target
            {
                return tex2D(_OverlayTex, i.uv);
            }
            ENDCG
        }
    }
}
