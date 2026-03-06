using UnityEngine;

public static class AdsProgressManager
{
    private const string PREF_TOTAL_ADS_VIEWED = "TOTAL_ADS_VIEWED";

    public static int TotalAdsViewed => PlayerPrefs.GetInt(PREF_TOTAL_ADS_VIEWED, 0);

    public static void RegisterAdViewed()
    {
        int total = TotalAdsViewed + 1;
        PlayerPrefs.SetInt(PREF_TOTAL_ADS_VIEWED, total);
        PlayerPrefs.Save();

        Debug.Log($"[AdsProgressManager] Total ads viewed: {total}");
    }

    public static bool HasReachedAdsRequirement(int requiredAmount)
    {
        return TotalAdsViewed >= requiredAmount;
    }
}