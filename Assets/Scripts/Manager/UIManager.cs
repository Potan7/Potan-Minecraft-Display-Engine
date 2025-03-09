using Riten.Native.Cursors;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManger : BaseManager
{
    FileManager fileManager;
    BDObjectManager BDObjectManager;

    public GameObject LoadingPanel;
    int cursorID;

    private void Start()
    {
        fileManager = GameManager.GetManager<FileManager>();
        BDObjectManager = GameManager.GetManager<BDObjectManager>();
    }

    public void SetLoadingPanel(bool isOn)
    {
        LoadingPanel.SetActive(isOn);
        if (isOn)
        {
            cursorID = CursorStack.Push(NTCursors.Busy);
        }
        else
        {
            CursorStack.Pop(cursorID);
        }
    }

    public void OnPressImportButton()
    {
        fileManager.ImportFile();
    }

    public void OnRealodButton()
    {
        DestroyImmediate(GameManager.Instance.gameObject);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
