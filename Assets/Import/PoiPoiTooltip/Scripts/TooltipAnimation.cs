using UnityEngine;
using UnityEngine.UI;
using System;
using LitMotion;
using LitMotion.Extensions;

namespace PPTooltip
{
    public class TooltipAnimation : IDisposable
    {
        public enum ANIMATION_TYPE
        {
            TO_TOP,
            TO_TOP_FADE,
            TO_TOP_EXPAND,
            TO_TOP_ATTACHED,
            TO_TOP_FUN,
            TO_BOTTOM,
            TO_BOTTOM_FADE,
            TO_BOTTOM_EXPAND,
            TO_BOTTOM_ATTACHED,
            TO_BOTTOM_FUN,
        }

        public const float FAST = 0.25f;
        public const float NORMAL = 0.35f;
        public const float SLOW = 0.55f;

        private const float MARGIN_Y = 1.13f;
        private RectTransform anchorRect; // 앵커(루트) RectTransform
        private RectTransform visualContainerRect; // 실제 움직일 대상
        private CanvasGroup tooltipCanvasGroup;
        private Vector2 parentRectSize;
        private ANIMATION_TYPE animationType;

        private MotionHandle handle;

        /// <summary>
        /// 초기화
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="cg"></param>
        /// <param name="parentSize"></param>
        /// <param name="type"></param>
        public TooltipAnimation(RectTransform rect, CanvasGroup cg, Vector2 parentSize, ANIMATION_TYPE type)
        {
            anchorRect = rect; // 앵커(루트) RectTransform
            tooltipCanvasGroup = cg;
            parentRectSize = parentSize;
            animationType = type;

            // 자식인 VisualContainer를 찾습니다. 프리팹 구조를 꼭 맞춰주세요.
            visualContainerRect = anchorRect.GetChild(0).GetComponent<RectTransform>();
            if (visualContainerRect == null)
            {
                Debug.LogError("Tooltip 프리팹에서 'VisualContainer' 자식을 찾을 수 없습니다. 구조를 확인해주세요.");
                return;
            }

            ResetTooltip();
        }

