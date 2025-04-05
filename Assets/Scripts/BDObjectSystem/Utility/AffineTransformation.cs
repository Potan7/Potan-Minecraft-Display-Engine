using System;
using System.Collections.Generic;
using NUnit.Framework.Internal.Commands;
using UnityEngine;

namespace BDObjectSystem.Utility
{
    public static class AffineTransformation
    {

        public static Matrix4x4 GetMatrix(this float[] t)
        {
            if (t.Length == 16)
            {
                // Row-Major 16개짜리를 Column-Major로 바꿔서 넣기
                return new Matrix4x4(
                    // column0
                    new Vector4(t[0], t[4], t[8], t[12]),
                    // column1
                    new Vector4(t[1], t[5], t[9], t[13]),
                    // column2
                    new Vector4(t[2], t[6], t[10], t[14]),
                    // column3
                    new Vector4(t[3], t[7], t[11], t[15])
                );
            }

            // 예외 처리
            Debug.LogError("Invalid transform data");
            return Matrix4x4.identity;
        }

        public static void MatrixToArray(Matrix4x4 m, float[] array)
        {
            if (array == null || array.Length != 16)
                throw new ArgumentException("array must be a float[16]");

            array[0] = m[0, 0]; array[1] = m[0, 1]; array[2] = m[0, 2]; array[3] = m[0, 3];
            array[4] = m[1, 0]; array[5] = m[1, 1]; array[6] = m[1, 2]; array[7] = m[1, 3];
            array[8] = m[2, 0]; array[9] = m[2, 1]; array[10] = m[2, 2]; array[11] = m[2, 3];
            array[12] = m[3, 0]; array[13] = m[3, 1]; array[14] = m[3, 2]; array[15] = m[3, 3];
        }
        public static void ApplyMatrixToTransform(Transform target, in Matrix4x4 matrix)
        {
            // 1) Translation 추출 (4번째 컬럼)
            Vector3 translation = matrix.GetColumn(3);

            // 2) 3x3 부분 추출 (스케일, shear, 회전 포함)
            Vector3 col0 = matrix.GetColumn(0);
            Vector3 col1 = matrix.GetColumn(1);
            Vector3 col2 = matrix.GetColumn(2);

            // a) X축 스케일과 정규화 벡터 'a' 계산
            float scaleX = col0.magnitude;
            Vector3 a = (scaleX != 0) ? col0 / scaleX : Vector3.zero;

            // b) X-Y shear: a와 col1의 내적
            float shearXY = Vector3.Dot(a, col1);
            // col1에서 a 방향 성분 제거
            Vector3 col1NoShear = col1 - a * shearXY;
            float scaleY = col1NoShear.magnitude;
            Vector3 b = (scaleY != 0) ? col1NoShear / scaleY : Vector3.zero;

            // c) X-Z, Y-Z shear 계산: a와 col2, b와 col2의 내적
            float shearXZ = Vector3.Dot(a, col2);
            float shearYZ = Vector3.Dot(b, col2);
            // col2에서 a, b 방향 성분 제거
            Vector3 col2NoShear = col2 - a * shearXZ - b * shearYZ;
            float scaleZ = col2NoShear.magnitude;
            Vector3 c = (scaleZ != 0) ? col2NoShear / scaleZ : Vector3.zero;

            // 이제 a, b, c는 순수 회전을 나타내는 정규화된 축 벡터입니다.
            // 순수 회전 행렬 생성
            Matrix4x4 rotationMatrix = new Matrix4x4();
            rotationMatrix.SetColumn(0, new Vector4(a.x, a.y, a.z, 0));
            rotationMatrix.SetColumn(1, new Vector4(b.x, b.y, b.z, 0));
            rotationMatrix.SetColumn(2, new Vector4(c.x, c.y, c.z, 0));
            rotationMatrix.SetColumn(3, new Vector4(0, 0, 0, 1));

            // Quaternion 추출 (이미 정규화된 회전 행렬에서)
            Quaternion rotation = QuaternionFromMatrix(rotationMatrix);

            // 최종 스케일 (shear는 따로 Transform에 적용할 수 없으므로 보통 무시)
            Vector3 scale = new Vector3(scaleX, scaleY, scaleZ);

            // 4) 결과를 Transform에 적용 (로컬 기준)
            target.localPosition = translation;
            target.localRotation = rotation;
            target.localScale = scale;

            // 참고: 추출된 shear 값은
            // shearXY, shearXZ, shearYZ 에 저장되어 있습니다.
            // 필요시 별도로 로깅하거나 디버깅할 수 있습니다.
            
            // shear 값이 존재하면 출력
            if (Mathf.Abs(shearXY) > 0.01f || Mathf.Abs(shearXZ) > 0.01f || Mathf.Abs(shearYZ) > 0.01f)
            {
                Debug.Log($"Shear values: XY={shearXY}, XZ={shearXZ}, YZ={shearYZ}");
            }
        }

