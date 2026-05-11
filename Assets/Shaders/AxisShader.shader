// Unlit/AxisShader.shader
Shader "Unlit/AxisShader"
{
    Properties
    {
        _LineLength ("Line Length", Float) = 1.0
        _LineWidth ("Line Width", Range(0.1, 10)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Cull Off
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom

            #include "UnityCG.cginc"

            uniform float _LineLength;
            uniform float _LineWidth;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2g
            {
                float4 vertex : SV_POSITION;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            v2g vert (appdata v)
            {
                v2g o;
                // 정점 위치를 월드 공간으로 변환하여 지오메트리 쉐이더로 전달
                o.vertex = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            [maxvertexcount(18)]
            void geom(point v2g p[1], inout TriangleStream<g2f> triStream)
            {
                float3 origin = p[0].vertex.xyz;
                float halfWidth = _LineWidth * 0.005;

                // 카메라의 월드 위치와 up 벡터를 가져옴
                float3 camPos = _WorldSpaceCameraPos;
                float3 camUp = UNITY_MATRIX_V[1].xyz; // 카메라의 up 벡터

                float3 directions[3] = { float3(1,0,0), float3(0,1,0), float3(0,0,1) };
                float4 colors[3] = { float4(1,0,0,1), float4(0,1,0,1), float4(0,0,1,1) };

                for (int i = 0; i < 3; ++i)
                {
                    float3 axisDir = directions[i];
                    float4 color = colors[i];

                    // 축이 음수 방향으로도 뻗어나가도록 시작점을 변경합니다.
                    float3 start = origin - axisDir * _LineLength;
                    float3 end = origin + axisDir * _LineLength;

                    // 축과 카메라 위치를 이용해 빌보드 평면의 방향 벡터 계산
                    float3 viewDir = normalize(camPos - origin);
                    float3 lineRight = normalize(cross(axisDir, viewDir));
                    
                    // 축과 시선이 거의 평행할 때, 카메라의 up 벡터를 사용
                    if (length(lineRight) < 0.1) {
                        lineRight = normalize(cross(axisDir, camUp));
                    }

                    float3 widthVec = lineRight * halfWidth;

                    // 월드 공간에서 계산된 정점들을 클립 공간으로 변환
                    float4 v0 = UnityWorldToClipPos(float4(start - widthVec, 1.0));
                    float4 v1 = UnityWorldToClipPos(float4(start + widthVec, 1.0));
                    float4 v2 = UnityWorldToClipPos(float4(end - widthVec, 1.0));
                    float4 v3 = UnityWorldToClipPos(float4(end + widthVec, 1.0));

                    g2f pOut;

                    pOut.vertex = v0; pOut.color = color; triStream.Append(pOut);
                    pOut.vertex = v1; pOut.color = color; triStream.Append(pOut);
                    pOut.vertex = v2; pOut.color = color; triStream.Append(pOut);
                    triStream.RestartStrip();

                    pOut.vertex = v1; pOut.color = color; triStream.Append(pOut);
                    pOut.vertex = v3; pOut.color = color; triStream.Append(pOut);
                    pOut.vertex = v2; pOut.color = color; triStream.Append(pOut);
                    triStream.RestartStrip();
                }
            }

            fixed4 frag (g2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
