using System;
using System.Collections.Generic;
using GameSystem;
using UnityEngine;
using Animation.UI;
using BDObjectSystem;
using FileSystem;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // Required for Mouse.current

namespace Animation.AnimFrame
{
    public class AnimObjList : BaseManager
    {
        public RectTransform importButton;
        public float jump;

        public AnimObject animObjectPrefab;
        public Frame framePrefab;
        public List<AnimObject> animObjects = new();
        public AnimObject selectedAnimObject = null; // 현재 선택된 AnimObject

        public Timeline timeline;
        public RectTransform scrollViewContent;

        public HashSet<Frame> selectedFrames = new();
        private Frame _lastAnchorFrame = null; // Shift 선택의 기준점

        public GameObject cancelPanel;

        // Dragging state variables
        [SerializeField]
        private bool _isDraggingAnyFrame = false;
        private int _dragInitialMouseTick;
        private readonly Dictionary<Frame, int> _dragInitialFrameTicks = new Dictionary<Frame, int>();


        private void Start()
        {
            GameManager.GetManager<FileLoadManager>().animObjList = this;
            timeline = GameManager.GetManager<AnimManager>().timeline;
            jump = importButton.sizeDelta.y * 1.5f;
        }

        public AnimObject AddAnimObject(string fileName)
        {
            //Debug.Log("EndAddObject: " + obj.name);

            var animObject = Instantiate(animObjectPrefab, scrollViewContent);
            // animObject.rect.anchoredPosition = new Vector2(animObject.rect.anchoredPosition.x, importButton.anchoredPosition.y - 60f);

            // importButton.anchoredPosition = new Vector2(importButton.anchoredPosition.x, importButton.anchoredPosition.y - jump);
            // scrollViewContent.anchoredPosition = new Vector2(scrollViewContent.anchoredPosition.x, scrollViewContent.anchoredPosition.y - jump/2f);
            // scrollViewContent.sizeDelta = new Vector2(scrollViewContent.sizeDelta.x, scrollViewContent.sizeDelta.y + jump);
            importButton.SetAsLastSibling();

            var animMan = GameManager.GetManager<AnimManager>();
            animMan.Tick = 0;
            animMan.timeline.SetTickTexts(0);

            var SaveMan = GameManager.GetManager<SaveManager>();

            // 최초 삽입 시 세이브 파일 생성
            if (SaveMan.IsNoneSaved)
            {
                SaveMan.MakeNewMDEFile(fileName);
            }
            animObjects.Add(animObject);
            animObject.Init(fileName, this);

            return animObject;
        }

        public void ResetAnimObject()
        {
            var objs = animObjects.ToArray();
            foreach (var obj in objs)
            {
                RemoveAnimObject(obj);
            }

        }

        public void RemoveAnimObject(AnimObject obj)
        {
            var idx = animObjects.IndexOf(obj);
            animObjects.RemoveAt(idx);

            // AnimObject가 제거될 때, 해당 Object에 속한 모든 프레임을 선택 해제합니다.
            List<Frame> framesToRemoveFromSelection = new List<Frame>();
            foreach (var selectedFrame in selectedFrames)
            {
                if (selectedFrame.animObject == obj)
                {
                    framesToRemoveFromSelection.Add(selectedFrame);
                }
            }
            foreach (var frameToRemove in framesToRemoveFromSelection)
            {
                selectedFrames.Remove(frameToRemove);
                // SetSelectedVisual(false)는 Frame의 GameObject가 Destroy될 것이므로 호출하지 않아도 됩니다.
            }
            if (_lastAnchorFrame != null && _lastAnchorFrame.animObject == obj)
            {
                _lastAnchorFrame = null;
            }

            GameManager.GetManager<BdObjectManager>().RemoveBdObject(obj.bdFileName);

            Destroy(obj.gameObject);

            for (var i = idx; i < animObjects.Count; i++)
            {
                animObjects[i].rect.anchoredPosition = new Vector2(animObjects[i].rect.anchoredPosition.x, animObjects[i].rect.anchoredPosition.y + jump);
            }
            importButton.anchoredPosition = new Vector2(importButton.anchoredPosition.x, importButton.anchoredPosition.y + jump);

            CustomLog.Log("Line Removed: " + obj.bdFileName);
        }

        public List<SortedList<int, ExportFrame>> GetAllFrames()
        {
            var frames = new List<SortedList<int, ExportFrame>>();

            foreach (var animObject in animObjects)
            {
                var animFrames = new SortedList<int, ExportFrame>();
                foreach (var frame in animObject.frames.Values)
                {
                    animFrames.Add(frame.tick, new ExportFrame(frame));
                }
                frames.Add(animFrames);
            }

            return frames;
        }

        public void SelectFrame(Frame frame)
        {
            selectedFrames.Add(frame);
        }

        public void DeSelectFrame(Frame frame)
        {
            selectedFrames.Remove(frame);
        }

