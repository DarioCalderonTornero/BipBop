using UnityEngine;
using UnityEngine.Advertisements;


public class RewardedAdButton : MonoBehaviour, IUnityAdsInitializationListener
{
    [SerializeField] private string _androidGameId = "5974898";
    [SerializeField] private string _iOSGameId = "5974899";
    [SerializeField] private bool _testMode = true;

    private string _gameId;

    void Awake()
    {
        InitializeAds();
    }

    public void InitializeAds()
    {
#if UNITY_ANDROID
        _gameId = _androidGameId;
#elif UNITY_IOS
        _gameId = _iOSGameId;
#endif
        Advertisement.Initialize(_gameId, _testMode, this);
    }

    public void OnInitializationComplete()
    {
        Debug.Log(" Unity Ads initialization complete.");
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.Log($" Unity Ads Initialization Failed: {error.ToString()} - {message}");
    }
}
