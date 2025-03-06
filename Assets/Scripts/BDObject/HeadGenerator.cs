using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Minecraft;

public class HeadGenerator : BlockModelGenerator
{
    public enum HeadType
    {
        PLAYER,
        PIGLIN,
        DRAGON,
        ZOMBIE,
        SKULL,
        WITHERSKULL,
        CREEPER,
        NONE,
    }
    const string DEFAULT_TEXTURE_PATH = "textures/entity/";

    public HeadType headType;
    public Texture2D headTexture;
    public string downloadUrl;

    public void GenerateHead(string name)
    {
        modelName = "head";

        headType = name switch
        {
            "player" => HeadType.PLAYER,
            "piglin" => HeadType.PIGLIN,
            "dragon" => HeadType.DRAGON,
            "zombie" => HeadType.ZOMBIE,
            "skeleton" => HeadType.SKULL,
            "wither_skeleton" => HeadType.WITHERSKULL,
            "creeper" => HeadType.CREEPER,
            _ => HeadType.NONE,
        };

        if (headType == HeadType.NONE)
        {
            CustomLog.LogError("헤드 타입이 잘못되었습니다.");
            return;
        }

        StartCoroutine(GenerateHeadCoroutine());
    }

    IEnumerator GenerateHeadCoroutine()
    {
        headTexture = headType switch
        {
            HeadType.PLAYER => SetPlayerTexture(),
            HeadType.PIGLIN => MinecraftFileManager.GetTextureFile(DEFAULT_TEXTURE_PATH + "piglin/piglin.png"),
            HeadType.DRAGON => MinecraftFileManager.GetTextureFile(DEFAULT_TEXTURE_PATH + "enderdragon/dragon.png"),
            HeadType.ZOMBIE => MinecraftFileManager.GetTextureFile(DEFAULT_TEXTURE_PATH + "zombie/zombie.png"),
            HeadType.SKULL => MinecraftFileManager.GetTextureFile(DEFAULT_TEXTURE_PATH + "skeleton/skeleton.png"),
            HeadType.WITHERSKULL => MinecraftFileManager.GetTextureFile(DEFAULT_TEXTURE_PATH + "skeleton/wither_skeleton.png"),
            HeadType.CREEPER => MinecraftFileManager.GetTextureFile(DEFAULT_TEXTURE_PATH + "creeper/creeper.png"),
            _ => null,
        };

        GameManager.GetManager<FileManager>().WorkingGenerators.Add(this);

        yield return new WaitWhile(() => headTexture == null);

        GameManager.GetManager<FileManager>().WorkingGenerators.Remove(this);

        switch (headType)
        {
            case HeadType.PLAYER:
                SetModel("item/player_head");
                break;
            case HeadType.ZOMBIE:
                SetModel("item/zombie_head");
                break;
            case HeadType.WITHERSKULL:
            case HeadType.SKULL:
            case HeadType.CREEPER:
                SetModel("item/creeper_head");
                break;
            case HeadType.PIGLIN:
                SetModel("item/piglin_head");
                break;
            case HeadType.DRAGON:
                SetModel("item/dragon_head");
                break;
        }
    }

    //static void SetPlayerSkin(Texture2D edit)
    //{
    //    int interval = edit.width/2;

    //    edit.filterMode = FilterMode.Point;
    //    edit.wrapMode = TextureWrapMode.Clamp;

    //    for (int i = 0; i < interval; i++)
    //    {
    //        for (int j = 0; j < interval; j++)
    //        {
    //            Color color = edit.GetPixel(i + interval, j+interval);

    //            if (color.a > 0)
    //            {
    //                //CustomLog.Log(color);
    //                edit.SetPixel(i, j + interval, color);
    //            }
    //        }
    //    }

    //    edit.Apply();
    //}



    protected override Texture2D CreateTexture(string path)
    {
        return headTexture;
    }

    //protected override bool CheckForTransparency(Texture2D texture)
    //{
    //    return false;
    //}

    //protected override void SetFaces(MinecraftModelData model, JObject element, MeshRenderer cubeObject)
    //{
    //    base.SetFaces(model, element, cubeObject);

    //    if (headType == HeadType.PLAYER)
    //    {
    //        int cnt = cubeObject.materials.Length;
    //        for (int i = 0; i < cnt; i++)
    //        {
    //            cubeObject.materials[i].EnableKeyword("_ALPHATEST_ON");
    //            cubeObject.materials[i].SetFloat("_AlphaClip", 1.0f);
    //        }
    //    }
    //}

    Texture2D SetPlayerTexture()
    {
        // BDObject 가져오기
        BDObject data = transform.parent.parent.GetComponent<BDObjectContainer>().BDObject;
        if (data.ExtraData.TryGetValue("defaultTextureValue", out object value))
        {
            try
            {
                byte[] jsonDataBytes = Convert.FromBase64String(value.ToString());
                string jsonString = Encoding.UTF8.GetString(jsonDataBytes);

                JObject jsonObject = JObject.Parse(jsonString);

                string url = jsonObject["textures"]?["SKIN"]?["url"]?.ToString();
                StartCoroutine(DownloadTexture(url));
                return null;
            }
            catch (Exception e)
            {
                CustomLog.LogError("플레이어 텍스쳐 다운로드 실패: " + e.Message);
            }
        }
        return MinecraftFileManager.GetTextureFile(DEFAULT_TEXTURE_PATH + "player/wide/steve.png");
    }

    IEnumerator DownloadTexture(string url)
    {
        url = url.Replace("http://", "https://");
        downloadUrl = url;
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
#if UNITY_EDITOR
            CustomLog.LogError("이미지 다운로드 실패: " + request.error);
#else
            CustomLog.LogError("이미지 다운로드 실패! 재시도합니다.");
#endif
            StartCoroutine(DownloadTexture(url));
        }
        else
        {
            Texture2D downloadedTexture = ((DownloadHandlerTexture)request.downloadHandler).texture;

            downloadedTexture.filterMode = FilterMode.Point;
            downloadedTexture.wrapMode = TextureWrapMode.Clamp;
            downloadedTexture.Apply();

            //SetPlayerSkin(downloadedTexture);
            //downloadedTexture.Apply();

            headTexture = downloadedTexture;

        }
    }
}