        void Update()
        {
            if (_isDraggingAnyFrame)
            {
                if (Mouse.current.leftButton.isPressed)
                {
                    var currentLine = timeline.GetTickLine(Mouse.current.position.ReadValue());
                    if (currentLine != null)
                    {
                        int currentMouseTick = currentLine.Tick;
                        int tickDelta = currentMouseTick - _dragInitialMouseTick;

                        var framesToDrag = _dragInitialFrameTicks.Keys;

                        foreach (var frameToDrag in framesToDrag)
                        {
                            if (frameToDrag != null && frameToDrag.gameObject.activeInHierarchy && _dragInitialFrameTicks.TryGetValue(frameToDrag, out int initialTick))
                            {
                                frameToDrag.SetTick(initialTick + tickDelta);
                            }
                        }
                    }
                }
                else // Mouse button was released or is no longer pressed
                {
                    _isDraggingAnyFrame = false;
                    foreach (var frame in _dragInitialFrameTicks.Keys)
                    {
                        if (frame != null)
                        {
                            frame.IsBeingDragged = false;
                        }
                    }
                    _dragInitialFrameTicks.Clear();
                }
            }
        }

        public void HandleFramePointerDown(Frame clickedFrame, bool isCtrlPressed, bool isShiftPressed)
        {
            bool wasAlreadySelected = selectedFrames.Contains(clickedFrame);

            if (wasAlreadySelected && !isCtrlPressed && !isShiftPressed)
            {
                // 이미 선택된 프레임을 클릭한 경우, 선택 로직(HandleFrameClick)을 건너뛰고 바로 드래그 준비
                // Ctrl이나 Shift를 누르고 이미 선택된 프레임을 클릭해도, 선택 상태는 변경하지 않고 드래그만 시작
                _lastAnchorFrame = clickedFrame; // 마지막으로 상호작용한 프레임을 anchor로 설정
            }
            else
            {
                // 선택되지 않은 프레임을 클릭한 경우, 먼저 선택 로직을 처리
                HandleFrameClick(clickedFrame, isCtrlPressed, isShiftPressed);
            }

            // 클릭된 프레임이 (새롭게 또는 기존에) 선택된 상태라면 드래그 작업 시작
            if (selectedFrames.Contains(clickedFrame))
            {
                _isDraggingAnyFrame = true;
                var initialMouseLine = timeline.GetTickLine(Mouse.current.position.ReadValue());
                _dragInitialMouseTick = initialMouseLine?.Tick ?? clickedFrame.tick; // Fallback if timeline returns null

                _dragInitialFrameTicks.Clear();
                foreach (var frame in selectedFrames)
                {
                    if (frame != null) // Ensure frame is not null
                    {
                        _dragInitialFrameTicks[frame] = frame.tick;
                        frame.IsBeingDragged = true;
                    }
                }
            }
            else
            {
                // 클릭 후에도 프레임이 선택되지 않은 경우 (예: Ctrl 클릭으로 선택 해제) 드래그 상태 초기화
                _isDraggingAnyFrame = false;
                _dragInitialFrameTicks.Clear();
            }
        }


        public void HandleFrameClick(Frame clickedFrame, bool isCtrlPressed, bool isShiftPressed)
        {
            if (clickedFrame == null || clickedFrame.animObject == null) return;

            if (!isShiftPressed) // Shift가 눌리지 않은 경우
            {
                if (!isCtrlPressed) // Ctrl도 눌리지 않은 경우 (단일 선택)
                {
                    ClearAllSelections();
                    AddToSelection(clickedFrame);
                    _lastAnchorFrame = clickedFrame;
                }
                else // Ctrl만 눌린 경우 (토글 선택)
                {
                    if (selectedFrames.Contains(clickedFrame))
                    {
                        RemoveFromSelection(clickedFrame);
                        // 마지막으로 상호작용한 프레임을 anchor로 설정 (선택 해제된 경우에도)
                        _lastAnchorFrame = clickedFrame;
                    }
                    else
                    {
                        AddToSelection(clickedFrame);
                        _lastAnchorFrame = clickedFrame;
                    }
                }
            }
            else // Shift가 눌린 경우 (범위 선택)
            {
                // _lastAnchorFrame이 유효하고, clickedFrame과 같은 AnimObject에 속하며,
                // 실제로 해당 AnimObject의 frames 리스트에 존재하는지 확인합니다.
                bool anchorIsValidForRangeSelection = _lastAnchorFrame != null &&
                                                      _lastAnchorFrame.animObject == clickedFrame.animObject &&
                                                      clickedFrame.animObject.frames.Values.Contains(_lastAnchorFrame);

                if (!anchorIsValidForRangeSelection)
                {
                    // 기준점이 없거나, 다른 AnimObject의 프레임이거나,
                    // AnimObject의 내부 리스트에 더 이상 존재하지 않으면 단일 선택처럼 동작합니다.
                    ClearAllSelections();
                    AddToSelection(clickedFrame);
                    _lastAnchorFrame = clickedFrame;
                }
                else
                {
                    // 같은 AnimObject 내에서 범위 선택 (anchor가 유효함)
                    if (!isCtrlPressed) // Ctrl 키가 눌리지 않았다면, 기존 선택을 모두 해제합니다.
                    {
                        var saveAnchor = _lastAnchorFrame; // anchor를 저장해두고
                        ClearAllSelections();
                        _lastAnchorFrame = saveAnchor; // anchor를 다시 설정합니다.
                    }
                    // Ctrl 키가 눌렸다면, 기존 선택에 현재 범위를 추가합니다.

                    var framesInObject = clickedFrame.animObject.frames.Values.ToList();
                    int anchorIndex = framesInObject.IndexOf(_lastAnchorFrame);
                    // clickedFrame은 방금 클릭되었으므로 해당 AnimObject의 frames 리스트에 반드시 있어야 합니다.
                    int clickedIndex = framesInObject.IndexOf(clickedFrame);

                    // ContainsValue로 확인했으므로 anchorIndex는 -1이 아니어야 하지만, 안전을 위해 체크합니다.
                    if (anchorIndex == -1 || clickedIndex == -1)
                    {
                        // 예외 상황: 프레임을 찾지 못함.
                        // Ctrl이 안 눌렸다면 ClearAllSelections가 이미 호출되었을 수 있으므로,
                        // clickedFrame만 다시 선택하도록 합니다.
                        if (!isCtrlPressed)
                        {
                            // ClearAllSelections(); // 이미 위에서 호출되었을 수 있음
                            AddToSelection(clickedFrame); // 현재 클릭된 프레임만 선택
                        }
                        else
                        {
                            // Ctrl + Shift인데 인덱스를 못찾는 경우는 드물지만, 일단 현재 프레임 추가 시도
                            AddToSelection(clickedFrame);
                        }
                        _lastAnchorFrame = clickedFrame; // anchor 재설정
                        return;
                    }

                    int startIndex = Mathf.Min(anchorIndex, clickedIndex);
                    int endIndex = Mathf.Max(anchorIndex, clickedIndex);

                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        AddToSelection(framesInObject[i]);
                    }
                    // _lastAnchorFrame은 변경하지 않아, 최초의 anchor를 기준으로 계속 범위 선택이 가능합니다.
                }
            }
        }

