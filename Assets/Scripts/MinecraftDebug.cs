using UnityEngine;
using Minecraft;
using Riten.Native.Cursors;
public class MinecraftDebug : MonoBehaviour
{
    bool toggle = false;
    int id;

    public void OnDebugButton()
    {
        //MinecraftModelData data = MinecraftFileManager.GetModelData("models/block/block.json");
        //Debug.Log(data.ToString());

        if (toggle)
        {
            toggle = false;
            id = CursorStack.Push(NTCursors.ResizeVertical);
        }
        else
        {
            toggle = true;
            CursorStack.Pop(id);
        }

    }
}
