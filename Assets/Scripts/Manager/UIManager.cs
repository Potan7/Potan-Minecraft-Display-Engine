using UnityEngine;

public class UIManger : RootManager
{
    FileManager fileManager;
    BDObjectManager BDObjectManager;

    private void Start()
    {
        fileManager = GameManager.GetManager<FileManager>();
        BDObjectManager = GameManager.GetManager<BDObjectManager>();
    }

    public void OnPressImportButton()
    {
        fileManager.ImportFile();
    }

    public void OnPressClearButton()
    {
        BDObjectManager.ClearAllObject();
    }
}
