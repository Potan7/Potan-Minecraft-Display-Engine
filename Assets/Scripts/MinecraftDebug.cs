using Riten.Native.Cursors;
using UnityEngine;

public class MinecraftDebug : MonoBehaviour
{
    private bool _toggle;
    private int _id;

    public void OnDebugButton()
    {
        //MinecraftModelData data = MinecraftFileManager.GetModelData("models/block/block.json");
        //Debug.Log(data.ToString());

        if (_toggle)
        {
            _toggle = false;
            _id = CursorStack.Push(NTCursors.ResizeVertical);
        }
        else
        {
            _toggle = true;
            CursorStack.Pop(_id);
        }

    }
}
