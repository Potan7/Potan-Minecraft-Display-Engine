using UnityEngine;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Threading;
using System; // LINQ를 사용하기 위해 추가

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    // 슬라이스된 개별 스프라이트들을 저장할 Texture2D 배열
    // 에디터에서 미리 할당하거나, Resources.Load로 로드 후 가공할 수 있습니다.
    private Texture2D defaultCursorTexture;
    private Texture2D dragCursorTexture;

    public Texture2D[] loadingCursorAnimationFrames; // 로딩 애니메이션 프레임

    CancellationTokenSource loadingCancellationTokenSource = new CancellationTokenSource();
    public float loadingFrameRate = 0.1f; // 로딩 애니메이션 프레임 속도

    // [수정] 초기값을 -1로 설정하여 첫 SetCursor 호출이 항상 실행되도록 합니다.
    CursorType currentCursorType = (CursorType)(-1);

    public enum CursorType
    {
        Default,
        Drag,
        Loading,
    }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadTextures(); // 이미지 로드 및 처리
    }

    void Start()
    {
        SetCursorImg(CursorType.Default);
    }

    void LoadTextures()
    {
        // "cursor"라는 스프라이트 시트에서 슬라이스된 스프라이트들을 로드
        Sprite[] cursorSprites = Resources.LoadAll<Sprite>("UI/cursor");
        defaultCursorTexture = ConvertSpriteToTexture2D(cursorSprites[0]);
        dragCursorTexture = ConvertSpriteToTexture2D(cursorSprites[1]);

        // "loading"이라는 스프라이트 시트에서 로딩 애니메이션 프레임들을 로드
        Sprite[] loadingSprites = Resources.LoadAll<Sprite>("UI/loading");
        loadingCursorAnimationFrames = new Texture2D[loadingSprites.Length];
        for (int i = 0; i < loadingSprites.Length; i++)
        {
            loadingCursorAnimationFrames[i] = ConvertSpriteToTexture2D(loadingSprites[i]);
        }
    }

    // Sprite를 Texture2D로 변환하는 헬퍼 함수
    // 이 함수는 'Read/Write Enabled'가 켜져 있어야 제대로 동작합니다.
    private Texture2D ConvertSpriteToTexture2D(Sprite sprite)
    {
        if (sprite == null) return null;

        // [수정] 새로운 텍스처를 생성할 때, 압축되지 않은 RGBA32 형식으로 명시적으로 지정합니다.
        Texture2D newTexture = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, TextureFormat.RGBA32, false);

        Color[] pixels = sprite.texture.GetPixels((int)sprite.rect.x,
                                                  (int)sprite.rect.y,
                                                  (int)sprite.rect.width,
                                                  (int)sprite.rect.height);
        newTexture.SetPixels(pixels);
        newTexture.Apply();
        return newTexture;
    }

    // 일반 커서 설정 함수 (Default, Drag)
    public static void SetCursor(CursorType cursorType) => Instance.SetCursorImg(cursorType);
    private void SetCursorImg(CursorType cursorType)
    {
        if (currentCursorType == cursorType && cursorType != CursorType.Loading)
        {
            // 현재 커서 타입과 동일하면 아무 작업도 하지 않음
            // 단, 로딩은 중복 호출될 수 있으므로 예외
            return;
        }

        // [수정] 다른 커서로 변경 시, 진행 중인 로딩 애니메이션이 있다면 취소.
        if (currentCursorType == CursorType.Loading)
        {
            loadingCancellationTokenSource?.Cancel();
        }

        currentCursorType = cursorType;

        switch (cursorType)
        {
            case CursorType.Default:
                Cursor.SetCursor(defaultCursorTexture, Vector2.zero, CursorMode.Auto);
                break;
            case CursorType.Drag:
                Cursor.SetCursor(dragCursorTexture, Vector2.zero, CursorMode.Auto);
                break;
            case CursorType.Loading:
                // [수정] 로딩 커서 시작 로직을 여기로 이동
                // 이전 작업을 취소하고 새로운 CancellationTokenSource를 만듭니다.
                loadingCancellationTokenSource?.Cancel();
                loadingCancellationTokenSource?.Dispose();
                loadingCancellationTokenSource = new CancellationTokenSource();

                // 새로운 토큰을 전달하며 비동기 메서드를 시작합니다.
                SetLoadingCursorAsync(loadingCancellationTokenSource.Token).Forget();
                break;
            default:
                // Debug.LogError($"'{cursorType}' 타입의 커서 텍스처를 찾을 수 없습니다.");
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                break;
        }
    }

    async UniTask SetLoadingCursorAsync(CancellationToken cancellationToken)
    {
        if (loadingCursorAnimationFrames == null || loadingCursorAnimationFrames.Length == 0)
        {
            Debug.LogError("로딩 커서 애니메이션 프레임이 설정되지 않았습니다.");
            return;
        }

        try
        {
            while (true)
            {
                foreach (var frame in loadingCursorAnimationFrames)
                {
                    // [수정] 취소 요청이 오면 즉시 OperationCanceledException을 발생시킵니다.
                    cancellationToken.ThrowIfCancellationRequested();

                    Cursor.SetCursor(frame, Vector2.zero, CursorMode.Auto);

                    // [수정] Delay에 CancellationToken을 전달하여 즉시 취소될 수 있도록 합니다.
                    await UniTask.Delay(TimeSpan.FromSeconds(loadingFrameRate), cancellationToken: cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 취소 요청이 오면 여기로 와서 조용히 작업을 종료합니다.
            // Debug.Log("로딩 커서 애니메이션 취소됨.");
        }
    }

    void OnDisable()
    {
        loadingCancellationTokenSource?.Cancel();
        loadingCancellationTokenSource?.Dispose();
    }

    // // 게임 내에서 커서 변경을 테스트용 디버그
    // void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.Alpha1))
    //     {
    //         SetCursor(CursorType.Default);
    //     }
    //     if (Input.GetKeyDown(KeyCode.Alpha2))
    //     {
    //         SetCursor(CursorType.Drag);
    //     }
    //     if (Input.GetKeyDown(KeyCode.Alpha3))
    //     {
    //         SetCursor(CursorType.Loading);
    //     }
    // }
}