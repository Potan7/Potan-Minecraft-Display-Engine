using UnityEngine;

namespace GameSystem
{
    public abstract class BaseManager : MonoBehaviour
    {
        // ��� ���� �Ŵ����� GameManager�� ����ϱ�
        protected virtual void Awake()
        {
            GameManager.Instance.RegisterManager(this);
        }
    }
}
