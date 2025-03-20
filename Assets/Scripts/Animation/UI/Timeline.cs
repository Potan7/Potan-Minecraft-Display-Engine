using System;
using System.Collections.Generic;
using Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Animation.UI
{
    public class Timeline : MonoBehaviour, IPointerDownHandler
    {
        public TickLine gridPrefab;
        public List<TickLine> grid;
        [FormerlySerializedAs("TimeBar")] public RectTransform timeBar;
        private int _tick;

        public int gridCount = 100;
        public event Action OnGridChanged;

        public bool isClicking;

        private AnimManager _animManager;
        RectTransform _rectTransform;

        private void Start()
        {
            _rectTransform = GetComponent<RectTransform>();
            SetTickTexts(0);
            for (var i = 0; i < gridCount; i++)
            {
                grid[i].index = i;
            }
            AnimManager.TickChanged += OnAnimManagerTickChanged;

            _animManager = GameManager.GetManager<AnimManager>();
            OnAnimManagerTickChanged(_animManager.Tick);

        }

        // AnimManager의 Tick 값이 변경됨
        private void OnAnimManagerTickChanged(int tick)
        {
            _tick = tick;
            var line = GetTickLine(_tick, true);

            if (line)
            {
                timeBar.anchoredPosition = new Vector2(line.rect.anchoredPosition.x, timeBar.anchoredPosition.y);
            }
        }

        // tick 값에 해당하는 TickLine을 반환
        public TickLine GetTickLine(int tick, bool changeGrid)
        {
            if (tick < 0)
            {
                return null;
            }

            var line = grid[0];
            if (tick < line.Tick)
            {
                if (!changeGrid) return null;

                SetTickTexts(tick);
                return GetTickLine(tick, true);
            }

            line = grid[gridCount - 1];
            if (tick > line.Tick)
            {
                if (!changeGrid) return null;

                SetTickTexts(line.Tick + 1);
                return GetTickLine(tick, true);
            }

            var index = BinarySearchTick(grid, tick, gridCount);
            return index >= 0 ? grid[index] : null;
        }

        private int BinarySearchTick(List<TickLine> grid, int tick, int max)
        {
            int left = 0, right = gridCount - 1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;

                // Tick 값을 비교
                if (grid[mid].Tick == tick)
                    return mid; // 정확한 값 찾음

                if (grid[mid].Tick < tick)
                    left = mid + 1; // 오른쪽 탐색
                else
                    right = mid - 1; // 왼쪽 탐색
            }

            return ~left; // 찾지 못하면 삽입 위치 반환 (BinarySearch와 동일한 동작)
        }


        // 주어진 pos와 가장 가까운 TickLine을 반환
        public TickLine GetTickLine(Vector2 pos)
        {
            var maxIndex = 0;
            var max = Vector2.Distance(pos, grid[maxIndex].rect.position);
            for (var i = 1; i < grid.Count; i++)
            {
                var distance = Vector2.Distance(pos, grid[i].rect.position);
                if (distance < max)
                {
                    max = distance;
                    maxIndex = i;
                }
            }
            return grid[maxIndex];
        }

        // Grid당 개수 변경
        public void ChangeGrid(int move)
        {
            gridCount += move;
            SetTickTexts(grid[0].Tick);

            OnAnimManagerTickChanged(_tick);
        }

        // TickLine Grid 변경하기
        public void SetTickTexts(int start)
        {
            for (var i = 0; i < gridCount; i++)
            {
                if (grid.Count <= i)
                {
                    var newGrid = Instantiate(gridPrefab, transform);
                    grid.Add(newGrid);
                    newGrid.index = i;
                }
                else
                {
                    grid[i].gameObject.SetActive(true);
                }

                if (gridCount < 51)
                {
                    grid[i].SetTick(start + i, i == 0 || i % 5 == 0);
                }
                else
                {
                    grid[i].SetTick(start + i, i == 0);
                }

            }
            for (var i = gridCount; i < grid.Count; i++)
            {
                grid[i].gameObject.SetActive(false);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);

            OnGridChanged?.Invoke();
        }

        // 마우스 클릭시
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                isClicking = true;
            }
        }

        private void Update()
        {
            if (isClicking)
            {
                Vector2 pos = Input.mousePosition;
                var line = GetTickLine(pos);
                if (line && line.Tick != _animManager.Tick)
                {
                    _animManager.Tick = line.Tick;
                }

                if (Input.GetMouseButtonUp(0))
                {
                    isClicking = false;
                }
            }
        }
    }
}
