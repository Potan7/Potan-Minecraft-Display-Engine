using System.Collections.Generic;
using BDObjectSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using BDObjectSystem.Utility;
using UnityEngine.UI;
using Animation.UI;
using System;

namespace Animation.AnimFrame
{
    public class Frame : MonoBehaviour, IPointerDownHandler//, IPointerUpHandler
    {
        [Header("Frame Components")]
        public RectTransform rect;
        public Image outlineImage;
        public AnimObject animObject;

        private Color _initColor;
        private readonly Color _selectedColor = Color.yellow;

        [Header("Frame Info")]
        public bool isSelected;
        public int tick;
        public int interpolation;
        public string fileName;
        public TickLine tickLine;
        public RectTransform interpolationRect;

        [Header("BDObject Info")]
        public BdObject Info;
        public List<BdObject> leafObjects;

        public bool modelDiffrent;
        public Dictionary<string, Matrix4x4> modelMatrixDict = new Dictionary<string, Matrix4x4>();
        public bool IsJump = false;

        private Timeline _timeline;


        public void Init(string initFileName, int initTick, int inter, BdObject info, AnimObject obj, Timeline timeLine)
        {
            //Debug.Log("tick : " + tick);
            fileName = initFileName;
            animObject = obj;
            _initColor = outlineImage.color;
            _timeline = timeLine;
            Info = info;
            tick = initTick;
            SetInter(inter);

            UpdatePos();
            _timeline.OnGridChanged += UpdatePos;

            leafObjects = BdObjectHelper.SetDisplayList(info, modelMatrixDict);

            modelDiffrent = animObject.animator.RootObject.bdObjectID != info.ID;
            //Debug.Log(animObject.animator);
            if (modelDiffrent)
            {
                Debug.Log($"Model is different, name : {fileName}\nModel : {animObject.animator.RootObject.bdObjectID}\nInfo : {info.ID}");
                //Debug.Log("Model is different, name : " + animObject.animator.RootObject.bdObjectID);
            }


        }

        public Matrix4x4 GetMatrix(string id)
        {
            if (modelMatrixDict.TryGetValue(id, out var matrix))
            {
                return matrix;
            }
            Debug.LogError($"Matrix not found for ID: {id}");
            return Matrix4x4.identity;
        }

        public int SetTick(int newTick)
        {
            // 0�̸� ����
            if (tick == 0)
                return tick;
            if (!animObject.ChangePos(this, tick, newTick)) return tick;

            tick = newTick;
            UpdatePos();
            animObject.UpdateAllFrameInterJump();
            return newTick;

        }

        // 변경된 Grid에 맞추어 위치 변경 
        private void UpdatePos()
        {
            var line = _timeline.GetTickLine(tick, false);
            if (line is null)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
                //var pos = line.rect.anchoredPosition.x;
                //rect.anchoredPosition = new Vector2(line.rect.anchoredPosition.x, rect.anchoredPosition.y);
                rect.position = new Vector2(line.rect.position.x, rect.position.y);
                tickLine = line;
            }
            UpdateInterpolationBar();
        }

        public bool SetInter(int inter)
        {
            if (interpolation < 0)
                return false;

            interpolation = inter;
            UpdateInterpolationBar();

            animObject.UpdateAllFrameInterJump();

            return true;
        }

