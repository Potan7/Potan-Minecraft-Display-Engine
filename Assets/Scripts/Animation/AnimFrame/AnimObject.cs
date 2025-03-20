using System;
using System.Collections.Generic;
using BDObjectSystem;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Animation.UI;
using BDObjectSystem.Utility;

namespace Animation.AnimFrame
{
    public class AnimObject : MonoBehaviour
    {
        public RectTransform rect;
        public TextMeshProUGUI title;
        public Frame firstFrame;
        
        private TransformationManager _transformationManager;
        private SortedList<int, Frame> _frames = new SortedList<int, Frame>();
        public string bdFileName;
        
        private int MaxTick => _frames.Count == 0 ? 0 : _frames.Values[_frames.Count - 1].tick;

        private AnimObjList _manager;

        private BdObjectContainer _root;
        // private Dictionary<string, BdObjectContainer> _idDict;
        private List<AnimModel> _displayList;

        // Set initial values and initialize first frame
        public void Init(string fileName, AnimObjList list)
        {
            
            title.text = fileName;
            _manager = list;
            bdFileName = fileName;
        }

        public void InitAnimModelData()
        {
            var bdObject = GameManager.GetManager<BdObjectManager>().BdObjects[bdFileName];
            _root = bdObject.RootObject;
            // _idDict = bdObject.Item2;
            _displayList = bdObject.AnimObjects;

            GetTickAndInterByFileName(bdFileName, out _, out var inter);
            firstFrame.Init(bdFileName, 0, inter, _root.BdObject, this, _manager.timeline);

            _frames[0] = firstFrame;
            
            _transformationManager = new TransformationManager(_frames, _displayList);

            AnimManager.TickChanged += _transformationManager.OnTickChanged;
        }

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
            AnimManager.TickChanged -= _transformationManager.OnTickChanged;
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

            _transformationManager.OnTickChanged(GameManager.GetManager<AnimManager>().Tick);
            return true;
        }
        #endregion
    }
}
