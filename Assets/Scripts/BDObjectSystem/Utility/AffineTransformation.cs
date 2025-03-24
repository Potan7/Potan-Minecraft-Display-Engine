using UnityEngine;

namespace BDObjectSystem.Utility
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

        public static void ApplyMatrixToTransform(Transform target, in Matrix4x4 matrix)
        {
            // 1) 위치
            Vector3 translation = matrix.GetColumn(3);

            // 2) 스케일
            Vector3 scale = new Vector3(
                matrix.GetColumn(0).magnitude,
                matrix.GetColumn(1).magnitude,
                matrix.GetColumn(2).magnitude
            );

            // 3) 회전 - LookRotation 대신에 matrix.rotation 사용
            Quaternion rotation = matrix.rotation;

            // 4) 실제 Transform에 적용
            target.localPosition = translation;
            target.localScale = scale;
            target.localRotation = rotation;
        }

        // 해당 BDObject의 모든 Parent Transform을 적용한 WorldMatrix 반환
        public static Matrix4x4 GetWorldMatrix(BdObject bdObject)
        {
            BdObject obj = bdObject;
            Matrix4x4 transforms = GetMatrix(bdObject.Transforms);

            while (obj.Parent != null)
            {
                BdObject parent = obj.Parent;
                Matrix4x4 parentMatrix = GetMatrix(parent.Transforms);

                transforms = parentMatrix * transforms;
                obj = parent;
            }

            return transforms;
        }

    }
}
