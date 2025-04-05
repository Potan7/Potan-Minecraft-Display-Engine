using System.Collections.Generic;
using Animation.AnimFrame;
using BDObjectSystem;
using BDObjectSystem.Utility;
using UnityEngine;

namespace Animation
{
    public class BDObjectAnimator
    {
        public BdObjectContainer RootObject;

        public readonly Dictionary<string, BdObjectContainer> modelDict;
        public readonly Dictionary<string, Matrix4x4> parentMatrixDict = new();
        private readonly HashSet<BdObjectContainer> visitedObjects = new();
        private Transform noParentTransform;

        public BDObjectAnimator(BdObjectContainer rootObject)
        {
            RootObject = rootObject;
            modelDict = BdObjectHelper.SetDisplayIDDictionary(rootObject);

            noParentTransform = GameManager.GetManager<BdObjectManager>().bdObjectParent;

            foreach (var obj in modelDict.Values)
            {
                parentMatrixDict[obj.bdObjectID] = ComputeParentWorldMatrix(obj);
            }
        }

        #region Transformation

        /// <summary>
        /// 부모를 따라 순회하며 변환을 적용합니다.
        /// </summary>
        /// <param name="bdObj"></param>
        public void ApplyTransformation(Frame targetFrame)
        {
            // 모델의 부모 자식 구조가 다를 경우
            if (targetFrame.IsModelDiffrent)
            {
                ApplyDiffrentStructureTransform(targetFrame);
                return;
            }
            visitedObjects.Clear();

            // 자식에서 부모로 올라가면서 변환을 적용합니다.
            foreach (var obj in targetFrame.leafObjects)
            {
                if (!modelDict.TryGetValue(obj.ID, out var model)) continue;

                if (model.transform.parent == noParentTransform)
                {
                    model.transform.SetParent(model.parent.transform, true);
                }

                var modelRef = model;
                var targetRef = obj;
                while (modelRef != null && targetRef != null)
                {
                    if (visitedObjects.Contains(modelRef)) break;

                    if (modelRef.bdObjectID == targetRef.ID)
                    {
                        modelRef.SetTransformation(targetFrame.GetMatrix(targetRef.ID));
                        visitedObjects.Add(modelRef);
                    }

                    modelRef = modelRef.parent;
                    targetRef = targetRef.Parent;
                }
            }
        }

        public void ApplyTransformation(Frame aFrame, Frame bFrame, float ratio)
        {
            visitedObjects.Clear();

            // 모델의 부모 자식 구조가 다를 경우
            if (aFrame.IsModelDiffrent || bFrame.IsModelDiffrent)
            {
                // aFrame, bFrame 각각에서 해당 ID의 행렬을 가져와 보간
                ApplyDiffrentStructureTransform(aFrame, bFrame, ratio);
                return;
            }

            // aFrame의 leafObjects를 순회
            foreach (var leafA in aFrame.leafObjects)
            {
                // bFrame의 leafObjects에서 같은 ID를 가진 노드를 찾습니다.
                var leafB = bFrame.leafObjects.Find(x => x.ID == leafA.ID);
                if (leafB == null) continue;

                // 모델 사전에서 해당 노드를 찾습니다.
                if (!modelDict.TryGetValue(leafA.ID, out var model)) continue;

                if (model.transform.parent == noParentTransform)
                {
                    model.transform.SetParent(model.parent.transform, true);
                }

                var modelRef = model;
                var aRef = leafA;
                var bRef = leafB;

                while (modelRef != null && aRef != null && bRef != null)
                {
                    if (visitedObjects.Contains(modelRef))
                        break;

                    if (modelRef.bdObjectID == aRef.ID)
                    {
                        // aFrame, bFrame 각각에서 해당 ID의 행렬을 가져와 보간
                        Matrix4x4 aMatrix = aFrame.GetMatrix(aRef.ID);
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

        /// <summary>
        /// 구조가 다른 BDObject를 적용합니다.
        /// </summary>
        /// <param name="changedBDObjects"></param>
        public void ApplyDiffrentStructureTransform(Frame targetFrame)
        {
            foreach (var obj in targetFrame.leafObjects)
            {
                if (!modelDict.TryGetValue(obj.ID, out var model)) continue;

                var worldMat = targetFrame.GetWorldMatrix(obj.ID);
                if (model.transform.parent != noParentTransform)
                {
                    model.transform.SetParent(noParentTransform);
                }

                model.SetTransformation(worldMat);
            }
        }

        public void ApplyDiffrentStructureTransform(Frame aFrame, Frame bFrame, float ratio)
        {
            foreach (var obj in aFrame.leafObjects)
            {
                if (!modelDict.TryGetValue(obj.ID, out var model)) continue;

                var worldMatA = aFrame.GetWorldMatrix(obj.ID);
                var worldMatB = bFrame.GetWorldMatrix(obj.ID);

                Matrix4x4 lerpedMatrix = InterpolateMatrixTRS(worldMatA, worldMatB, ratio);

                if (model.transform.parent != noParentTransform)
                {
                    model.transform.SetParent(noParentTransform);
                }

                model.SetTransformation(lerpedMatrix);
            }
        }
        #endregion

        #region Matrix4x4 Functions

        private Matrix4x4 ComputeParentWorldMatrix(BdObjectContainer node)
        {
            Matrix4x4 result = Matrix4x4.identity;
            var current = node.parent;
            while (current != null)
            {
                result = result * current.transformation; // ← 바꿔야 할 핵심
                current = current.parent;
            }
            return result;
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
            pos = m.GetColumn(3);

            Vector3 col0 = m.GetColumn(0);
            Vector3 col1 = m.GetColumn(1);
            Vector3 col2 = m.GetColumn(2);

            // 최소값 epsilon (수치 불안정 방지용)
            const float epsilon = 1e-5f;

            // a) X축 스케일 및 정규화
            float scaleX = col0.magnitude;
            if (scaleX < epsilon) scaleX = epsilon;
            Vector3 normX = col0 / scaleX;

            // b) X-Y shear: normX와 col1의 내적
            float shearXY = Vector3.Dot(normX, col1);
            Vector3 col1NoShear = col1 - normX * shearXY;
            float scaleY = col1NoShear.magnitude;
            Vector3 normY;
            if (scaleY < epsilon)
            {
                // scaleY가 너무 작다면, normY를 normX와 col2의 외적으로 계산하여 보완
                normY = Vector3.Cross(normX, col2);
                if (normY.sqrMagnitude < epsilon)
                {
                    normY = Vector3.up; // 극한 상황의 기본값
                }
                else
                {
                    normY.Normalize();
                }
                // scaleY는 col1의 normY 성분으로 재계산
                scaleY = Mathf.Abs(Vector3.Dot(col1, normY));
                // 재구성한 col1NoShear
                col1NoShear = normY * scaleY;
            }
            else
            {
                normY = col1NoShear / scaleY;
            }

            // c) X-Z 및 Y-Z shear 제거
            float shearXZ = Vector3.Dot(normX, col2);
            float shearYZ = Vector3.Dot(normY, col2);
            Vector3 col2NoShear = col2 - normX * shearXZ - normY * shearYZ;
            float scaleZ = col2NoShear.magnitude;
            if (scaleZ < epsilon) scaleZ = epsilon;
            Vector3 normZ = col2NoShear / scaleZ;

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
        #endregion

    }
}
