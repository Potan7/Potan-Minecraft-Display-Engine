using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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
    public static event Action<int> TickChanged;
    Coroutine tickCoroutine;
    WaitForSeconds wait;

    [SerializeField]
    private bool _isPlaying = false;
    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            _isPlaying = value;
            if (_isPlaying)
            {
                tickCoroutine = StartCoroutine(TickCoroutine());
            }
            else
            {
                StopCoroutine(tickCoroutine);
            }
        }
    }

    public Timeline Timeline;
    
    private void Start()
    {
        wait = new WaitForSeconds(1.0f / 20.0f);
    }

    IEnumerator TickCoroutine()
    {
        while (true)
        {
            Tick++;
            yield return wait;
        }
    }

    private void Update()
    {
        if (IsPlaying)
        {
            Tick++;
        }
    }

    public void TickAdd(int value)
    {
        Tick += value;
    }
}
