using UnityEngine;
using UnityEngine.EventSystems;
using GameSystem;

[RequireComponent(typeof(Collider))]
public class ObjectSelector : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"ObjectSelector: {gameObject.name} clicked");
        // HandleManager를 찾아서 선택 요청을 보냅니다.
        GameManager.GetManager<HandleManager>().SelectTarget(transform);
    }
}
