using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class AnimPanel : MonoBehaviour
{
    AnimManager manager;

    public DragPanel dragPanel;
    public RectTransform animPanel;
    Vector2 InitPos;
    bool IsHiding = false;
    public bool IsMouseEnter = false;

    public TextMeshProUGUI totalTickText;
    public TMP_InputField tickField;
    public TMP_InputField tickSpeedField;

    public Image playPauseButton;
    public Sprite playSprite;
    public Sprite pauseSprite;

    private void Start()
    {
        manager = GetComponent<AnimManager>();
        animPanel = dragPanel.AnimPanel;
        InitPos = new Vector2(0, 225);
        AnimManager.TickChanged += AnimManager_TickChanged;

        IsHiding = true;
        dragPanel.SetPanelSize(0);
    }

    private void Update()
    {
        if (RectTransformUtility.RectangleContainsScreenPoint(
            animPanel, Input.mousePosition, null
            ))
        {
            IsMouseEnter = true;
            BDEngineStyleCameraMovement.CanMoveCamera = false;
        }
        else
        {
            IsMouseEnter = false;
            BDEngineStyleCameraMovement.CanMoveCamera = true;

        }
    }

    public void OnTickFieldEndEdit(string value)
    {
        if (int.TryParse(value, out int t))
            manager.Tick = t;
        else
            tickField.text = manager.Tick.ToString();
    }

    public void OnTickSpeedFieldEndEdit(string value)
    {
        if (float.TryParse(value, out float t))
            manager.TickSpeed = t;
        else
            tickSpeedField.text = manager.TickSpeed.ToString();
    }

    private void AnimManager_TickChanged(int obj)
    {
        tickField.text = obj.ToString();
    }

    public void Stop()
    {
        manager.IsPlaying = false;
        playPauseButton.sprite = playSprite;
        manager.Tick = 0;
    }

    public void PlayPause()
    {

        if (manager.IsPlaying)
        {
            playPauseButton.sprite = playSprite;
        }
        else
        {
            playPauseButton.sprite = pauseSprite;
        }
        manager.IsPlaying = !manager.IsPlaying;
    }

    public void TogglePanel()
    {
        if (IsHiding)
        {
            StopAllCoroutines();
            StartCoroutine(MovePanelCoroutine(InitPos.y));
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(MovePanelCoroutine(0));
        }

        IsHiding = !IsHiding;
    }

    IEnumerator MovePanelCoroutine(float targetY)
    {
        float pos = dragPanel.rect.position.y;
        float target = targetY;

        float time = 0;
        while (time < 1f)
        {
            pos = Mathf.Lerp(pos, target, 0.03f);
            dragPanel.SetPanelSize(pos);
            time += Time.deltaTime;
            yield return null;
        }
        dragPanel.SetPanelSize(target);
    }

    //public void OnAnimPanelPointer(bool IsEnter)
    //{
    //    BDEngineStyleCameraMovement.CanMoveCamera = !IsEnter;
    //    IsMouseEnter = IsEnter;
    //}

    public void OnScrollWheel(InputAction.CallbackContext callback)
    {
        if (!IsMouseEnter) return;

        var scroll = callback.ReadValue<Vector2>();

        if (scroll.y > 0.1f)
        {
            manager.Timeline.ChangeGrid(5);
        }
        else if (scroll.y < -0.1f && manager.Timeline.GridCount > 20)
        {
            manager.Timeline.ChangeGrid(-5);
        }

    }

    public void MoveTickLeft(InputAction.CallbackContext callback)
    {
        if (callback.started)
            manager.TickAdd(-1);
    }

    public void MoveTickRight(InputAction.CallbackContext callback)
    {
        if (callback.started)
            manager.TickAdd(1);
    }
}
