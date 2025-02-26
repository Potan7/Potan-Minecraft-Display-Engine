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
    Vector2 InitPos;
    bool IsHiding = false;
    bool IsMouseEnter = false;

    public TextMeshProUGUI totalTickText;
    public TMP_InputField tickField;

    public Image playPauseButton;
    public Sprite playSprite;
    public Sprite pauseSprite;

    private void Start()
    {
        manager = GetComponent<AnimManager>();
        InitPos = dragPanel.rect.position;
        AnimManager.TickChanged += AnimManager_TickChanged;
    }

    public void OnTickFieldEndEdit(string value)
    {
        if (int.TryParse(value, out int t))
            manager.Tick = t;
        else
            tickField.text = manager.Tick.ToString();
    }

    private void AnimManager_TickChanged(int obj)
    {
        tickField.text = obj.ToString();
    }

    public void Stop()
    {
        manager.IsPlaying = false;
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
        Vector2 pos = dragPanel.rect.position;
        Vector2 target = new Vector2(pos.x, targetY);

        float time = 0;
        while (time < 1f)
        {
            pos = Vector2.Lerp(pos, target, 0.03f);
            dragPanel.SetPanelSize(pos);
            time += Time.deltaTime;
            yield return null;
        }
        dragPanel.SetPanelSize(target);
    }

    public void OnAnimPanelPointer(bool IsEnter)
    {
        BDEngineStyleCameraMovement.CanMoveCamera = !IsEnter;
        IsMouseEnter = IsEnter;
    }

    public void OnScrollWheel(InputAction.CallbackContext callback)
    {
        if (!IsMouseEnter) return;

        var scroll = callback.ReadValue<Vector2>();

        if (scroll.y > 0.1f)
        {
            manager.Timeline.ChangeGrid(1);
        }
        else if (scroll.y < -0.1f && manager.Timeline.GridCount > 20)
        {
            manager.Timeline.ChangeGrid(-1);
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
