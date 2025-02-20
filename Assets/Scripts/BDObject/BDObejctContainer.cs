using Newtonsoft.Json.Linq;
using UnityEngine;

public class BDObejctContainer : MonoBehaviour
{
    public BDObject BDObject;
    public DisplayObject displayObj;

    public Matrix4x4 transformation;

    public void Init(BDObject bdObject, BDObjectManager manager)
    {
        // 정보 저장
        BDObject = bdObject;
        gameObject.name = bdObject.name;

        // 디스플레이라면
        if (bdObject.isBlockDisplay || bdObject.isItemDisplay || bdObject.isTextDisplay)
        {
            // 이름과 상태 추출
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
                // 블록 디스플레이 자식으로 생성 후 모델 로드
                //GameObject blockDisplay = new GameObject("BlockDisplay");
                //blockDisplay.transform.SetParent(transform);
                //blockDisplay.transform.localPosition = Vector3.zero;
                //displayObj = blockDisplay.AddComponent<BlockDisplay>().LoadDisplayModel(name, state);
                var obj = Instantiate(manager.blockDisplay, transform);
                obj.LoadDisplayModel(name, state);
                displayObj = obj;

                // blockDisplay의 위치를 좌측 하단에 맞춤
                obj.transform.localPosition = -obj.AABBBound.min/2;
            }
            // 아이템 디스플레이일 때
            else if (bdObject.isItemDisplay)
            {
                // 아이템 디스플레이 자식으로 생성 후 모델 로드
                //GameObject itemDisplay = new GameObject("ItemDisplay");
                //itemDisplay.transform.SetParent(transform);
                //itemDisplay.transform.localPosition = Vector3.zero;
                //displayObj = itemDisplay.AddComponent<ItemDisplay>();
                var obj = Instantiate(manager.itemDisplay, transform);
                obj.LoadDisplayModel(name, state);
                displayObj = obj;
            }
            // 텍스트 디스플레이일 때
            else
            {
                var obj = Instantiate(manager.textDisplay, transform);
                obj.Init(bdObject);
                displayObj = obj;

            }
        }
    }

    // 후처리
    public void PostProcess()
    {
        // 변환 행렬을 적용
        transformation = AffineTransformation.GetMatrix(BDObject.transforms);
        AffineTransformation.ApplyMatrixToTransform(transform, transformation);

        //if (displayObj == null)
        //{
        //    transform.position = new Vector3(transform.position.x, transform.position.y, -transform.position.z);
        //}
    }

    
}
