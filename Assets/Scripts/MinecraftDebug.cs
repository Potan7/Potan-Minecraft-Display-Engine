using UnityEngine;
using Minecraft;
public class MinecraftDebug : MonoBehaviour
{
    public void OnDebugButton()
    {
        MinecraftModelData data = MinecraftFileManager.GetModelData("models/block/block.json");
        Debug.Log(data.ToString());
    }
}
