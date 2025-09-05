using UnityEngine;

namespace GameSystem
{
    public abstract class BaseManager : MonoBehaviour
    {
        protected void Awake()
        {
            GameManager.RegisterManager(this);
            AwakeAfter();
        }

        protected virtual void AwakeAfter()
        {
            // Override in derived classes if needed
        }
    }
}
