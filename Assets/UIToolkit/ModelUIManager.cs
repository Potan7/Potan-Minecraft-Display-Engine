using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ModelUIManager : MonoBehaviour
{
    UIDocument _uiDocument;
    VisualElement _rootElement;
    // ListView를 TreeView로 변경합니다.
    TreeView _hierarchyTreeView;

    public GameObject CubePrefab;
    public Transform MainObjectTransform;

    // TreeView에 사용할 데이터 리스트입니다.
    private List<TreeViewItemData<GameObject>> _treeViewItems = new List<TreeViewItemData<GameObject>>();
    private int _nextId = 0; // 각 아이템에 고유 ID를 부여하기 위한 카운터

    void Start()
    {
        _uiDocument = GetComponent<UIDocument>();
        _rootElement = _uiDocument.rootVisualElement;
        // Q<TreeView>로 쿼리합니다.
        _hierarchyTreeView = _rootElement.Q<TreeView>("HierarchyView");

        SetupHierarchyTreeView();

        _rootElement.Q<Button>("CreateCube").clicked += CreateCube;
    }

    void SetupHierarchyTreeView()
    {
        // TreeView의 각 아이템 UI를 생성하는 방법을 정의합니다.
        _hierarchyTreeView.makeItem = () => new Label();

        // 생성된 아이템 UI에 실제 데이터를 연결하는 방법을 정의합니다.
        _hierarchyTreeView.bindItem = (item, index) =>
        {
            // TreeView는 TreeViewItemData<T> 타입의 데이터를 사용합니다.
            var targetObject = _hierarchyTreeView.GetItemDataForIndex<GameObject>(index);
            (item as Label).text = targetObject.name;
        };

        // TreeView에 데이터 소스를 설정합니다.
        _hierarchyTreeView.SetRootItems(_treeViewItems);
    }

    void CreateCube()
    {
        GameObject newCube = Instantiate(CubePrefab, MainObjectTransform.position, Quaternion.identity, MainObjectTransform);
        newCube.name = "Cube " + _nextId;

        // TreeView에 추가할 새로운 아이템 데이터를 생성합니다.
        var newItemData = new TreeViewItemData<GameObject>(_nextId++, newCube);
        _treeViewItems.Add(newItemData);

        // TreeView를 새로고침하여 변경사항을 적용합니다.
        // _hierarchyTreeView.Rebuild(); // Rebuild() 대신 SetRootItems()를 다시 호출합니다.

        // 1. TreeView에게 업데이트된 데이터 소스를 다시 설정해줍니다.
        _hierarchyTreeView.SetRootItems(_treeViewItems);
        // 2. 변경사항을 즉시 반영하도록 뷰를 새로고칩니다.
        _hierarchyTreeView.RefreshItems();
    }

    // 참고: 자식으로 추가하는 방법 예시
    void CreateChildCube(int parentId)
    {
        // 1. 리스트에서 부모 아이템의 '인덱스'를 찾습니다.
        int parentIndex = _treeViewItems.FindIndex(item => item.id == parentId);
        if (parentIndex == -1) return; // 부모가 없으면 중단

        // 2. 인덱스를 이용해 기존 부모 아이템의 '복사본'을 가져옵니다.
        var oldParentItem = _treeViewItems[parentIndex];

        // 부모의 자식으로 큐브를 생성합니다.
        GameObject newCube = Instantiate(CubePrefab, oldParentItem.data.transform.position, Quaternion.identity, oldParentItem.data.transform);
        newCube.name = "Child Cube of " + parentId;

        var newChildItemData = new TreeViewItemData<GameObject>(_nextId++, newCube);

        // 3. 새로운 자식 리스트를 준비합니다.
        var newChildrenList = new List<TreeViewItemData<GameObject>>();
        // 기존에 자식이 있었다면, 새로운 리스트에 모두 추가합니다.
        if (oldParentItem.children != null)
        {
            foreach (var child in oldParentItem.children)
            {
                newChildrenList.Add(child);
            }
        }
        // 새로운 자식을 리스트에 추가합니다.
        newChildrenList.Add(newChildItemData);

        // 4. 자식 리스트가 업데이트된 '완전히 새로운' 부모 아이템을 생성합니다.
        var newParentItem = new TreeViewItemData<GameObject>(oldParentItem.id, oldParentItem.data, newChildrenList);

        // 5. 가장 중요한 단계: 새로 만든 부모 아이템으로 리스트의 기존 아이템을 덮어씁니다.
        _treeViewItems[parentIndex] = newParentItem;

        // 여기도 동일하게 SetRootItems와 RefreshItems를 사용합니다.
        _hierarchyTreeView.SetRootItems(_treeViewItems);
        _hierarchyTreeView.RefreshItems();
    }
}