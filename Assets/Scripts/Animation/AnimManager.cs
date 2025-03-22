using System;
using Manager;
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

        // 애니메이션 틱 속도 (1초당 몇 틱, 그대로 유지)
        private float tickSpeed = 20.0f;
        public float TickSpeed
        {
            get => tickSpeed;
            set
            {
                tickSpeed = value;
                _tickInterval = 1.0f / (tickSpeed * 2.0f); // 0.5틱 단위
            }
        }

        private float _tickTimer = 0f;
        private float _tickInterval = 1.0f / (20.0f * 2.0f); // 기본값

        public static event Action<float> TickChanged;

        public bool IsPlaying { get; set; }

        [FormerlySerializedAs("Timeline")] 
        public Timeline timeline;

        private void Start()
        {
            _tickInterval = 1.0f / (tickSpeed * 2.0f);
        }

        private void FixedUpdate()
        {
            if (IsPlaying)
            {
                _tickTimer += Time.fixedDeltaTime;

                while (_tickTimer >= _tickInterval)
                {
                    _tickTimer -= _tickInterval;
                    Tick += 0.5f;
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
