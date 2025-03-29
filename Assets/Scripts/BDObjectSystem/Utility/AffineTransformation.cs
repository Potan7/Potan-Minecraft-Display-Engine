using System;
using System.Collections.Generic;
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

        public static float[] MatrixToArray(Matrix4x4 m)
        {
            return new float[]
            {
        m[0,0], m[0,1], m[0,2], m[0,3], // row 0
        m[1,0], m[1,1], m[1,2], m[1,3], // row 1
        m[2,0], m[2,1], m[2,2], m[2,3], // row 2
        m[3,0], m[3,1], m[3,2], m[3,3]  // row 3
            };
        }




        public static void ApplyMatrixToTransform(Transform target, in Matrix4x4 matrix)
        {
            // 1) 위치 ( Translation )
            Vector3 translation = matrix.GetColumn(3);

            // 2) 스케일 (단순 컬럼의 크기)
            Vector3 scale = new Vector3(
                matrix.GetColumn(0).magnitude,
                matrix.GetColumn(1).magnitude,
                matrix.GetColumn(2).magnitude
            );

            // 3) 회전
            //  - 음수 스케일이 섞여 있으면 Unity 내부적으로 "뒤집힌 축"을 양수로 만들어 놓고 rotation을 추출함.
            //  - 그래도 LookRotation(...)으로 직접 구하는 것보다 안전함
            Quaternion rotation = matrix.rotation;
            // Vector3 euler = rotation.eulerAngles;
            // euler.z *= -1;
            // rotation = Quaternion.Euler(euler);

            // 4) 실제 Transform에 적용
            target.localPosition = translation;
            target.localScale = scale;
            target.localRotation = rotation;
        }



        // 해당 BDObject의 모든 Parent Transform을 적용한 WorldMatrix 반환
        // public static Matrix4x4 GetWorldMatrix(BdObject bdObject)
        // {
        //     BdObject obj = bdObject;
        //     Matrix4x4 transforms = GetMatrix(bdObject.Transforms);

        //     while (obj.Parent != null)
        //     {
        //         BdObject parent = obj.Parent;
        //         Matrix4x4 parentMatrix = GetMatrix(parent.Transforms);

        //         transforms = parentMatrix * transforms;
        //         obj = parent;
        //     }

        //     return transforms;
        // }

        /// <summary>
        /// 최상위 부모 오브젝트(root)로부터 시작하여,
        /// 트리 구조 내의 모든 '잎(leaf) 노드'들의 월드 행렬을 구해 반환한다.
        /// </summary>
        /// <param name="root">최상위 부모 BdObject</param>
        /// <returns>잎 노드 -> 해당 월드행렬 딕셔너리</returns>
        public static Dictionary<string, Matrix4x4> GetAllLeafWorldMatrices(BdObject root)
        {
            var result = new Dictionary<string, Matrix4x4>();

            // 재귀 호출 시작: 처음 parentWorld는 단위행렬(Identity)로 시작
            TraverseAndCollectLeaf(root, Matrix4x4.identity, result);

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

            // 3) 자식이 없으면 => 잎(leaf) 노드
            if (node.Children == null || node.Children.Length == 0)
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
