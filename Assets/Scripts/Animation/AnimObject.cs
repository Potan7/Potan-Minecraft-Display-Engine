using System;
using System.Collections.Generic;
using BDObjectSystem;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Animation
{
    public class AnimObject : MonoBehaviour
    {
        public RectTransform rect;
        public TextMeshProUGUI title;
        public Frame firstFrame;
        
        private SortedList<int, Frame> _frames = new SortedList<int, Frame>();
        public string bdFileName;

        private readonly HashSet<string> _noID = new HashSet<string>();

        private int MaxTick => _frames.Count == 0 ? 0 : _frames.Values[_frames.Count - 1].tick;

        private AnimObjList _manager;

        private BdObjectContainer _root;
        // private Dictionary<string, BdObjectContainer> _idDict;
        private List<BdObjectContainer> _displayList;
        private readonly HashSet<string> _visitedNodes = new HashSet<string>();

        public void Init(string fileName, AnimObjList list)
        {
            title.text = fileName;
            _manager = list;
            bdFileName = fileName;

            var bdObject = GameManager.GetManager<BdObjectManager>().BdObjects[fileName];
            _root = bdObject.Item1;
            // _idDict = bdObject.Item2;
            _displayList = bdObject.Item2;
            
            GetTickAndInterByFileName(bdFileName, out _, out var inter);
            firstFrame.Init(bdFileName, 0, inter, _root.BdObject, this, _manager.timeline);

            _frames[0] = firstFrame;

            AnimManager.TickChanged += OnTickChanged;
        }

        #region Transform

        private void OnTickChanged(int tick)
        {
            if (tick == 0)
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
                var t = (float)(tick - leftFrame.tick) / leftFrame.interpolation;

                // get before frame
                var before = _frames.Values[left - 1];
                SetObjectTransformationInterpolation(t, before, leftFrame);
            }
        }

        /// <summary>
        /// 기존에는 모든 BDObj가 Dict에 ID를 key로 저장되서 Root부터 children을 돌며 내려감
        /// 하지만 지금은 dict에 최하단 leaf 노드만 저장됨
        /// dict를 순회하면서 위로 올라가는 방식
        /// 그러면 그냥 list로 바꿔도 될듯?
        /// </summary>
        
         // 보간 없이, 단일 Frame에서 변환 적용
        private void SetObjectTransformation(Frame frame)
        {
            foreach (var display in _displayList)
            {
                // 1) 현재 display에 해당하는 데이터 찾기
                if (!frame.IDDataDict.TryGetValue(display.bdObjectID, out var idData))
                {
                    // 한 번도 없는 bdObjectID라면 로그만 찍고 넘어감
                    if (_noID.Contains(display.bdObjectID)) 
                        continue;

                    CustomLog.LogError("Target not found, name : " + display.bdObjectID);
                    _noID.Add(display.bdObjectID);
                    continue;
                }

                // 2) 현재 display의 변환 적용
                display.SetTransformation(idData.Transforms);

                // 3) 부모 노드 변환 순차 적용
                ApplyChainTransformations(idData.Parent, display.Parent, _visitedNodes);
            }

            _visitedNodes.Clear();
        }

        // 부모 체인을 따라가며 변환을 적용하는 유틸 메서드
        private static void ApplyChainTransformations(BdObject parentData, BdObjectContainer parentDisplay, HashSet<string> visited)
        {
            while (parentData != null && parentDisplay is not null)
            {
                // 이미 방문한 부모 노드면 중단 (중복 처리 방지)
                if (visited.Contains(parentData.ID))
                    break;

                // 부모 변환 적용
                parentDisplay.SetTransformation(parentData.Transforms);

                // 방문 표시
                visited.Add(parentData.ID);

                // 한 칸 더 위로
                parentData = parentData.Parent;
                parentDisplay = parentDisplay.Parent;
            }
        }

        // -----------------------------------------------------------
        // 두 Frame(a, b) 사이에서 t만큼 보간하여 변환 적용
        private void SetObjectTransformationInterpolation(float t, Frame a, Frame b)
        {
            foreach (var display in _displayList)
            {
                var aContains = a.IDDataDict.TryGetValue(display.bdObjectID, out var aData);
                var bContains = b.IDDataDict.TryGetValue(display.bdObjectID, out var bData);

                // a, b 어느 쪽에도 없으면 스킵
                if (!aContains && !bContains)
                {
                    if (_noID.Contains(display.bdObjectID)) 
                        continue;

                    CustomLog.LogError("Target not found, name : " + display.bdObjectID);
                    _noID.Add(display.bdObjectID);
                    continue;
                }

                // 1) 자식(현재 display) 자체 변환 계산
                float[] childTransform;
                if (!aContains)
                {
                    // aFrame에는 없고 bFrame에만 있다면 bTransform 그대로
                    childTransform = bData.Transforms;
                }
                else if (!bContains)
                {
                    // bFrame에는 없고 aFrame에만 있다면 aTransform 그대로
                    childTransform = aData.Transforms;
                }
                else
                {
                    // a, b 모두 있으니 보간
                    childTransform = InterpolateTransforms(aData.Transforms, bData.Transforms, t);
                }

                // display에 설정
                display.SetTransformation(childTransform);

                // 2) 부모 노드 보간 적용
                var aParent = aData?.Parent;
                var bParent = bData?.Parent;
                ApplyChainTransformationsInterpolation(t, aParent, bParent, display.Parent, _visitedNodes);
            }

            _visitedNodes.Clear();
        }

        // 부모 체인을 따라가며 보간 변환을 적용하는 유틸 메서드
        private static void ApplyChainTransformationsInterpolation(float t, BdObject aParent, BdObject bParent, BdObjectContainer parentDisplay, HashSet<string> visited)
        {
            while ((aParent != null || bParent != null) && parentDisplay is not null)
            {
                // 부모 노드 ID를 구한다. (하나라도 있으면 그걸로)
                // - 보통 aParent.ID == bParent.ID라 가정
                var parentId = aParent?.ID ?? bParent?.ID;
                if (parentId != null && visited.Contains(parentId))
                    break;

                float[] finalTransform;
                if (aParent != null && bParent != null)
                {
                    finalTransform = InterpolateTransforms(aParent.Transforms, bParent.Transforms, t);
                }
                else if (aParent != null)
                {
                    finalTransform = aParent.Transforms;
                }
                else
                {
                    // bParent != null 인 경우
                    finalTransform = bParent.Transforms;
                }

                // 부모 Display에 설정
                parentDisplay.SetTransformation(finalTransform);

                if (parentId != null)
                    visited.Add(parentId);

                // 체인 업
                aParent = aParent?.Parent;
                bParent = bParent?.Parent;
                parentDisplay = parentDisplay.Parent;
            }
        }

        // -----------------------------------------------------------
        // 행렬(혹은 float[16]) 보간 메서드
        private static float[] InterpolateTransforms(float[] aMatrix, float[] bMatrix, float t)
        {
            // 길이 16의 두 행렬 a, b를 원소별 선형 보간
            var result = new float[16];
            var invT = 1f - t;
            for (var i = 0; i < 16; i++)
            {
                result[i] = aMatrix[i] * invT + bMatrix[i] * t;
            }
            return result;
        }

        // find left frame by tick
        private int GetLeftFrame(int tick)
        {

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

        #endregion

        #region EditFrame

        // mouse click event
        public void OnEventTriggerClick(BaseEventData eventData)
        {
            // right click
            if (eventData is PointerEventData { button: PointerEventData.InputButton.Right } pointerData)
            {
                //Debug.Log("Right Click");
                var line = _manager.timeline.GetTickLine(pointerData.position);
                GameManager.GetManager<ContextMenuManager>().ShowContextMenu(this, line.Tick);
            }
        }

        // add frame with tick and inter
        public void AddFrame(string fileName, BdObject frameInfo, int tick, int inter)
        {
            //Debug.Log("fileName : " + fileName + ", tick : " + tick + ", inter : " + inter);

            var frame = Instantiate(_manager.framePrefab, transform.GetChild(0));

            // if already exists, tick increment
            while (_frames.ContainsKey(tick))
            {
                tick++;
            }

            _frames.Add(tick, frame);
            frame.Init(fileName, tick, inter, frameInfo, this, _manager.timeline);
        }

        // add frame with fileName
        public void AddFrame(BdObject frameInfo, string fileName)
        {
            CustomLog.Log("AddFrame : " + fileName);    
            GetTickAndInterByFileName(fileName, out var tick, out var inter);
            AddFrame(fileName, frameInfo, tick, inter);
        }

        // get tick and inter from fileName
        private void GetTickAndInterByFileName(string fileName, out int tick, out int inter)
        {
            var setting = GameManager.Setting;

            // default setting
            tick = MaxTick;
            inter = setting.defaultInterpolation;

            var fileManager = GameManager.GetManager<FileManager>();

            // frame.txt 쓴다면
            if (setting.UseFrameTxtFile)
            {
                var frame = BdObjectHelper.ExtractFrame(fileName, "f");
                if (!string.IsNullOrEmpty(frame))
                {
                    if (fileManager.FrameInfo.TryGetValue(frame, out var info))
                    {
                        tick += info.Item1;
                        inter = info.Item2;
                        return;
                    }
                }
            }

            // if using name info extract
            if (setting.UseNameInfoExtract)
            {
                var sValue = BdObjectHelper.ExtractNumber(fileName, "s", setting.defaultTickInterval);
                inter = BdObjectHelper.ExtractNumber(fileName, "i", inter);

                if (sValue > 0)
                    tick += sValue;
            }
            else
            {
                // using default setting
                tick += setting.defaultTickInterval;
            }
        }

        // remove frame
        public void RemoveFrame(Frame frame)
        {
            if (_frames == null) return;

            _frames.Remove(frame.tick);
            Destroy(frame.gameObject);

            if (_frames.Count == 0)
            {
                RemoveAnimObj();
            }
            else if (frame.tick == 0)
            {
                _frames.Values[0].SetTick(0);
            }

        }

        // remove self
        public void RemoveAnimObj()
        {
            AnimManager.TickChanged -= OnTickChanged;
            var frame = _frames;
            _frames = null;
            while (frame.Count > 0)
            {
                frame.Values[0].RemoveFrame();
                Destroy(frame.Values[0].gameObject);
                frame.RemoveAt(0);
            }
            _manager.RemoveAnimObject(this);
        }

        // change frame's tick
        public bool ChangePos(Frame frame, int firstTick, int changedTick)
        {
            //Debug.Log("firstTick : " + firstTick + ", changedTick : " +  changedTick);
            if (firstTick == changedTick) return true;
            
            // if already exists, return false
            if (_frames.ContainsKey(changedTick)) return false;

            _frames.Remove(firstTick);
            _frames.Add(changedTick, frame);

            OnTickChanged(GameManager.GetManager<AnimManager>().Tick);
            return true;
        }

        //bool CheckBDObject(BDObject a, BDObject b, bool IsFirst = false)
        //{
        //    //Debug.Log(a?.ToString() + " vs " + b?.ToString());

        //    // 1) �� �� null�̸� "����"�� ����
        //    if (a == null && b == null)
        //    {
        //        //CustomLog.Log("Both objects are null �� Considered equal.");
        //        return true;
        //    }

        //    // 2) �� �ʸ� null�̸� �ٸ�
        //    if (a == null || b == null)
        //    {
        //        CustomLog.LogError($"One object is null -> a: {(a == null ? "null" : a.name)}, b: {(b == null ? "null" : b.name)}");
        //        return false;
        //    }

        //    // 3) name�� �ٸ��� �ٷ� false
        //    if (a.name != b.name && !IsFirst)
        //    {
        //        CustomLog.LogError($"Different Name -> a: {a.name}, b: {b.name}");
        //        return false;
        //    }

        //    // 4) children ��
        //    if (a.children == null && b.children == null)
        //    {
        //        //CustomLog.Log($"Both '{a.name}' and '{b.name}' have no children �� Considered equal.");
        //        return true;
        //    }

        //    if (a.children == null || b.children == null)
        //    {
        //        CustomLog.LogError($"Children mismatch -> a: {(a.children == null ? "null" : "exists")}, b: {(b.children == null ? "null" : "exists")}");
        //        return false;
        //    }

        //    // ���̰� �ٸ��� false
        //    if (a.children.Length != b.children.Length)
        //    {
        //        CustomLog.LogError($"Children count mismatch -> a: {a.children.Length}, b: {b.children.Length}");
        //        CustomLog.LogError($"a: {string.Join(", ", a.children.Select(c => c.name))}");
        //        CustomLog.LogError($"b: {string.Join(", ", b.children.Select(c => c.name))}");
        //        return false;
        //    }

        //    // ������ �ڽ��� ��������� ��
        //    for (int i = 0; i < a.children.Length; i++)
        //    {
        //        if (!CheckBDObject(a.children[i], b.children[i]))
        //        {
        //            CustomLog.LogError($"Child mismatch at index {i} �� a: {a.children[i]?.name}, b: {b.children[i]?.name}");
        //            return false;
        //        }
        //    }

        //    // ���� ��� �˻縦 ����ϸ� true
        //    return true;
        //}
        #endregion
    }
}
