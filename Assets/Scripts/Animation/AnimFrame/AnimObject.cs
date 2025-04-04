using System;
using System.Collections.Generic;
using BDObjectSystem;
using GameSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Animation.UI;
using BDObjectSystem.Utility;
using FileSystem;

namespace Animation.AnimFrame
{
    public partial class AnimObject : MonoBehaviour
    {
        #region Variables
        public RectTransform rect;
        public TextMeshProUGUI title;
        public Frame firstFrame;

        public SortedList<int, Frame> frames = new SortedList<int, Frame>();
        public string bdFileName;

        private int MaxTick => frames.Count == 0 ? 0 : frames.Values[frames.Count - 1].tick;

        private AnimObjList _manager;

        public BDObjectAnimator animator;

        private readonly HashSet<string> _noID = new HashSet<string>();
        #endregion

        #region Functions
        // Set initial values and initialize first frame
        public void Init(string fileName, AnimObjList list)
        {
            title.text = fileName;
            _manager = list;
            bdFileName = fileName;

            animator = GameManager.GetManager<BdObjectManager>().BDObjectAnim[fileName];

            GetTickAndInterByFileName(bdFileName, out _, out var inter);
            firstFrame.Init(bdFileName, 0, inter, animator.RootObject.BdObject, this, _manager.timeline);

            frames[0] = firstFrame;

            AnimManager.TickChanged += OnTickChanged;
        }
        
        #region Transform

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
                animator.ApplyTransformation(leftFrame);
            }
            else
            {
                SetObjectTransformationInterpolation(tick, left);
            }
        }

        private void SetObjectTransformationInterpolation(float tick, int indexOf)
        {
            Frame a = frames.Values[indexOf - 1];
            Frame b = frames.Values[indexOf];

            // b 프레임 기준 보간 비율 t 계산 (0~1로 클램프)
            float t = Mathf.Clamp01((tick - b.tick) / b.interpolation);
            animator.ApplyTransformation(a, b, t);
        }

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

        public void UpdateAllFrameInterJump()
        {
            foreach (var frame in frames.Values)
            {
                frame.UpdateInterpolationJump();
            }
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
            while (frames.ContainsKey(tick))
            {
                tick++;
            }

            frames.Add(tick, frame);
            frame.Init(fileName, tick, inter, frameInfo, this, _manager.timeline);
        }

        // add frame with fileName
        public void AddFrame(BdObject frameInfo, string fileName)
        {
            //CustomLog.Log("AddFrame : " + fileName);    
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

            var fileManager = GameManager.GetManager<FileLoadManager>();

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
            if (frames == null) return;

            frames.Remove(frame.tick);
            Destroy(frame.gameObject);

            if (frames.Count == 0)
            {
                RemoveAnimObj();
            }
            else if (frame.tick == 0)
            {
                frames.Values[0].SetTick(0);
            }

        }

        // remove self
        public void RemoveAnimObj()
        {
            AnimManager.TickChanged -= OnTickChanged;
            var frame = frames;
            frames = null;
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
            if (frames.ContainsKey(changedTick)) return false;

            frames.Remove(firstTick);
            frames.Add(changedTick, frame);

            OnTickChanged(GameManager.GetManager<AnimManager>().Tick);
            return true;
        }
        #endregion
        #endregion
    }
}
