using System;
using System.Collections.Generic;
using Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Animation
{
    public class Timeline : MonoBehaviour, IPointerDownHandler
    {
        public TickLine gridPrefab;
        public List<TickLine> grid;
        [FormerlySerializedAs("TimeBar")] public RectTransform timeBar;
        private int _tick;

        [FormerlySerializedAs("GridCount")] public int gridCount = 100;
        public event Action OnGridChanged;

        [FormerlySerializedAs("IsClicking")] public bool isClicking;

        private AnimManager _animManager;

        private void Start()
        {
            SetTickTexts(0);
            for (var i = 0; i < gridCount; i++)
            {
                grid[i].index = i;
            }
            AnimManager.TickChanged += OnAnimManagerTickChanged;

            _animManager = GameManager.GetManager<AnimManager>();
            OnAnimManagerTickChanged(_animManager.Tick);

        }

        private void OnAnimManagerTickChanged(int tick)
        {
            _tick = tick;
            var line = GetTickLine(_tick, true);

            if (line)
            {
                timeBar.anchoredPosition = new Vector2(line.rect.anchoredPosition.x, timeBar.anchoredPosition.y);
            }
        }

        /// <summary>
        /// ƽ�� �ش��ϴ� �׸��带 �����´�.
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="changeGrid"> true�� ��� tick�� �ش��ϵ��� �׸��带 �����Ѵ�.</param>
        /// <returns>ã�� ��� : TickLine, ��ã�� ��� : null</returns>
        public TickLine GetTickLine(int tick, bool changeGrid)
        {
            if (tick < 0)
            {
                return null;
            }
            // ���� ���̸� �׸��� ����
            var line = grid[0];
            if (tick < line.Tick)
            {
                if (!changeGrid) return null;
                SetTickTexts(tick);
                return GetTickLine(tick, changeGrid);
            }

            line = grid[gridCount-1];
            if (tick > line.Tick)
            {
                if (!changeGrid) return null;
                SetTickTexts(line.Tick + 1);
                return GetTickLine(tick, changeGrid);
            }

            // ���� Ž������ ã��
            var index = grid.BinarySearch(null, Comparer<TickLine>.Create((a, b) => a.Tick.CompareTo(tick)));
            return index >= 0 ? grid[index] : null;
        }

        // ���� RectTransform�� ���� ����� TickLine�� ��ȯ�Ѵ�.
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

        public void ChangeGrid(int move)
        {
            gridCount += move;
            SetTickTexts(grid[0].Tick);

            OnAnimManagerTickChanged(_tick);
        }

        // �׸��� �����ϱ�
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

                grid[i].SetTick(start + i, i == 0);
            }
            for (var i = gridCount; i < grid.Count; i++)
            {
                grid[i].gameObject.SetActive(false);
            }

            OnGridChanged?.Invoke();
        }

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
