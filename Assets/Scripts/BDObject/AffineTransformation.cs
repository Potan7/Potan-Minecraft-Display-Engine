using UnityEngine;
using System.Collections.Generic;

public class AffineTransformation
{
    //  float배열을 변환하여 Matrix4x4 생성
    public static Matrix4x4 GetMatrix(float[] transforms)
    {
        if (transforms.Length != 16)
        {
            CustomLog.LogError("Invalid transform data");
            return Matrix4x4.identity;
        }

        //  Row-Major
        return new Matrix4x4(
            new Vector4(transforms[0], transforms[4], transforms[8], transforms[12]),  // X 축
            new Vector4(transforms[1], transforms[5], transforms[9], transforms[13]),  // Y 축
            new Vector4(transforms[2], transforms[6], transforms[10], transforms[14]), // Z 축
            new Vector4(transforms[3], transforms[7], transforms[11], transforms[15])  // Translation
        );
    }

    public static void ApplyMatrixToTransform(Transform target, Matrix4x4 matrix)
    {
        // 1. Translation (localPosition)
        Vector3 translation = matrix.GetColumn(3);

        // 2. Scale (localScale)
        Vector3 scale = new Vector3(
            matrix.GetColumn(0).magnitude, // X 축 스케일
            matrix.GetColumn(1).magnitude, // Y 축 스케일
            matrix.GetColumn(2).magnitude  // Z 축 스케일
        );

        // 3. Rotation (localRotation)
        Vector3 normalizedX = matrix.GetColumn(0).normalized;
        Vector3 normalizedY = matrix.GetColumn(1).normalized;
        Vector3 normalizedZ = matrix.GetColumn(2).normalized;

        Quaternion rotation = Quaternion.LookRotation(normalizedZ, normalizedY);
        //Quaternion rotation = Quaternion.FromToRotation(Vector3.right, normalizedX) *
        //              Quaternion.FromToRotation(Vector3.up, normalizedY);

        //Quaternion rotation = matrix.rotation;


        // 4. Transform에 적용
        target.localPosition = translation;
        target.localScale = scale;
        target.localRotation = rotation;
    }
}
