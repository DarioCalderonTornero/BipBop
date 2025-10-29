using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.UI;

public class RewardedAdButton2 : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] private Button _showAdButton;
    [SerializeField] private string _androidAdUnitId = "Rewarded_Android";
    [SerializeField] private string _iOSAdUnitId = "Rewarded_iOS";
    private string _adUnitId;

    void Awake()
    {
#if UNITY_ANDROID
        _adUnitId = _androidAdUnitId;
#elif UNITY_IOS
        _adUnitId = _iOSAdUnitId;
#endif
        _showAdButton.interactable = false;
    }

    void Start()
    {
        LoadAd();
    }

    public void LoadAd()
    {
        Debug.Log(" Loading Ad: " + _adUnitId);
        Advertisement.Load(_adUnitId, this);
    }

    public void ShowAd()
    {
        Debug.Log(" Showing Ad: " + _adUnitId);
        Advertisement.Show(_adUnitId, this);
        _showAdButton.interactable = false;
    }

    // --- Callbacks ---
    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        Debug.Log(" Ad loaded: " + adUnitId);
        _showAdButton.interactable = true;
        _showAdButton.onClick.AddListener(ShowAd);
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.Log($" Error loading Ad Unit {adUnitId}: {error} - {message}");
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.Log($" Error showing Ad Unit {adUnitId}: {error} - {message}");
    }

    public void OnUnityAdsShowStart(string adUnitId) { }
    public void OnUnityAdsShowClick(string adUnitId) { }

    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            Debug.Log(" Reward granted!");
            //  Aquí colocas la recompensa que quieras dar al jugador
        }
        LoadAd();
    }
}
