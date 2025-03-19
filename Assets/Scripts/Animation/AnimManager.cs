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
                _tickInterval = 1.0f / tickSpeed; // ��Ȯ�� �ð� ���� ������Ʈ
            }
        }

        public static event Action<int> TickChanged;

        public bool IsPlaying { get; set; }

        [FormerlySerializedAs("Timeline")] public Timeline timeline;

        private float _lastTickTime;  // ������ Tick ������Ʈ �ð�
        private float _tickInterval = 1.0f / 20.0f; // �ʱ� Tick ����

        private void Start()
        {
            _tickInterval = 1.0f / tickSpeed; // �ʱ� TickSpeed �ݿ�
            _lastTickTime = Time.time; // ���� �ð� ���
        }

        private void Update()
        {
            if (IsPlaying)
            {
                if (Time.time - _lastTickTime >= _tickInterval)
                {
                    _lastTickTime = Time.time; // ���� �ð� ������Ʈ
                    Tick++; // Tick ����
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
