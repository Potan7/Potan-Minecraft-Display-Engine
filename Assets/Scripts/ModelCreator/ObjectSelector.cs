using UnityEngine;
using TransformHandles;
using UnityEngine.EventSystems;

public class ObjectSelector : MonoBehaviour, IPointerClickHandler
{
    public Handle handle;
    void Start()
    {
        handle = TransformHandleManager.Instance.CreateHandle(transform);
        
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Object selected: " + gameObject.name);

        handle.Enable(transform);
    }
}
