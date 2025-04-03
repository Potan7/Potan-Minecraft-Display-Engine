using System;
using System.Collections.Generic;
using BDObjectSystem;
using Unity.Mathematics;
using UnityEngine;

namespace Animation.AnimFrame
{
    public partial class AnimObject
    {
        public void OnTickChanged(float tick)
        {
            if (tick <= 0.01f)
            {
                _noID.Clear();
            }

            // get left frame index
            var left = GetLeftFrame(tick);
            if (left < 0) return;
            var leftFrame = frames.Values[left];

            // 보간 없이 적용해야 하는 경우: interpolation이 0이거나, 보간 종료됐거나, 첫 프레임인 경우
            if (leftFrame.interpolation == 0 || leftFrame.tick + leftFrame.interpolation < tick || left == 0)
            {
                SetObjectTransformation(leftFrame);
            }
            else
            {
                //SetObjectTransformationInterpolation(tick, left);
            }
        }

        // 단일 Frame의 변환을 각 display에 적용 (보간 없이)
        private void SetObjectTransformation(Frame frame)
        {
            animator.ApplyTransformation(frame.leafObjects);
            
        }

        // // 두 Frame(a, b) 사이에서 보간 적용 (보간 점프 처리를 포함)
        // private void SetObjectTransformationInterpolation(float tick, int indexOf)
        // {
        //     Frame a = frames.Values[indexOf - 1];
        //     Frame b = frames.Values[indexOf];

        //     // b 프레임 기준 보간 비율 t 계산 (0~1로 클램프)
        //     float t = Mathf.Clamp01((tick - b.tick) / b.interpolation);

        //     foreach (var display in _displayList)
        //     {
        //         // 보간 시 시작 상태를 결정할 때,
        //         bool aExists = a.worldTransforms.TryGetValue(display.bdObjectID, out var aData);
        //         bool bExists = b.worldTransforms.TryGetValue(display.bdObjectID, out var bData);

        //         if (!aExists && !bExists)
        //         {
        //             if (_noID.Contains(display.bdObjectID))
        //                 continue;
        //             CustomLog.LogError("Target not found, name : " + display.bdObjectID);
        //             _noID.Add(display.bdObjectID);
        //             continue;
        //         }

        //         Matrix4x4 childTransform;
        //         if (!aExists)
        //         {
        //             childTransform = bData;
        //         }
        //         else if (!bExists)
        //         {
        //             childTransform = aData;
        //         }
        //         else
        //         {
        //             // A->B 보간하기

        //             // 이때 보간점프일 수 있으므로, GetFrameRealMatrix를 사용하여 aData 계산
        //             aData = GetFrameRealMatrix(indexOf - 1, b.tick, display.bdObjectID);
        //             childTransform = InterpolateMatrixTRS(aData, bData, t);

        //         }
        //         // display에 변환 적용
        //         //display.SetTransformation(childTransform);
        //     }
        // }

        // // 주어진 Frame의 변환을 계산하여 반환
        // // 이때 해당 프레임이 보간 점프라면 이전 프레임을 사용하여 보간 점프를 계산함
        // // 그 이전도 보간 점프일 수 있으니 재귀 함수를 사용
        // private Matrix4x4 GetFrameRealMatrix(int idx, float tick, string ID)
        // {
        //     if (idx < 0) return Matrix4x4.identity;
        //     Frame frame = frames.Values[idx];

        //     if (!frame.worldTransforms.TryGetValue(ID, out var worldTransform))
        //     {
        //         return Matrix4x4.identity;
        //     }

        //     if (frame.interpolation == 0 || frame.tick + frame.interpolation < tick || idx == 0)
        //     {
        //         // 보간 없이 적용해야 하는 경우: interpolation이 0이거나, 보간 종료됐거나, 첫 프레임인 경우
        //         return worldTransform;
        //     }
        //     else
        //     {
        //         float t = Mathf.Clamp01((tick - frame.tick) / frame.interpolation);

        //         Matrix4x4 newAData = GetFrameRealMatrix(idx - 1, frame.tick, ID);
        //         return InterpolateMatrixTRS(newAData, worldTransform, t);
        //     }
        // }

        // private static Matrix4x4 InterpolateMatrixTRS(in Matrix4x4 a, in Matrix4x4 b, float t)
        // {
        //     // 1) 위치(Translation) 보간
        //     Vector3 posA = a.GetColumn(3);
        //     Vector3 posB = b.GetColumn(3);
        //     Vector3 pos = Vector3.Lerp(posA, posB, t);

        //     // 2) 회전(Rotation) 보간
        //     Quaternion rotA = a.rotation;
        //     Quaternion rotB = b.rotation;
        //     Quaternion rot = Quaternion.Slerp(rotA, rotB, t);

        //     // 3) 스케일(Scale) 보간
        //     Vector3 scaleA = new Vector3(a.GetColumn(0).magnitude,
        //                                  a.GetColumn(1).magnitude,
        //                                  a.GetColumn(2).magnitude);

        //     Vector3 scaleB = new Vector3(b.GetColumn(0).magnitude,
        //                                  b.GetColumn(1).magnitude,
        //                                  b.GetColumn(2).magnitude);

        //     Vector3 scale = Vector3.Lerp(scaleA, scaleB, t);

        //     // 4) 최종 TRS 행렬 재구성
        //     return Matrix4x4.TRS(pos, rot, scale);
        // }

        // 현재 tick에 맞는 왼쪽 프레임의 인덱스를 찾음 (binary search)
        private int GetLeftFrame(float tick)
        {
            tick = (int)tick;
            if (frames.Values[0].tick > tick)
                return -1;

            var left = 0;
            var right = frames.Count - 1;
            var keys = frames.Keys;
            var idx = -1;

            while (left <= right)
            {
                var mid = (left + right) / 2;
                if (keys[mid] <= tick)
                {
                    idx = mid;
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }
            return idx >= 0 ? idx : -1;
        }
    }
}
