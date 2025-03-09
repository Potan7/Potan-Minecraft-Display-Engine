using System;
using UnityEngine;

public class AnimManager : BaseManager
{
    [SerializeField]
    private int _tick = 0;
    public int Tick
    {
        get => _tick;
        set
        {
            if (value < 0)
            {
                value = 0;
            }
            _tick = value;
            TickChanged?.Invoke(_tick);
        }
    }

    [SerializeField]
    private float _tickSpeed = 20.0f;
    public float TickSpeed
    {
        get => _tickSpeed;
        set
        {
            _tickSpeed = value;
            tickInterval = 1.0f / _tickSpeed; // 정확한 시간 간격 업데이트
        }
    }

    public static event Action<int> TickChanged;

    public bool IsPlaying { get; set; } = false;

    public Timeline Timeline;

    private float lastTickTime = 0f;  // 마지막 Tick 업데이트 시간
    private float tickInterval = 1.0f / 20.0f; // 초기 Tick 간격

    private void Start()
    {
        tickInterval = 1.0f / _tickSpeed; // 초기 TickSpeed 반영
        lastTickTime = Time.time; // 시작 시간 기록
    }

    private void Update()
    {
        if (IsPlaying)
        {
            if (Time.time - lastTickTime >= tickInterval)
            {
                lastTickTime = Time.time; // 현재 시간 업데이트
                Tick++; // Tick 증가
            }
        }
    }

    public void TickAdd(int value)
    {
        Tick += value;
    }
}
