using System.Collections.Generic;
using BDObjectSystem;
using BDObjectSystem.Utility;
using UnityEngine;

public class BDObjectAnimator
{
    public BdObjectContainer RootObject;

    public readonly Dictionary<string, BdObjectContainer> modelDict;
    private readonly HashSet<BdObjectContainer> visitedObjects = new();

    public BDObjectAnimator(BdObjectContainer rootObject)
    {
        RootObject = rootObject;
        modelDict = BdObjectHelper.SetDisplayIDDictionary(rootObject);

    }

    /// <summary>
    /// 부모를 따라 순회하며 변환을 적용합니다.
    /// </summary>
    /// <param name="bdObj"></param>
    public void ApplyTransformation(List<BdObject> target)
    {
        visitedObjects.Clear();
        // 자식에서 부모로 올라가면서 변환을 적용합니다.
        foreach (var obj in target)
        {
            if (!modelDict.TryGetValue(obj.ID, out var model)) continue;

            var modelRef = model;
            var targetRef = obj;
            while (modelRef != null && targetRef != null)
            {
                if (visitedObjects.Contains(modelRef)) break;

                if (modelRef.bdObjectID == targetRef.ID)
                {
                    modelRef.SetTransformation(targetRef.Transforms);
                    visitedObjects.Add(modelRef);
                }

                modelRef = modelRef.parent;
                targetRef = targetRef.Parent;
            }
        }
    }

    public void ApplyTransformation(List<BdObject> a, List<BdObject> b, float ratio)
    {
        visitedObjects.Clear();
        // 자식에서 부모로 올라가면서 변환을 적용합니다.
        for (int i = 0; i < a.Count; i++)
        {
            if (!modelDict.TryGetValue(a[i].ID, out var model)) continue;

            // 부모 올라가면서 변환적용
            var modelRef = model;
            var aRef = a[i];
            var bRef = b[i];
            while (modelRef != null && aRef != null && bRef != null)
            {
                if (visitedObjects.Contains(modelRef)) break;

                if (modelRef.bdObjectID == aRef.ID)
                {
                    Matrix4x4 aMatrix = aRef.Transforms.GetMatrix();
                    Matrix4x4 bMatrix = bRef.Transforms.GetMatrix();
                    Matrix4x4 lerpedMatrix = InterpolateMatrixTRS(aMatrix, bMatrix, ratio);

                    modelRef.SetTransformation(lerpedMatrix);
                    visitedObjects.Add(modelRef);
                }

                modelRef = modelRef.parent;
                aRef = aRef.Parent;
                bRef = bRef.Parent;
            }
        }
    }

    public static Matrix4x4 InterpolateMatrixTRS(in Matrix4x4 a, in Matrix4x4 b, float t)
    {
        // 두 행렬을 TRS 성분으로 분해 (shear 제거 포함)
        DecomposeMatrix(a, out Vector3 posA, out Quaternion rotA, out Vector3 scaleA);
        DecomposeMatrix(b, out Vector3 posB, out Quaternion rotB, out Vector3 scaleB);

        // Translation 보간
        Vector3 pos = Vector3.Lerp(posA, posB, t);
        // Rotation 보간 (Quaternion Slerp)
        Quaternion rot = Quaternion.Slerp(rotA, rotB, t);
        // Scale 보간
        Vector3 scale = Vector3.Lerp(scaleA, scaleB, t);

        // 보간된 TRS 성분으로 행렬 재구성
        return Matrix4x4.TRS(pos, rot, scale);
    }



    public static void DecomposeMatrix(in Matrix4x4 m, out Vector3 pos, out Quaternion rot, out Vector3 scale)
    {
        // 1) Translation: 4번째 컬럼
        pos = m.GetColumn(3);

        // 2) 3x3 부분 추출 (회전, 스케일, shear가 섞여 있음)
        Vector3 col0 = m.GetColumn(0);
        Vector3 col1 = m.GetColumn(1);
        Vector3 col2 = m.GetColumn(2);

        // a) X축 스케일 및 정규화
        float scaleX = col0.magnitude;
        Vector3 normX = (scaleX != 0f) ? col0 / scaleX : Vector3.zero;

        // b) X-Y shear: normX와 col1의 내적
        float shearXY = Vector3.Dot(normX, col1);
        // col1에서 normX 방향의 shear 성분 제거
        Vector3 col1NoShear = col1 - normX * shearXY;
        float scaleY = col1NoShear.magnitude;
        Vector3 normY = (scaleY != 0f) ? col1NoShear / scaleY : Vector3.zero;

        // c) X-Z 및 Y-Z shear: normX, normY와 col2의 내적
        float shearXZ = Vector3.Dot(normX, col2);
        float shearYZ = Vector3.Dot(normY, col2);
        // col2에서 normX, normY 방향의 shear 성분 제거
        Vector3 col2NoShear = col2 - normX * shearXZ - normY * shearYZ;
        float scaleZ = col2NoShear.magnitude;
        Vector3 normZ = (scaleZ != 0f) ? col2NoShear / scaleZ : Vector3.zero;

        // 최종 스케일
        scale = new Vector3(scaleX, scaleY, scaleZ);

        // 순수 회전 행렬 구성 (shear 제거된 정규화된 축)
        Matrix4x4 pureRotation = new Matrix4x4();
        pureRotation.SetColumn(0, new Vector4(normX.x, normX.y, normX.z, 0));
        pureRotation.SetColumn(1, new Vector4(normY.x, normY.y, normY.z, 0));
        pureRotation.SetColumn(2, new Vector4(normZ.x, normZ.y, normZ.z, 0));
        pureRotation.SetColumn(3, new Vector4(0, 0, 0, 1));

        // Quaternion으로 변환 (이미 정규화된 회전 행렬)
        rot = AffineTransformation.QuaternionFromMatrix(pureRotation);
    }


    /// <summary>
    /// 구조가 다른 BDObjectContainer를 적용합니다.
    /// 들어온 displayList를 순회하며 각 오브젝트에 부모 변환을 무시하고 적용합니다.
    /// </summary>
    /// <param name="changedBDObjects"></param>
    public void ApplyDiffrentStructureTransform(Dictionary<string, BdObjectContainer> changedBDObjects)
    {

    }


}