        /// <summary>
        /// ツールチップの表示アニメーションを開始
        /// </summary>
        public void PlayTooltip()
        {
            if (handle.IsActive()) handle.Cancel();
            ResetTooltip();

            // 목표 Y 위치 계산 (대상 UI의 절반 크기 + 툴팁의 절반 크기)
            float targetOffsetY = (parentRectSize.y) + (visualContainerRect.sizeDelta.y / 2f);

            // 모든 애니메이션의 대상을 'visualContainerRect'로 변경합니다.
            // 앵커는 버튼 위치에 고정되고, 그 자식인 visualContainerRect만 움직입니다.
            switch (animationType)
            {
                case ANIMATION_TYPE.TO_TOP:
                    handle = LSequence.Create()
                        .Append(LMotion.Create(0f, targetOffsetY, NORMAL).BindToLocalPositionY(visualContainerRect))
                        .Join(LMotion.Create(0f, 1.0f, FAST).BindToAlpha(tooltipCanvasGroup))
                        .Run();
                    break;
                case ANIMATION_TYPE.TO_TOP_FADE:
                    visualContainerRect.localPosition = Vector3.up * targetOffsetY;
                    handle = LMotion.Create(0f, 1.0f, NORMAL).BindToAlpha(tooltipCanvasGroup);
                    break;
                case ANIMATION_TYPE.TO_TOP_EXPAND:
                    visualContainerRect.localPosition = Vector3.up * targetOffsetY;
                    visualContainerRect.localScale = Vector3.one * 0.5f;
                    handle = LSequence.Create()
                        .Append(LMotion.Create(0f, 1.0f, SLOW).BindToAlpha(tooltipCanvasGroup))
                        .Join(LMotion.Create(Vector3.one * 0.5f, Vector3.one, NORMAL).WithEase(Ease.OutBack).BindToLocalScale(visualContainerRect))
                        .Run();
                    break;
                case ANIMATION_TYPE.TO_TOP_ATTACHED:
                    visualContainerRect.localPosition = Vector3.up * (targetOffsetY * MARGIN_Y);
                    handle = LSequence.Create()
                        .Append(LMotion.Create(visualContainerRect.localPosition.y, targetOffsetY, FAST).WithEase(Ease.InSine).BindToLocalPositionY(visualContainerRect))
                        .Join(LMotion.Create(0f, 1.0f, FAST).BindToAlpha(tooltipCanvasGroup))
                        .Run();
                    break;
                case ANIMATION_TYPE.TO_TOP_FUN:
                    visualContainerRect.localScale = new Vector3(1f, 0.5f, 1f);
                    handle = LSequence.Create()
                        .Append(LMotion.Create(0f, targetOffsetY * MARGIN_Y, SLOW).WithEase(Ease.OutBounce).BindToLocalPositionY(visualContainerRect))
                        .Join(LMotion.Create(new Vector3(1f, 0.5f, 1f), Vector3.one, SLOW).BindToLocalScale(visualContainerRect))
                        .Join(LMotion.Create(0f, 1.0f, FAST).BindToAlpha(tooltipCanvasGroup))
                        .Run();
                    break;
                case ANIMATION_TYPE.TO_BOTTOM:
                    handle = LSequence.Create()
                        .Append(LMotion.Create(0f, -targetOffsetY, NORMAL).BindToLocalPositionY(visualContainerRect))
                        .Join(LMotion.Create(0f, 1.0f, FAST).BindToAlpha(tooltipCanvasGroup))
                        .Run();
                    break;
                case ANIMATION_TYPE.TO_BOTTOM_FADE:
                    visualContainerRect.localPosition = Vector3.down * targetOffsetY;
                    handle = LMotion.Create(0f, 1.0f, NORMAL).BindToAlpha(tooltipCanvasGroup);
                    break;
                case ANIMATION_TYPE.TO_BOTTOM_EXPAND:
                    visualContainerRect.localPosition = Vector3.down * targetOffsetY;
                    visualContainerRect.localScale = Vector3.one * 0.5f;
                    handle = LSequence.Create()
                        .Append(LMotion.Create(0f, 1.0f, SLOW).BindToAlpha(tooltipCanvasGroup))
                        .Join(LMotion.Create(Vector3.one * 0.5f, Vector3.one, NORMAL).WithEase(Ease.OutBack).BindToLocalScale(visualContainerRect))
                        .Run();
                    break;
                case ANIMATION_TYPE.TO_BOTTOM_ATTACHED:
                    visualContainerRect.localPosition = Vector3.down * (targetOffsetY * MARGIN_Y);
                    handle = LSequence.Create()
                        .Append(LMotion.Create(visualContainerRect.localPosition.y, -targetOffsetY, FAST).WithEase(Ease.InSine).BindToLocalPositionY(visualContainerRect))
                        .Join(LMotion.Create(0f, 1.0f, FAST).BindToAlpha(tooltipCanvasGroup))
                        .Run();
                    break;
                case ANIMATION_TYPE.TO_BOTTOM_FUN:
                    visualContainerRect.localScale = new Vector3(1f, 0.5f, 1f);
                    handle = LSequence.Create()
                        .Append(LMotion.Create(0f, -targetOffsetY * MARGIN_Y, SLOW).WithEase(Ease.OutBounce).BindToLocalPositionY(visualContainerRect))
                        .Join(LMotion.Create(new Vector3(1f, 0.5f, 1f), Vector3.one, SLOW).BindToLocalScale(visualContainerRect))
                        .Join(LMotion.Create(0f, 1.0f, FAST).BindToAlpha(tooltipCanvasGroup))
                        .Run();
                    break;
            }
        }

        /// <summary>
        /// ツールチップの表示アニメーションを終了
        /// </summary>
        public void StopTooltip()
        {
            if (handle.IsActive())
            {
                handle.Cancel();
            }
        }

        /// <summary>
        /// ツールチップの状態リセット
        /// </summary>
        private void ResetTooltip()
        {
            // 리셋 대상도 visualContainerRect로 명확히 합니다.
            if (visualContainerRect != null)
            {
                visualContainerRect.localPosition = Vector3.zero;
                visualContainerRect.localScale = Vector3.one;
            }
            tooltipCanvasGroup.alpha = 0.0f;
        }

        public void Dispose()
        {
            if (handle.IsActive())
            {
                handle.Cancel();
            }
        }
    }
}
