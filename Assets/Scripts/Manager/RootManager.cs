using UnityEngine;

public abstract class RootManager : MonoBehaviour
{
    protected virtual void Awake()
    {
        GameManager.Instance.RegisterManager(this);
    }
}