        public void HandleFrameRemoval(Frame removedFrame)
        {
            if (selectedFrames.Contains(removedFrame))
            {
                RemoveFromSelection(removedFrame); // SetSelectedVisual(false)는 여기서 호출됨
            }
            if (_lastAnchorFrame == removedFrame)
            {
                _lastAnchorFrame = null;
            }
        }

        private void AddToSelection(Frame frame)
        {
            if (selectedFrames.Add(frame))
            {
                frame.SetSelectedVisual(true);
            }
        }

        private void RemoveFromSelection(Frame frame)
        {
            if (selectedFrames.Remove(frame))
            {
                frame.SetSelectedVisual(false);
            }
        }

        public void ClearAllSelections()
        {
            foreach (var frame in selectedFrames)
            {
                frame.SetSelectedVisual(false);
                frame.IsBeingDragged = false; // Ensure IsBeingDragged is reset if selection is cleared externally
            }
            selectedFrames.Clear();
            // _lastAnchorFrame = null; // This was moved to CancelPanelClicked or handled by selection logic
        }


        public void DuplicateSelectedFrames()
        {
            if (selectedFrames.Count == 0) return;

            List<Frame> newlySelectedFrames = new List<Frame>();
            List<Frame> originalSelectedFrames = selectedFrames.ToList();

            foreach (var frame in originalSelectedFrames)
            {
                frame.SetSelectedVisual(false);
                frame.IsBeingDragged = false;
            }
            selectedFrames.Clear();

            foreach (var frame in originalSelectedFrames)
            {
                var newFrame = frame.Duplicate(); // Duplicate는 새 tick으로 프레임을 생성하고 AnimObject에 추가
                if (newFrame != null)
                {
                    newlySelectedFrames.Add(newFrame);
                }
            }

            foreach (var newFrame in newlySelectedFrames)
            {
                AddToSelection(newFrame); // 복제된 새 프레임들을 선택 상태로 만듦
            }

            if (newlySelectedFrames.Count > 0)
            {
                _lastAnchorFrame = newlySelectedFrames.LastOrDefault(); // 마지막으로 복제된 프레임을 새 anchor로 설정
            }
        }

        public void CancelPanelClicked(BaseEventData eventData)
        {
            if (eventData is PointerEventData pointerData && pointerData.button == PointerEventData.InputButton.Left)
            {
                if (pointerData.pointerCurrentRaycast.gameObject == cancelPanel || pointerData.pointerCurrentRaycast.gameObject == null)
                {
                    ClearAllSelections();
                    _lastAnchorFrame = null; // Explicitly clear anchor on background click
                }
                timeline.OnPointerDown(pointerData); // Ensure timeline handles pointer down event
                SelectAnimObject(null); // 선택된 AnimObject를 해제합니다.
            }
        }

        public void SelectAnimObject(AnimObject animObject)
        {
            foreach (var obj in animObjects)
            {
                obj.SetSelectPanel(false);
            }
            animObject?.SetSelectPanel(true);
            selectedAnimObject = animObject;
        }
    }
}
