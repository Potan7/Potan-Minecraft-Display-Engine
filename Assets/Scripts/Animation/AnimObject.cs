using System;
using System.Collections.Generic;
using BDObject;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Animation
{
    public class AnimObject : MonoBehaviour
    {
        public RectTransform rect;
        public TextMeshProUGUI title;
        public Frame firstFrame;
        private SortedList<int, Frame> _frames = new SortedList<int, Frame>();
        [FormerlySerializedAs("fileName")] public string bdFileName;

        private readonly HashSet<string> _noID = new HashSet<string>();

        private int MaxTick
        {
            get
            {
                if (_frames.Count == 0)
                {
                    return 0;
                }
                return _frames.Values[_frames.Count - 1].tick;
            }
        }

        private AnimObjList _manager;

        private BdObjectContainer _root;
        private Dictionary<string, BdObjectContainer> _idDict;

        public void Init(string fileName, AnimObjList list)
        {
            title.text = fileName;
            _manager = list;
            bdFileName = fileName;

            var bdObject = GameManager.GetManager<BdObjectManager>().BdObjects[fileName];
            _root = bdObject.Item1;
            _idDict = bdObject.Item2;

            GetTickAndInterByFileName(bdFileName, out _, out var inter);
            firstFrame.Init(bdFileName, 0, inter, _root.BdObject, this);


            _frames[0] = firstFrame;

            AnimManager.TickChanged += OnTickChanged;

        }

        #region Transform

        private void OnTickChanged(int tick)
        {
            if (tick == 0)
                _noID.Clear();

            // ƽ�� �´� �������� ã��
            var left = GetLeftFrame(tick);
            if (left < 0) return;
            var leftFrame = _frames.Values[left];

            // ù��° �������� ������ ������
            if (leftFrame.interpolation == 0 || leftFrame.tick + leftFrame.interpolation <= tick || left == 0) 
            {

                // ���� ���� ����
                SetObjectTransformation(_root.BdObject.ID, leftFrame.Info);
            }
            else
            {
                // ���� On
                var t = (float)(tick - leftFrame.tick) / leftFrame.interpolation;

                var before = _frames.Values[left - 1];
                SetObjectTransformationInter(t, before, leftFrame);
            }

        }

        // ���� ���� �״�� ����
        private void SetObjectTransformation(string id, BdObject obj)
        {
            if (!_idDict.TryGetValue(id, out var target))
            {
                if (!_noID.Contains(id))
                {
                    CustomLog.LogError("Target not found, name : " + id);
                    _noID.Add(id);
                }
                return;
            }

            target.SetTransformation(obj.Transforms);

            if (obj.Children != null)
            {
                foreach (var child in obj.Children)
                {
                    SetObjectTransformation(child.ID, child);
                }
            }
        }

        
        // ReSharper disable Unity.PerformanceAnalysis
        public void SetObjectTransformationInter(float t, Frame a, Frame b)
            => SetObjectTransformationInter(
                _root.BdObject.ID, t, 
                a.Info, b.Info, 
                a.IDDataDict, b.IDDataDict,
                new HashSet<string>());
        
        // target�� a, b�� t ������ �����Ͽ� ����
        private void SetObjectTransformationInter(
            string targetName, float t, 
            BdObject a, BdObject b, 
            Dictionary<string, BdObject> aDict, Dictionary<string, BdObject> bDict,
            HashSet<string> visitedNodes
        )
        {
            if (!visitedNodes.Add(targetName)) return;

            if (!_idDict.TryGetValue(targetName, out var target))
            {
                CustomLog.Log("Target not found, name : " + targetName);
                return;
            }

            // 1. transforms(4x4 ���, float[16])�� t ������ ����
            var result = new float[16];
            for (var i = 0; i < 16; i++)
            {
                result[i] = a.Transforms[i] * (1f - t) + b.Transforms[i] * t;
            }

            // 2. ������ ����� target�� ����
            target.SetTransformation(result);

            // �ڽ��� ������ ����
            if (a.Children == null && b.Children == null) return;

            var processedKeys = new HashSet<string>();

            foreach (var key in aDict.Keys)
            {
                if (bDict.TryGetValue(key, out var bChild) && aDict[key] != null)
                {
                    var aChild = aDict[key];
                    processedKeys.Add(key);

                    if (_idDict.ContainsKey(key))
                    {
                        SetObjectTransformationInter(key, t, aChild, bChild, aDict, bDict, visitedNodes);
                    }
                }
            }

            foreach (var key in bDict.Keys)
            {
                if (!processedKeys.Contains(key) && aDict.TryGetValue(key, out var aChild))
                {
                    var bChild = bDict[key];

                    if (_idDict.ContainsKey(key))
                    {
                        SetObjectTransformationInter(key, t, aChild, bChild, aDict, bDict, visitedNodes);
                    }
                }
            }
        }


        private int GetLeftFrame(int tick)
        {

            // 1. ���� ���� �����Ӻ��� tick�� ������ null ��ȯ
            if (_frames.Values[0].tick > tick)
                return -1;

            var left = 0;
            var right = _frames.Count - 1;
            var keys = _frames.Keys;
            var idx = -1; // �ʱ갪�� -1�� ���� (��ȿ�� �ε����� ���� ��� ���)

            // 2. ���� Ž������ left ������ ã��
            while (left <= right)
            {
                var mid = (left + right) / 2;
                if (keys[mid] <= tick) // "<" ��� "<=" ����Ͽ� ��Ȯ�� tick�� ��� mid�� idx�� ����
                {
                    idx = mid; // ���� mid�� left �ĺ�
                    left = mid + 1; // �� ū �� Ž��
                }
                else
                {
                    right = mid - 1; // �� ���� �� Ž��
                }
            }

            // 3. leftIdx ���� (idx�� -1�� ��쵵 ���)
            if (idx >= 0)
            {
                return idx;
            }

            return -1;
        }

        #endregion

        #region EditFrame

        // Ŭ������ �� 
        public void OnEventTriggerClick(BaseEventData eventData)
        {
            if (eventData is PointerEventData { button: PointerEventData.InputButton.Right } pointerData)
            {
                //Debug.Log("Right Click");
                var line = _manager.timeline.GetTickLine(pointerData.position);
                GameManager.GetManager<ContextMenuManager>().ShowContextMenu(this, line.Tick);
            }
        }

        // tick ��ġ�� ������ �߰��ϱ�. ���� tick�� �̹� �������� �ִٸ� tick�� �׸�ŭ �ڷ� �̷�
        // ���� �Է����� ���� BDObject�� firstFrame�� �ٸ� ���¶�� �ź�
        public void AddFrame(string fileName, BdObject frameInfo, int tick, int inter)
        {
            //Debug.Log("fileName : " + fileName + ", tick : " + tick + ", inter : " + inter);

            var frame = Instantiate(_manager.framePrefab, transform.GetChild(0));

            while (_frames.ContainsKey(tick))
            {
                tick++;
            }

            _frames.Add(tick, frame);
            frame.Init(fileName, tick, inter, frameInfo, this);
        }

        // �̸����� s, i ���� �����Ͽ� ������ �߰��ϱ�
        public void AddFrame(BdObject frameInfo, string fileName)
        {
            CustomLog.Log("AddFrame : " + fileName);    
            GetTickAndInterByFileName(fileName, out var tick, out var inter);
            AddFrame(fileName, frameInfo, tick, inter);
        }

        private void GetTickAndInterByFileName(string fileName, out int tick, out int inter)
        {
            var setting = GameManager.Setting;

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

            if (setting.UseNameInfoExtract)
            {
                var sValue = BdObjectHelper.ExtractNumber(fileName, "s");
                inter = BdObjectHelper.ExtractNumber(fileName, "i", inter);

                if (sValue > 0)
                    tick += sValue;
            }
            else
            {
                tick += setting.defaultTickInterval;
            }
        }

        // ������ �����ϱ�
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

        // �ִϸ��̼� ������Ʈ �����ϱ�
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

        // ��ġ�� ���� �������� Ȯ���ϰ� ���� �����ϸ� frames �����ϰ� true ��ȯ
        public bool ChangePos(Frame frame, int firstTick, int changedTick)
        {
            //Debug.Log("firstTick : " + firstTick + ", changedTick : " +  changedTick);
            if (firstTick == changedTick) return true;
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
