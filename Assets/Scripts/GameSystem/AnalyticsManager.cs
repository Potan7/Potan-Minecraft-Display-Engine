using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UnityConsent;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool isInitialized = false;
    async void Start()
    {
        if (Application.isEditor) return; // 에디터에서는 초기화하지 않음

        await UnityServices.InitializeAsync();
        EndUserConsent.SetConsentState(new ConsentState
        {
            AnalyticsIntent = ConsentStatus.Granted // 수집 허용
        });
        isInitialized = true;
    }
}
