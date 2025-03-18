using System;
using Unity.Mathematics;
using UnityEngine;

namespace BDObjectSystem
{
    public static class AffineTransformation
    {
        //  float�迭�� ��ȯ�Ͽ� Matrix4x4 ����
        public static Matrix4x4 GetMatrix(float[] transforms)
        {
            if (transforms.Length == 16)
                return new Matrix4x4(
                    new Vector4(transforms[0], transforms[4], transforms[8], transforms[12]), // X ��
                    new Vector4(transforms[1], transforms[5], transforms[9], transforms[13]), // Y ��
                    new Vector4(transforms[2], transforms[6], transforms[10], transforms[14]), // Z ��
                    new Vector4(transforms[3], transforms[7], transforms[11], transforms[15]) // Translation
                );
            CustomLog.LogError("Invalid transform data");
            return Matrix4x4.identity;

            //  Row-Major
        }

        public static void ApplyMatrixToTransform(Transform target, Matrix4x4 matrix)
        {
            // 1. Translation (localPosition)
            Vector3 translation = matrix.GetColumn(3);

            // 2. Scale (localScale)
            var scale = new Vector3(
                matrix.GetColumn(0).magnitude, // X �� ������
                matrix.GetColumn(1).magnitude, // Y �� ������
                matrix.GetColumn(2).magnitude  // Z �� ������
            );

            // 3. Rotation (localRotation)
            //Vector3 normalizedX = matrix.GetColumn(0).normalized;
            Vector3 normalizedY = matrix.GetColumn(1).normalized;
            Vector3 normalizedZ = matrix.GetColumn(2).normalized;

            var forward = normalizedZ.magnitude > 0 ? normalizedZ : Vector3.forward;
            var up = normalizedY.magnitude > 0 ? normalizedY : Vector3.up;

            var rotation = Quaternion.LookRotation(forward, up);
            //Quaternion rotation = Quaternion.FromToRotation(Vector3.right, normalizedX) *
            //              Quaternion.FromToRotation(Vector3.up, normalizedY);

            //Quaternion rotation = matrix.rotation;


            // 4. Transform�� ����
            target.localPosition = translation;
            target.localScale = scale;
            target.localRotation = rotation;
        }

        // 해당 BDObject의 모든 Parent Transform을 적용한 WorldMatrix 반환
        public static float[] GetWorldMatrix(BdObject bdObject)
{
    BdObject obj = bdObject;
    var transforms = new float[16];
    
    // 초기 transforms를 bdObject의 로컬 Transform으로 설정
    for (int i = 0; i < 16; i++)
    {
        transforms[i] = bdObject.Transforms[i];
    }

    float[] temp = new float[16];

    while (obj.Parent != null)  // 부모가 존재하는 동안 반복
    {
        BdObject parent = obj.Parent;

        // 부모 * 현재 행렬을 곱함 (부모를 먼저 적용)
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                temp[i * 4 + j] =
                    parent.Transforms[i * 4 + 0] * transforms[0 + j] +
                    parent.Transforms[i * 4 + 1] * transforms[4 + j] +
                    parent.Transforms[i * 4 + 2] * transforms[8 + j] +
                    parent.Transforms[i * 4 + 3] * transforms[12 + j];
            }
        }

        // temp 값을 transforms에 복사하여 기존 배열을 덮어씀
        for (int i = 0; i < 16; i++)
        {
            transforms[i] = temp[i];
        }

        obj = parent;  // 부모로 이동
    }
    return transforms;
}

    }
}
