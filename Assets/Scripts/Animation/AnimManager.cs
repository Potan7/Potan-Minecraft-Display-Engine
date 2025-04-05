using System;
using GameSystem;
using UnityEngine;
using UnityEngine.Serialization;
using Animation.UI;

namespace Animation
{
    public class AnimManager : BaseManager
    {
        // 현재 애니메이션 틱 (0.5 단위 사용)
        [SerializeField]
        private float _tick;
        public float Tick
        {
            get => _tick;
            set
            {
                if (value < 0f)
                {
                    value = 0f;
                }
                _tick = value;
                TickChanged?.Invoke(_tick);
            }
        }

        private float _tickInterval;

        [SerializeField]
        private float tickUnit = 0.5f;
        public float TickUnit
        {
            get => tickUnit;
            set
            {
                tickUnit = Mathf.Max(0.001f, value); // 최소값 제한
                RecalculateTickInterval();
            }
        }


        [SerializeField]
        private float tickSpeed = 20.0f;
        public float TickSpeed
        {
            get => tickSpeed;
            set
            {
                tickSpeed = Mathf.Max(0.01f, value); // 0 방지
                RecalculateTickInterval();
            }
        }

        private float _tickTimer = 0f;

        public static event Action<float> TickChanged;

        public bool IsPlaying { get; set; }

        [FormerlySerializedAs("Timeline")]
        public Timeline timeline;

        private void RecalculateTickInterval()
        {
            _tickInterval = tickUnit / tickSpeed;
        }

        private void Start()
        {
            RecalculateTickInterval();
        }

        private void FixedUpdate()
        {
            if (IsPlaying)
            {
                _tickTimer += Time.fixedDeltaTime;

                while (_tickTimer >= _tickInterval)
                {
                    _tickTimer -= _tickInterval;
                    Tick += tickUnit;
                }
            }
        }

        public void TickAdd(float value)
        {
            Tick += value;
        }

        private void OnDestroy()
        {
            TickChanged = null;
        }
    }
}
