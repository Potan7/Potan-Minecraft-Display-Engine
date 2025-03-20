using System;
using Manager;
using UnityEngine;
using UnityEngine.Serialization;
using Animation.UI;

namespace Animation
{
    public class AnimManager : BaseManager
    {
        // 현재 애니메이션 틱 
        [SerializeField]
        private int tick;
        public int Tick
        {
            get => tick;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                tick = value;
                TickChanged?.Invoke(tick);
            }
        }

        // 애니메이션 틱 속도
        private float tickSpeed = 20.0f;
        public float TickSpeed
        {
            get => tickSpeed;
            set
            {
                tickSpeed = value;
                _tickInterval = 1.0f / tickSpeed;
            }
        }
        
        private float _tickTimer = 0f;

        public static event Action<int> TickChanged;

        public bool IsPlaying { get; set; }

        [FormerlySerializedAs("Timeline")] public Timeline timeline;

        private float _tickInterval = 1.0f / 20.0f;

        private void Start()
        {
            _tickInterval = 1.0f / tickSpeed; // Tick Speed
        }

        private void FixedUpdate()
        {
            if (IsPlaying)
            {
                _tickTimer += Time.fixedDeltaTime;
                if (_tickTimer >= _tickInterval)
                {
                    _tickTimer -= _tickInterval; // 남은 시간을 보존
                    Tick++;
                }
            }
        }


        public void TickAdd(int value)
        {
            Tick += value;
        }

        private void OnDestroy()
        {
            TickChanged = null;
        }


    }
}
