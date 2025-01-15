using UnityEngine;

public class UIManger : RootManager
{
    FileManager fileManager;
    private void Start()
    {
        fileManager = GameManager.GetManager<FileManager>();
    }

    public void OnPressImportButton()
    {
        fileManager.ImportFile();
    }
}
