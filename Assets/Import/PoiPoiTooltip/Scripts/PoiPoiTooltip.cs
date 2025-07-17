using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PPTooltip {
	[RequireComponent(typeof(RectTransform))]
	[DisallowMultipleComponent]
	public class PoiPoiTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
	{
		// ツールチップとして表示したい要素
		[SerializeField]
		private RectTransform toolTipRect;
		// ツールチップの表示アニメーションタイプ
		[SerializeField]
		private TooltipAnimation.ANIMATION_TYPE playAnimationType;

		private RectTransform tooltipInstance;
		private TooltipAnimation tooltipAnimation;
		private Vector2 parentSize;

		[TextArea(3, 10)]
		public string tooltip;
		private void Start()
		{
			if (toolTipRect == null)
			{
				toolTipRect = Resources.Load<RectTransform>("UI/Tooltip");
			}

			InitTooltip();
		}

		/// <summary>
		/// ツールチップ静的生成時初期化
		/// </summary>
		private void InitTooltip()
		{
			// マウスイベントを受けないように停止
			enabled = false;

			CreateTooltip();
		}

		/// <summary>
		/// ツールチップ動的生成時初期化
		/// </summary>
		public void InitTooltip(RectTransform setTooltipRect, TooltipAnimation.ANIMATION_TYPE type)
		{
			// マウスイベントを受けないように停止
			enabled = false;

			toolTipRect = setTooltipRect;
			playAnimationType = type;

			CreateTooltip();
		}

		/// <summary>
		/// ツールチップの実体化
		/// </summary>
		private void CreateTooltip()
		{
			// 툴팁 캔버스 관리자를 찾습니다.
			if (TooltipCanvasManager.Instance == null)
			{
				Debug.LogError("씬에 TooltipCanvasManager가 없습니다. TooltipCanvas 오브젝트를 확인해주세요.");
				enabled = false;
				return;
			}

			// 툴팁을 이 오브젝트의 자식이 아닌, 전용 툴팁 캔버스의 자식으로 생성합니다.
			tooltipInstance = Instantiate(toolTipRect, TooltipCanvasManager.Instance.TooltipContainer);
			tooltipInstance.gameObject.SetActive(false);

			// 必要なコンポーネントを追加
			CanvasGroup cg = tooltipInstance.gameObject.GetComponent<CanvasGroup>();
			if (cg == null)
			{
				cg = tooltipInstance.gameObject.AddComponent<CanvasGroup>();
			}

			if (!string.IsNullOrEmpty(tooltip))
			{
				var text = tooltipInstance.GetComponentInChildren<TextMeshProUGUI>();
				if (text != null)
				{
					text.text = tooltip;
				}
			}

			parentSize = GetComponent<RectTransform>().sizeDelta;
			tooltipAnimation = new TooltipAnimation(tooltipInstance, cg, parentSize, playAnimationType);

			// マウスイベントを受け付けるようにする
			enabled = true;
		}

		/// <summary>
		/// Mouse Event
		/// </summary>
		public void OnPointerEnter(PointerEventData e)
		{
			ActiveTooltip();
		}
		public void OnSelect(BaseEventData e)
		{
			ActiveTooltip();
		}
		public void OnPointerExit(PointerEventData e)
		{
			InactiveTooltip();
		}
		public void OnDeselect(BaseEventData e)
		{
			InactiveTooltip();
		}

		/// <summary>
		/// ツールチップを表示
		/// </summary>
		private void ActiveTooltip()
		{
			// 1. 앵커의 위치를 대상 UI의 위치와 일치시킵니다.
			tooltipInstance.position = transform.position;
			// 2. 앵커의 크기를 대상 UI의 크기와 일치시킵니다.
			tooltipInstance.sizeDelta = parentSize;

			tooltipInstance.gameObject.SetActive(true);
			tooltipAnimation.PlayTooltip();
		}

		/// <summary>
		/// ツールチップを非表示
		/// </summary>
		private void InactiveTooltip()
		{
			tooltipAnimation.StopTooltip();
			tooltipInstance.gameObject.SetActive(false);
		}

        void OnDestroy()
        {
			tooltipAnimation?.Dispose();
        }
    }
}