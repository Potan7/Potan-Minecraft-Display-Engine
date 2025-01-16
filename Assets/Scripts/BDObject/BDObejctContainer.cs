using Newtonsoft.Json.Linq;
using UnityEngine;

public class BDObejctContainer : MonoBehaviour
{
    public BDObject BDObject;

    public Matrix4x4 transformation;

    public BDObejctContainer Init(BDObject bdObject)
    {
        BDObject = bdObject;
        gameObject.name = bdObject.name;

        // 디스플레이라면
        if (bdObject.isBlockDisplay || bdObject.isItemDisplay)
        {
            //var block = Resources.Load<GameObject>("Prefab/Block");
            //var blockObj = Instantiate(block, transform);
            //blockObj.transform.localPosition = new Vector3(0.5f, 0.5f, 0.5f);

            // 이름과 상태 분리
            int typeStart = bdObject.name.IndexOf('[');
            if (typeStart == -1)
            {
                typeStart = bdObject.name.Length;
            }
            string name = bdObject.name.Substring(0, typeStart);
            string state = bdObject.name.Substring(typeStart);
            state = state.Replace("[", "").Replace("]", "");

            // 블록 디스플레이일 때
            if (bdObject.isBlockDisplay)
            {
                GameObject blockDisplay = new GameObject("BlockDisplay");
                blockDisplay.transform.SetParent(transform);
                blockDisplay.transform.localPosition = Vector3.zero;
                blockDisplay.AddComponent<BlockDisplay>().LoadBlockModel(name, state);
                //LoadBlockModel(name, state);
            }
            // 아이템 디스플레이일 때
            else
            {
                //LoadItemModel(name, state);
            }
        }

        // 자식 오브젝트를 추가
        var manager = GameManager.GetManager<BDObjectManager>();
        if (bdObject.children != null)
        {
            foreach (var child in BDObject.children)
            {
                manager.AddObject(transform, child);
            }
        }

        // 변환 행렬을 적용
        transformation = AffineTransformation.GetMatrix(BDObject.transforms);
        AffineTransformation.ApplyMatrixToTransform(transform, transformation);

        return this;
    }

    
}