        /// <summary>
        /// 주어진 회전 행렬(Matrix4x4)의 3x3 부분으로부터 Quaternion을 추출한다.
        /// 결과 Quaternion은 정규화되어 반환된다.
        /// </summary>
        public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
        {
            Quaternion q = new Quaternion();
            float trace = m.m00 + m.m11 + m.m22;
            if (trace > 0)
            {
                float s = Mathf.Sqrt(trace + 1.0f) * 2; // s = 4 * qw
                q.w = 0.25f * s;
                q.x = (m.m21 - m.m12) / s;
                q.y = (m.m02 - m.m20) / s;
                q.z = (m.m10 - m.m01) / s;
            }
            else if ((m.m00 > m.m11) && (m.m00 > m.m22))
            {
                float s = Mathf.Sqrt(1.0f + m.m00 - m.m11 - m.m22) * 2; // s = 4 * qx
                q.w = (m.m21 - m.m12) / s;
                q.x = 0.25f * s;
                q.y = (m.m01 + m.m10) / s;
                q.z = (m.m02 + m.m20) / s;
            }
            else if (m.m11 > m.m22)
            {
                float s = Mathf.Sqrt(1.0f + m.m11 - m.m00 - m.m22) * 2; // s = 4 * qy
                q.w = (m.m02 - m.m20) / s;
                q.x = (m.m01 + m.m10) / s;
                q.y = 0.25f * s;
                q.z = (m.m12 + m.m21) / s;
            }
            else
            {
                float s = Mathf.Sqrt(1.0f + m.m22 - m.m00 - m.m11) * 2; // s = 4 * qz
                q.w = (m.m10 - m.m01) / s;
                q.x = (m.m02 + m.m20) / s;
                q.y = (m.m12 + m.m21) / s;
                q.z = 0.25f * s;
            }
            return q.normalized;
        }




        /// <summary>
        /// 최상위 부모 오브젝트(root)로부터 시작하여,
        /// 트리 구조 내의 모든 '잎(leaf) 노드'들의 월드 행렬을 구해 반환한다.
        /// </summary>
        /// <param name="root">최상위 부모 BdObject</param>
        /// <returns>잎 노드 -> 해당 월드행렬 딕셔너리</returns>
        public static Dictionary<string, Matrix4x4> GetAllLeafWorldMatrices(BdObject root)
        {
            var result = new Dictionary<string, Matrix4x4>();

            //Matrix4x4 bigMatrix = ScaleMatrixUp(Matrix4x4.identity, 10f);

            // 재귀 호출 시작: 처음 parentWorld는 단위행렬(Identity)로 시작
            TraverseAndCollectLeaf(root, Matrix4x4.identity, result);
            //TraverseAndCollectLeaf(root, bigMatrix, result);

            return result;
        }

        /// <summary>
        /// 현재 노드(node)와 누적 월드행렬(parentWorld)을 받아,
        /// 자식들이 있으면 순회하고, 없으면 잎이므로 result에 저장
        /// 이때 월드행렬은 부모의 월드행렬과 현재 노드의 로컬행렬을 곱하여 계산
        /// </summary>
        private static void TraverseAndCollectLeaf(
            BdObject node,
            Matrix4x4 parentWorld,
            Dictionary<string, Matrix4x4> result)
        {
            // 1) 현재 노드의 로컬 행렬
            Matrix4x4 localMatrix = GetMatrix(node.Transforms);

            // 2) 부모 월드행렬 x 로컬행렬 => 현재 노드의 월드행렬
            Matrix4x4 worldMatrix = parentWorld * localMatrix;

            if (node.IsDisplay)
            {
                // result에 기록
                result[node.ID] = worldMatrix;
            }
            else
            {
                // 자식이 있으면 모든 자식에 대해 재귀
                foreach (var child in node.Children)
                {
                    TraverseAndCollectLeaf(child, worldMatrix, result);
                }
            }
        }


    }
}
