using UnityEngine;

public abstract class RootManager : MonoBehaviour
{
    // 모든 게임 매니저를 GameManager에 등록하기
    protected virtual void Awake()
    {
        GameManager.Instance.RegisterManager(this);
    }
}
