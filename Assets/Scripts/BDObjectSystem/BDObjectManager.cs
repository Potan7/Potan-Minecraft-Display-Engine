using System.Collections.Generic;
using UnityEngine;
using GameSystem;
using BDObjectSystem.Display;
using BDObjectSystem.Utility;
using Animation;
using Cysharp.Threading.Tasks;
using Minecraft;

namespace BDObjectSystem
{
    public class BdObjectManager : BaseManager
    {
        #region Variables
        // BDObjects Property
        [Header("BDObject Materials")]
        public Material bdobjBlockMaterial;
        public Material bdObjTransportMaterial;
        public Material bdObjHeadMaterial;

        [Header("Variables and Transforms")]
        public Transform bdObjectParent;
        public int bdObjectCount;
        public readonly Dictionary<string, BDObjectAnimator> BDObjectAnim = new();
        public BdObjectContainer currentBdObject;

        [Header("Prefabs")]
        public BdObjectContainer bdObjectPrefab;
        public BlockDisplay blockDisplay;
        public ItemDisplay itemDisplay;
        public TextDisplay textDisplay;

        public MeshRenderer cubePrefab;
        public ItemModelGenerator itemPrefab;
        public BlockModelGenerator blockPrefab;
        public HeadGenerator headPrefab;

        // 풀
        BdObjectPool<BdObjectContainer> containerPool;

        // 재사용 버퍼
        private readonly Stack<(BdObject data, Transform parent, BdObjectContainer parentCont)> work = new();
        private readonly List<BdObjectContainer> created = new(4096);
        private readonly List<BdObjectContainer> tmpChildren = new(64);


        #endregion

        protected override void AwakeAfter()
        {
            // 프리팹은 비활성 저장 필수!
            bdObjectPrefab.gameObject.SetActive(false);
            containerPool = new BdObjectPool<BdObjectContainer>(bdObjectPrefab, prewarm: 512);
        }

        #region Make BDObject

        public async UniTask AddObject(BdObject root, string fileName)
        {
            bdObjectCount = 0;

            // 부모 루트도 비활성로 준비
            if (bdObjectParent == null)
            {
                bdObjectParent = new GameObject("BDObjectParent").transform;
                bdObjectParent.localScale = new Vector3(1, 1, -1);
            }

            // **반복**으로 트리 펼치기(대량 생성, 비활성 유지)
            currentBdObject = await CreateHierarchyIterative(root, bdObjectParent);

            // 애니 등록
            BDObjectAnim[fileName] = new BDObjectAnimator(currentBdObject);

            // 마지막에 루트 한 번만 활성화 (OnEnable 폭탄 방지)
            // currentBdObject.gameObject.SetActive(true); // CreateHierarchyIterative에서 전체 활성화로 변경
            Debug.Log($"AddObject: {fileName}");
        }

        private async UniTask<BdObjectContainer> CreateHierarchyIterative(
            BdObject rootData, Transform parent, BdObjectContainer parentBdobj = null, int batchSize = 1024)
        {
            created.Clear();
            work.Clear();

            // 루트 push
            work.Push((rootData, parent, parentBdobj));

            int createdSinceLastYield = 0;

            BdObjectContainer rootContainer = null;

            try
            {
                while (work.Count > 0)
                {
                    var (data, p, parentCont) = work.Pop();

                    // 컨테이너 빌드(비활성 상태 유지)
                    var cont = containerPool.Rent(p);
                    cont.parent = parentCont;
                    cont.Init(data, this); // Init에서 OnEnable에 의존하지 않도록!
                    created.Add(cont);
                    bdObjectCount++;

                    if (rootContainer == null) rootContainer = cont;

                    // 자식 예약(스택: 후입선출이라 역순 push하면 원래 순서로 처리)
                    var children = data.Children;
                    if (children is { Length: > 0 })
                    {
                        // 부모-자식 링크를 위해, 나중 PostProcess에서 쓸 컨테이너 목록은 다시 모읍니다.
                        // 여기서는 일단 work에만 쌓아요.
                        for (int i = children.Length - 1; i >= 0; i--)
                        {
                            work.Push((children[i], cont.transform, cont));
                        }
                    }

                    // 배치 단위로 프레임 양보
                    if (++createdSinceLastYield >= batchSize)
                    {
                        createdSinceLastYield = 0;
                        await UniTask.Yield();
                    }
                }

                // 자식 컨테이너 배열 만들 때 “새 할당” 최소화
                // 각 컨테이너 내부에서 필요하면, children을 List로 받고 재사용하도록 수정 권장.
                // 여기서는 PostProcess가 children 필요하다고 하셨으니, 한 번 더 모읍니다.
                // 예: 부모->자식 맵을 만들거나, 각 부모가 생성 당시 kids를 저장하도록 바꾸면 더 빠릅니다.

                // 간단 예: 각 컨테이너가 self의 transform.children을 스캔하여 PostProcess에 넘김
                // (GO 수가 많으면 이 또한 비쌈. 가능하면 생성 시 child 리스트를 직접 갖고 있으세요.)

                foreach (var cont in created)
                {
                    tmpChildren.Clear();
                    var t = cont.transform;
                    for (int i = 0; i < t.childCount; i++)
                    {
                        if (t.GetChild(i).TryGetComponent<BdObjectContainer>(out var childCont)) tmpChildren.Add(childCont);
                    }

                    cont.gameObject.SetActive(true); // 전체 활성화는 여기서 한 번에
                    cont.PostProcess(tmpChildren.ToArray()); // 자식 목록의 복사본을 생성하여 전달
                }

                return rootContainer;
            }
            finally
            {
                // 필요 시 한 번만 동기화
                Physics.SyncTransforms();
            }
        }
        #endregion


        // 모든 BDObject 제거        
        public void ClearAllObject()
        {
            if (bdObjectParent != null)
            {
                // 자식들을 전부 풀로 반환
                var all = bdObjectParent.GetComponentsInChildren<BdObjectContainer>(includeInactive: true);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].gameObject.activeSelf)
                    {
                        all[i].ResetContainer();
                        containerPool.Return(all[i]);
                    }
                }
                // 루트만 유지
                bdObjectParent.DetachChildren();
            }
            BDObjectAnim.Clear();
            bdObjectCount = 0;
        }

        // BDObject 제거
        public void RemoveBdObject(string bdName)
        {
            if (BDObjectAnim.Remove(bdName, out var anim))
            {
                var cont = anim.RootObject;
                // 하위까지 전부 풀 반환
                var all = cont.GetComponentsInChildren<BdObjectContainer>(includeInactive: true);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].gameObject.activeSelf)
                    {
                        all[i].ResetContainer();
                        containerPool.Return(all[i]);
                    }
                }
                if (cont.gameObject.activeSelf)
                {
                    cont.ResetContainer();
                    containerPool.Return(cont);
                }
            }
        }
    }
}