        /// <summary>
        /// Interpolation Bar를 업데이트합니다.
        /// Interpolation이 0일 경우에는 Bar를 비활성화합니다.
        /// </summary>
        private void UpdateInterpolationBar()
        {
            if (interpolation == 0)
            {
                interpolationRect.gameObject.SetActive(false);
            }
            else
            {
                interpolationRect.gameObject.SetActive(true);

                var line = _timeline.GetTickLine(tick + interpolation, false);
                line ??= _timeline.grid[_timeline.gridCount - 1];

                // 1. line의 World Position 구함
                Vector3 lineWorldPos = line.rect.position;

                // 2. interpolationRect의 부모 기준 Local Position으로 변환
                RectTransform parentRect = interpolationRect.parent as RectTransform;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect,
                    RectTransformUtility.WorldToScreenPoint(null, lineWorldPos),
                    null,
                    out Vector2 localPos);

                // 3. interpolationRect 위치 조정 (왼쪽 고정이니까 anchoredPosition은 그대로)
                float width = localPos.x - interpolationRect.anchoredPosition.x;
                width += line.rect.rect.width / 2; // line의 rect width 반영

                interpolationRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }
        }

        /// <summary>
        /// Interpolation Jump를 업데이트합니다.
        /// </summary>
        public void UpdateInterpolationJump()
        {
            int idx = animObject.frames.IndexOfKey(tick);
            if (idx <= 0 || idx >= animObject.frames.Count - 1) return;
            // ↑ 범위 밖이면 업데이트 불가

            // 다음 프레임
            Frame nextFrame = animObject.frames.Values[idx + 1];

            // 매번 dict를 초기화
           

            // (1) 점프 발생 여부 체크
            // "tick + interpolation > nextFrame.tick" → 보간점프 발생
            bool isJump = tick + interpolation > nextFrame.tick;

            if (!isJump)
            {
                modelMatrixDict.Clear();
                // (2) 점프가 아닌 경우
                // "현재 leafObjects의 Transforms 그대로" or "일반 보간"
                // 여기서는 예시로, "그냥 현재 상태 그대로"를 저장
                foreach (var obj in leafObjects)
                {
                    var current = obj;
                    while (current != null)
                    {
                        // 이미 넣었다면 중복 방지
                        if (modelMatrixDict.ContainsKey(current.ID))
                            break;

                        // '현재' transform 행렬 그대로
                        Matrix4x4 mat = current.Transforms.GetMatrix();
                        modelMatrixDict.Add(current.ID, mat);

                        current = current.Parent;
                    }
                }
                IsJump = isJump;
            }
            else if (isJump)
            {
                modelMatrixDict.Clear();
                // (3) 점프가 발생하는 경우
                // tick + interpolation > nextFrame.tick
                float ratio = Mathf.Clamp01((nextFrame.tick - tick) / (float)interpolation);

                // 예: 이전 프레임 beforeFrame = frames.Values[idx - 1];
                // 만약 idx==0이면 이전 프레임이 없으니, 안전장치 필요

                Frame beforeFrame = animObject.frames.Values[idx - 1];

                foreach (var obj in leafObjects)
                {
                    var current = obj;
                    while (current != null)
                    {
                        if (modelMatrixDict.ContainsKey(current.ID))
                            break;

                        // 현재 상태
                        Matrix4x4 aMatrix = current.Transforms.GetMatrix();
                        // 이전 프레임 상태
                        Matrix4x4 bMatrix = beforeFrame.GetMatrix(current.ID);

                        // 부분 보간
                        Matrix4x4 lerpedMatrix = BDObjectAnimator.InterpolateMatrixTRS(aMatrix, bMatrix, ratio);
                        modelMatrixDict.Add(current.ID, lerpedMatrix);

                        current = current.Parent;
                    }
                }
                IsJump = isJump;
            }
        }

        /// <summary>
        /// Frame을 클릭했을 때 호출됩니다.
        /// 좌클릭일 경우 선택 상태로 변경하고, 우클릭일 경우 ContextMenu를 띄웁니다.
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                isSelected = true;
                outlineImage.color = _selectedColor;
            }
            else
            {
                GameManager.GetManager<ContextMenuManager>().ShowContextMenu(this);
            }
        }

        private void Update()
        {
            if (!isSelected) return;

            Vector2 mouse = Input.mousePosition;
            var line = _timeline.GetTickLine(mouse);

            SetTick(line.Tick);

            if (Input.GetMouseButtonUp(0))
            {
                isSelected = false;
                outlineImage.color = _initColor;
            }
        }


        public void RemoveFrame()
        {
            _timeline.OnGridChanged -= UpdatePos;
            animObject.RemoveFrame(this);
        }

    }
}
