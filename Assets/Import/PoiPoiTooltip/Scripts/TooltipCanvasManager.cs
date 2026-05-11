using UnityEngine;

namespace PPTooltip
{
    /// <summary>
    /// 툴팁을 표시할 최상위 캔버스를 관리하는 싱글톤 클래스입니다.
    /// 씬에 이 컴포넌트를 가진 Canvas가 하나 있어야 합니다.
    /// </summary>
    public class TooltipCanvasManager : MonoBehaviour
    {
        private static TooltipCanvasManager _instance;
        public static TooltipCanvasManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<TooltipCanvasManager>();
                }
                return _instance;
            }
        }

        public Transform TooltipContainer => transform;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
        }
    }
}