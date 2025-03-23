using System;
using System.Collections.Generic;
using BDObjectSystem;
using Unity.Mathematics;
using UnityEngine;

namespace Animation.AnimFrame
{
    public class TransformationManager
    {
        private readonly HashSet<string> _noID = new HashSet<string>();
        private readonly HashSet<string> _visitedNodes = new HashSet<string>();

        private readonly SortedList<int, Frame> _frames;
        private readonly List<AnimModel> _displayList;

        public TransformationManager(SortedList<int, Frame> frames, List<AnimModel> displayList)
        {
            _frames = frames;
            _displayList = displayList;

        }

        public void OnTickChanged(float tick)
        {
            if (tick <= 0.01f)
                _noID.Clear();

            // get left frame
            var left = GetLeftFrame(tick);
            if (left < 0) return;
            var leftFrame = _frames.Values[left];

            // no interpolation
            if (leftFrame.interpolation == 0 || leftFrame.tick + leftFrame.interpolation <= tick || left == 0)
            {
                // SetObjectTransformation(_root.BdObject.ID, leftFrame.Info);
                SetObjectTransformation(leftFrame);
            }
            else
            {
                // interpolation ratio
                var t = (tick - leftFrame.tick) / leftFrame.interpolation;

                SetObjectTransformationInterpolation(t, left);
            }
        }


        // 보간 없이, 단일 Frame에서 변환 적용
        private void SetObjectTransformation(Frame frame)
        {
            foreach (var display in _displayList)
            {
                // 1) 현재 display에 해당하는 데이터 찾기
                if (!frame.worldTransforms.TryGetValue(display.ID, out var worldTransform))
                {

                    // 한 번도 없는 bdObjectID라면 로그만 찍고 넘어감
                    if (_noID.Contains(display.ID))
                        continue;

                    CustomLog.LogError("Target not found, name : " + display.ID);
                    _noID.Add(display.ID);
                    continue;
                }

                // 2) 현재 display의 변환 적용
                display.SetTransformation(worldTransform);

                // 3) 부모 노드 변환 순차 적용
                //ApplyChainTransformations(idData.Parent, display.Parent, _visitedNodes);
            }

            _visitedNodes.Clear();
        }

        // -----------------------------------------------------------
        // 두 Frame(a, b) 사이에서 t만큼 보간하여 변환 적용
        private void SetObjectTransformationInterpolation(float t, int IndexOf)
        {
            Frame a = _frames.Values[IndexOf - 1];
            Frame b = _frames.Values[IndexOf];

            foreach (var display in _displayList)
            {
                // var aContains = a.IDDataDict.TryGetValue(display.bdObjectID, out var aData);
                // var bContains = b.IDDataDict.TryGetValue(display.bdObjectID, out var bData);
                var aContains = a.worldTransforms.TryGetValue(display.ID, out var aData);
                var bContains = b.worldTransforms.TryGetValue(display.ID, out var bData);

                // a, b 어느 쪽에도 없으면 스킵
                if (!aContains && !bContains)
                {
                    if (_noID.Contains(display.ID))
                        continue;

                    CustomLog.LogError("Target not found, name : " + display.ID);
                    _noID.Add(display.ID);
                    continue;
                }

                // 1) 자식(현재 display) 자체 변환 계산
                Matrix4x4 childTransform;
                if (!aContains)
                {
                    // aFrame에는 없고 bFrame에만 있다면 bTransform 그대로
                    childTransform = bData;
                }
                else if (!bContains)
                {
                    // bFrame에는 없고 aFrame에만 있다면 aTransform 그대로
                    childTransform = aData;
                }
                else
                {
                    var newAData = aData;

                    // a, b 모두 있으니 보간
                    childTransform = InterpolateMatrixTRS(newAData, bData, t);
                }

                // display에 설정
                display.SetTransformation(childTransform);
            }

            _visitedNodes.Clear();
        }

        // -----------------------------------------------------------
        // 행렬(혹은 float[16]) 보간 메서드
        // private static float[] InterpolateTransforms(float[] aMatrix, float[] bMatrix, float t)
        // {
        //     // 길이 16의 두 행렬 a, b를 원소별 선형 보간
        //     var result = new float[16];
        //     var invT = 1f - t;
        //     for (var i = 0; i < 16; i++)
        //     {
        //         result[i] = aMatrix[i] * invT + bMatrix[i] * t;
        //     }
        //     return result;
        // }

        private static Matrix4x4 InterpolateMatrixTRS(in Matrix4x4 a, in Matrix4x4 b, float t)
        {
            // 1) 위치(Translation) 보간
            Vector3 posA = a.GetColumn(3);
            Vector3 posB = b.GetColumn(3);
            Vector3 pos = Vector3.Lerp(posA, posB, t);

            // 2) 회전(Rotation) 보간 - Unity 제공 프로퍼티 사용
            Quaternion rotA = a.rotation;
            Quaternion rotB = b.rotation;
            Quaternion rot = Quaternion.Slerp(rotA, rotB, t);

            // 3) 스케일(Scale) 보간
            Vector3 scaleA = new Vector3(a.GetColumn(0).magnitude,
                                         a.GetColumn(1).magnitude,
                                         a.GetColumn(2).magnitude);

            Vector3 scaleB = new Vector3(b.GetColumn(0).magnitude,
                                         b.GetColumn(1).magnitude,
                                         b.GetColumn(2).magnitude);

            Vector3 scale = Vector3.Lerp(scaleA, scaleB, t);

            // 4) 최종 TRS 행렬로 재구성
            return Matrix4x4.TRS(pos, rot, scale);
        }


        // find left frame by tick
        private int GetLeftFrame(float tick)
        {
            tick = (int)tick;
            // 1. if tick is smaller than first frame (<0)
            if (_frames.Values[0].tick > tick)
                return -1;

            // 2. binary search
            var left = 0;
            var right = _frames.Count - 1;
            var keys = _frames.Keys;
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

            // 3. return found index
            if (idx >= 0)
            {
                return idx;
            }

            return -1;
        }
    }
}