using System;
using System.Collections.Generic;
using NUnit.Framework.Internal.Commands;
using UnityEngine;

namespace BDObjectSystem.Utility
{
    public static class AffineTransformation
    {

        public static Matrix4x4 GetMatrix(float[] t)
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
            // 1) 위치 (Translation)
            Vector3 translation = matrix.GetColumn(3);

            // 2) 스케일 및 회전용 3x3 부분 추출
            Vector3 col0 = matrix.GetColumn(0);
            Vector3 col1 = matrix.GetColumn(1);
            Vector3 col2 = matrix.GetColumn(2);

            // 스케일은 각 컬럼 벡터의 크기
            float scaleX = col0.magnitude;
            float scaleY = col1.magnitude;
            float scaleZ = col2.magnitude;
            Vector3 scale = new Vector3(scaleX, scaleY, scaleZ);

            // 컬럼 벡터를 정규화하여 순수 회전 행렬을 만듦
            if (scaleX != 0) col0 /= scaleX;
            if (scaleY != 0) col1 /= scaleY;
            if (scaleZ != 0) col2 /= scaleZ;

            // 3) 회전: 정규화된 컬럼으로 회전 행렬 생성 후 Quaternion 변환
            Matrix4x4 rotationMatrix = new Matrix4x4();
            rotationMatrix.SetColumn(0, new Vector4(col0.x, col0.y, col0.z, 0));
            rotationMatrix.SetColumn(1, new Vector4(col1.x, col1.y, col1.z, 0));
            rotationMatrix.SetColumn(2, new Vector4(col2.x, col2.y, col2.z, 0));
            rotationMatrix.SetColumn(3, new Vector4(0, 0, 0, 1));

            Quaternion rotation = QuaternionFromMatrix(rotationMatrix);

            // 4) 실제 Transform에 적용
            target.localPosition = translation;
            target.localScale = scale;
            target.localRotation = rotation;
        }

        /// <summary>
        /// 주어진 회전 행렬(Matrix4x4)의 3x3 부분으로부터 Quaternion을 추출한다.
        /// 결과 Quaternion은 정규화되어 반환된다.
        /// </summary>
        private static Quaternion QuaternionFromMatrix(Matrix4x4 m)
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
        /// 입력 행렬 m에서 스케일을 제거하여, translation과 순수 회전만 포함하는 TRS 행렬을 반환한다.
        /// </summary>
        public static Matrix4x4 RemoveScale(Matrix4x4 m)
        {
            // 1. Translation 추출
            Vector3 pos = m.GetColumn(3);

            // 2. 회전 부분 추출: 각 컬럼을 정규화하여 스케일 제거
            Vector3 col0 = m.GetColumn(0);
            Vector3 col1 = m.GetColumn(1);
            Vector3 col2 = m.GetColumn(2);

            if (col0.sqrMagnitude > 0) col0.Normalize();
            if (col1.sqrMagnitude > 0) col1.Normalize();
            if (col2.sqrMagnitude > 0) col2.Normalize();

            // 3. 정규화된 컬럼으로 순수 회전 행렬 구성
            Matrix4x4 rotMat = new Matrix4x4();
            rotMat.SetColumn(0, new Vector4(col0.x, col0.y, col0.z, 0));
            rotMat.SetColumn(1, new Vector4(col1.x, col1.y, col1.z, 0));
            rotMat.SetColumn(2, new Vector4(col2.x, col2.y, col2.z, 0));
            rotMat.SetColumn(3, new Vector4(0, 0, 0, 1));

            // 4. 회전 행렬을 Quaternion으로 변환하고, 반드시 정규화
            Quaternion rot = QuaternionFromMatrix(rotMat);

            // 5. 스케일은 (1,1,1)로 설정한 TRS 행렬 반환
            return Matrix4x4.TRS(pos, rot, Vector3.one);
        }

        /// <summary>
        /// root를 시작으로 순회하며 display 노드의 바로 상위 부모 월드 역행렬을 구해 설정한다.
        /// 그렇게 구한 월드 행렬을 각 display 노드의 parentWorldMatrix에 저장한다.
        /// </summary>
        /// <param name="root"></param>
        public static void SetParentInverseWorldMatrix(BdObjectContainer root)
        {
            if (root == null) return;

            TraverseAndSet(root, Matrix4x4.identity);
        }

        private static void TraverseAndSet(BdObjectContainer node, Matrix4x4 parentWorld)
        {
            // 현재 노드의 local 행렬
            Matrix4x4 local = node.transformation;

            // 현재 노드의 world 행렬 = 부모의 world * 자신의 local
            Matrix4x4 currentWorld = parentWorld * local;

            // 자식이 displayObj를 가지고 있다면 → 현재 노드가 display의 상위 부모임
            if (node.children != null)
            {
                foreach (var child in node.children)
                {
                    if (child.BdObject.IsDisplay)
                    {
                        child.parentWorldMatrix = currentWorld.inverse;
                    }

                    // 재귀 호출
                    TraverseAndSet(child, currentWorld);
                }
            }
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
