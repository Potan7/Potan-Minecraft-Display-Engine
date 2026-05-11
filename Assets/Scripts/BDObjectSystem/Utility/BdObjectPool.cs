using System.Collections.Generic;
using UnityEngine;

namespace BDObjectSystem.Utility
{
    /// <summary>
    /// 간단한 오브젝트 풀링 클래스
    /// </summary>
    /// <typeparam name="T">MonoBehaviour를 상속하는 타입</typeparam>

    public class BdObjectPool<T> where T : MonoBehaviour
    {
        private readonly T prefab;
        private readonly Transform poolRoot;
        private readonly Stack<T> stack = new();

        public BdObjectPool(T prefab, int prewarm, Transform poolRoot = null)
        {
            this.prefab = prefab;
            this.poolRoot = poolRoot;
            prefab.gameObject.SetActive(false); // 비활성 프리팹
            for (int i = 0; i < prewarm; i++)
            {
                var inst = Object.Instantiate(prefab, poolRoot);
                inst.gameObject.SetActive(false);
                stack.Push(inst);
            }
        }

        public T Rent(Transform parent)
        {
            T inst = stack.Count > 0 ? stack.Pop() : Object.Instantiate(prefab, parent, false);
            if (inst.transform.parent != parent) inst.transform.SetParent(parent, false);
            // 여기선 여전히 비활성 상태(활성화는 나중에 일괄)
            return inst;
        }

        public void Return(T inst)
        {
            inst.gameObject.SetActive(false);
            inst.transform.SetParent(poolRoot, false);
            stack.Push(inst);
        }
    }
}
