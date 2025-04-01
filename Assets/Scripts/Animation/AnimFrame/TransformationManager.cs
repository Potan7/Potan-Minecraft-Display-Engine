using System;
using System.Collections.Generic;
using BDObjectSystem;
using BDObjectSystem.Utility;
using UnityEngine;

namespace Animation.AnimFrame
{
    public class TransformationManager
    {
        private readonly HashSet<string> _noID = new HashSet<string>();

        private readonly SortedList<int, Frame> _frames;
        // 이제 _displayList는 AnimModel이 아닌 BDObjectContainer의 Leaf 노드 리스트임.
        private readonly List<BdObjectContainer> _displayList;
        // 각 display의 효과적인(실제 화면에 반영된) 월드 변환 상태를 저장하는 캐시
        private readonly Dictionary<string, Matrix4x4> _effectiveTransforms = new Dictionary<string, Matrix4x4>();

        public TransformationManager(SortedList<int, Frame> frames, List<BdObjectContainer> displayList)
        {
            _frames = frames;
            _displayList = displayList;
        }

        public void OnTickChanged(float tick)
        {
            if (tick <= 0.01f)
            {
                _noID.Clear();
                _effectiveTransforms.Clear(); // 초기화 시 캐시도 비움
            }

            // 왼쪽 프레임 인덱스 검색
            var left = GetLeftFrame(tick);
            if (left < 0) return;
            var leftFrame = _frames.Values[left];

            // 보간 없이 적용해야 하는 경우
            if (leftFrame.interpolation == 0 || leftFrame.tick + leftFrame.interpolation < tick || left == 0)
            {
                SetObjectTransformation(leftFrame);
            }
            else
            {
                SetObjectTransformationInterpolation(tick, left);
            }
        }

        // 단일 프레임의 변환을 각 leaf 노드(BdObjectContainer)에 적용 (보간 없이)
        private void SetObjectTransformation(Frame frame)
        {
            foreach (var display in _displayList)
            {
                // BDObjectContainer의 식별자는 bdObjectID 혹은 display.BdObject.ID를 사용
                string id = display.bdObjectID;
                if (!frame.worldTransforms.TryGetValue(id, out var worldTransform))
                {
                    if (_noID.Contains(id))
                        continue;
                    CustomLog.LogError("Target not found, name : " + id);
                    _noID.Add(id);
                    continue;
                }

                // 현재 부모의 월드 행렬로부터 Local 행렬 계산
                Matrix4x4 parentWorld = display.transform.parent != null ? display.transform.parent.localToWorldMatrix : Matrix4x4.identity;
                Matrix4x4 localTransform = parentWorld.inverse * worldTransform;

                // Local 변환을 적용 (AffineTransformation은 TRS 분해 후 local Transform에 적용)
                AffineTransformation.ApplyMatrixToTransform(display.transform, localTransform);

                // 캐시 업데이트 (world transform 저장)
                _effectiveTransforms[id] = worldTransform;
            }
        }

        // 두 프레임(a, b) 사이 보간 적용 (보간 점프 처리를 포함)
        private void SetObjectTransformationInterpolation(float tick, int indexOf)
        {
            Frame a = _frames.Values[indexOf - 1];
            Frame b = _frames.Values[indexOf];

            // b 프레임 기준 보간 비율 계산 (0~1)
            float t = Mathf.Clamp01((tick - b.tick) / b.interpolation);

            foreach (var display in _displayList)
            {
                string id = display.bdObjectID;
                Matrix4x4 aData;
                bool aExists = _effectiveTransforms.TryGetValue(id, out aData);
                if (!aExists)
                {
                    // 캐시에 없으면 a 프레임 원본 데이터 사용
                    aExists = a.worldTransforms.TryGetValue(id, out aData);
                }

                bool bExists = b.worldTransforms.TryGetValue(id, out var bData);

                if (!aExists && !bExists)
                {
                    if (_noID.Contains(id))
                        continue;
                    CustomLog.LogError("Target not found, name : " + id);
                    _noID.Add(id);
                    continue;
                }

                Matrix4x4 childTransform;
                if (!aExists)
                {
                    childTransform = bData;
                }
                else if (!bExists)
                {
                    childTransform = aData;
                }
                else
                {
                    // 보간 시 A->B 변환 계산 (보간점프 고려)
                    aData = GetFrameRealMatrix(indexOf - 1, b.tick, id);
                    childTransform = InterpolateMatrixTRS(aData, bData, t);
                }

                // childTransform은 월드 좌표계에서의 목표 변환.
                // 현재 부모 기준 Local 변환으로 변경하여 적용
                Matrix4x4 parentWorld = display.transform.parent != null ? display.transform.parent.localToWorldMatrix : Matrix4x4.identity;
                Matrix4x4 localTransform = parentWorld.inverse * childTransform;
                AffineTransformation.ApplyMatrixToTransform(display.transform, localTransform);

                // 캐시 업데이트 (world transform 저장)
                _effectiveTransforms[id] = childTransform;
            }
        }

        // 재귀적으로 프레임의 실제(world) 변환 행렬 계산 (보간 점프 고려)
        private Matrix4x4 GetFrameRealMatrix(int idx, float tick, string ID)
        {
            if (idx < 0) return Matrix4x4.identity;
            Frame frame = _frames.Values[idx];

            if (!frame.worldTransforms.TryGetValue(ID, out var worldTransform))
            {
                return Matrix4x4.identity;
            }

            if (frame.interpolation == 0 || frame.tick + frame.interpolation < tick || idx == 0)
            {
                return worldTransform;
            }
            else
            {
                float t = Mathf.Clamp01((tick - frame.tick) / frame.interpolation);
                Matrix4x4 newAData = GetFrameRealMatrix(idx - 1, frame.tick, ID);
                return InterpolateMatrixTRS(newAData, worldTransform, t);
            }
        }

        // TRS 방식으로 두 행렬 사이를 보간
        private static Matrix4x4 InterpolateMatrixTRS(in Matrix4x4 a, in Matrix4x4 b, float t)
        {
            // 1) Translation 보간
            Vector3 posA = a.GetColumn(3);
            Vector3 posB = b.GetColumn(3);
            Vector3 pos = Vector3.Lerp(posA, posB, t);

            // 2) Rotation 보간
            Quaternion rotA = a.rotation;
            Quaternion rotB = b.rotation;
            Quaternion rot = Quaternion.Slerp(rotA, rotB, t);

            // 3) Scale 보간
            Vector3 scaleA = new Vector3(
                a.GetColumn(0).magnitude,
                a.GetColumn(1).magnitude,
                a.GetColumn(2).magnitude);
            Vector3 scaleB = new Vector3(
                b.GetColumn(0).magnitude,
                b.GetColumn(1).magnitude,
                b.GetColumn(2).magnitude);
            Vector3 scale = Vector3.Lerp(scaleA, scaleB, t);

            // 4) 최종 TRS 행렬 재구성
            return Matrix4x4.TRS(pos, rot, scale);
        }

        // 현재 tick에 맞는 왼쪽 프레임 인덱스 찾기 (이진 탐색)
        private int GetLeftFrame(float tick)
        {
            tick = (int)tick;
            if (_frames.Values[0].tick > tick)
                return -1;

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
            return idx >= 0 ? idx : -1;
        }
    }
}
