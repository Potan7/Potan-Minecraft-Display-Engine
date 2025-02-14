using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

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

    public void GenerateHead(string name)
    {
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

        yield return new WaitWhile(() => headTexture == null);

        switch (headType)
        {
            case HeadType.PLAYER:
            case HeadType.ZOMBIE:
                SetModel("item/head");
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

    protected override Texture2D CreateTexture(string path, JObject textures)
    {
        return headTexture;
    }

    Texture2D SetPlayerTexture()
    {
        // BDObject 가져오기
        BDObject data = transform.parent.parent.GetComponent<BDObejctContainer>().BDObject;
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
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                CustomLog.LogError("이미지 다운로드 실패: " + request.error);
            }
            else
            {
                Texture2D downloadedTexture = ((DownloadHandlerTexture)request.downloadHandler).texture;

                downloadedTexture.filterMode = FilterMode.Point;
                downloadedTexture.wrapMode = TextureWrapMode.Clamp;
                downloadedTexture.Apply();

                headTexture = downloadedTexture;
            }
        }
    }
}
