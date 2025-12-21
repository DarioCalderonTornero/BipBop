using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DailyStoreManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private StoreOfferSlotUI[] backgroundSlots; // 3
    [SerializeField] private StoreOfferSlotUI[] avatarSlots;     // 3
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Catalogs / Pools")]
    [SerializeField] private BackgroundCatalogSO backgroundCatalog;

    [Tooltip("Pool SOLO de avatares que se pueden comprar en tienda.")]
    [SerializeField] private DailyStoreAvatarPoolSO avatarStorePool;

    private const string PREF_DAY_KEY = "DailyStore_DayKey";
    private const string PREF_BG_OFFER_PREFIX = "DailyStore_BG_"; // + i
    private const string PREF_AV_OFFER_PREFIX = "DailyStore_AV_"; // + i

    private const string DEFAULT_BACKGROUND_ID = "DefaultBackground";
    private const string DEFAULT_AVATAR_ID = "NormalAvatar";

    private void Start()
    {
        RefreshIfNeeded(force: true);
        ApplyOffersToUI();
    }

    private void Update()
    {
        UpdateCountdown();
        RefreshIfNeeded(force: false);
    }

    private void RefreshIfNeeded(bool force)
    {
        string todayKey = DateTime.Now.ToString("yyyyMMdd");
        bool needsRefresh = force || PlayerPrefs.GetString(PREF_DAY_KEY, "") != todayKey;

        if (!needsRefresh) return;

        GenerateDailyOffers(todayKey);

        PlayerPrefs.SetString(PREF_DAY_KEY, todayKey);
        PlayerPrefs.Save();

        ApplyOffersToUI();
    }

    private void GenerateDailyOffers(string dayKey)
    {
        int seed = dayKey.GetHashCode();
        System.Random rng = new System.Random(seed);

        // -------- Fondos --------
        if (backgroundCatalog != null && backgroundCatalog.backgroundDataSO != null && backgroundCatalog.backgroundDataSO.Count > 0)
        {
            var pickedBG = PickUniqueBackgrounds(backgroundCatalog.backgroundDataSO, 3, rng);

            for (int i = 0; i < 3; i++)
                PlayerPrefs.SetString(PREF_BG_OFFER_PREFIX + i, (i < pickedBG.Count && pickedBG[i] != null) ? pickedBG[i].id : "");
        }
        else
        {
            Debug.LogWarning("[DailyStore] backgroundCatalog vacío o no asignado.");
            for (int i = 0; i < 3; i++) PlayerPrefs.SetString(PREF_BG_OFFER_PREFIX + i, "");
        }

        // -------- Avatares (POOL tienda) --------
        if (avatarStorePool != null && avatarStorePool.possibleAvatars != null && avatarStorePool.possibleAvatars.Count > 0)
        {
            // Si quieres una última seguridad:
            List<AvatarDataSO> storeAvatars = avatarStorePool.possibleAvatars.FindAll(a =>
                a != null && a.unlockByScore == false
            );

            if (storeAvatars.Count == 0)
            {
                Debug.LogWarning("[DailyStore] Pool de avatares de tienda vacía (o filtrada).");
                for (int i = 0; i < 3; i++) PlayerPrefs.SetString(PREF_AV_OFFER_PREFIX + i, "");
            }
            else
            {
                var pickedAV = PickUniqueAvatars(storeAvatars, 3, rng);

                for (int i = 0; i < 3; i++)
                    PlayerPrefs.SetString(PREF_AV_OFFER_PREFIX + i, (i < pickedAV.Count && pickedAV[i] != null) ? pickedAV[i].id : "");
            }
        }
        else
        {
            Debug.LogWarning("[DailyStore] avatarStorePool vacío o no asignado.");
            for (int i = 0; i < 3; i++) PlayerPrefs.SetString(PREF_AV_OFFER_PREFIX + i, "");
        }
    }

    private void ApplyOffersToUI()
    {
        // -------- Fondos --------
        for (int i = 0; i < backgroundSlots.Length; i++)
        {
            if (backgroundSlots[i] == null) continue;

            string id = PlayerPrefs.GetString(PREF_BG_OFFER_PREFIX + i, "");
            var data = FindBackground(id);

            if (data == null)
            {
                backgroundSlots[i].SetContent(null, "—", 0, null);
                backgroundSlots[i].SetInteractable(false);
                continue;
            }

            bool owned = IsBackgroundOwned(data.id);

            if (owned)
            {
                backgroundSlots[i].SetOwned(data.sprite);
            }
            else
            {
                string title = string.IsNullOrEmpty(data.name) ? "Fondo" : data.name;

                backgroundSlots[i].SetContent(
                    data.sprite,
                    title,
                    data.price,
                    () =>
                    {
                        if (PreviewFondos.Instance != null)
                            PreviewFondos.Instance.ShowPreview(data);
                        else
                            Debug.Log("[DailyStore] Click fondo: " + data.id);
                    }
                );
            }
        }

        // -------- Avatares --------
        for (int i = 0; i < avatarSlots.Length; i++)
        {
            if (avatarSlots[i] == null) continue;

            string id = PlayerPrefs.GetString(PREF_AV_OFFER_PREFIX + i, "");
            var data = FindAvatar(id);

            if (data == null)
            {
                avatarSlots[i].SetContent(null, "—", 0, null);
                avatarSlots[i].SetInteractable(false);
                continue;
            }

            bool owned = IsAvatarOwned(data.id);

            if (owned)
            {
                avatarSlots[i].SetOwned(data.sprite);
            }
            else
            {
                avatarSlots[i].SetContent(
                    data.sprite,
                    data.displayName,
                    data.price,
                    () =>
                    {
                        Debug.Log("[DailyStore] Click avatar: " + data.id);
                        // PreviewAvatars.Instance.ShowPreview(data);
                    }
                );
            }
        }
    }

    private void UpdateCountdown()
    {
        if (countdownText == null) return;

        DateTime now = DateTime.Now;
        DateTime nextMidnight = now.Date.AddDays(1);
        TimeSpan remaining = nextMidnight - now;
        if (remaining.TotalSeconds < 0) remaining = TimeSpan.Zero;

        countdownText.text = $"Nuevas ofertas en:{remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
    }

    private bool IsBackgroundOwned(string id)
    {
        if (id == DEFAULT_BACKGROUND_ID) return true;
        return PlayerPrefs.GetInt("Purchased_" + id, 0) == 1;
    }

    private bool IsAvatarOwned(string id)
    {
        if (id == DEFAULT_AVATAR_ID) return true;
        return PlayerPrefs.GetInt("AvatarPurchased_" + id, 0) == 1;
    }

    private List<BackgroundDataSO> PickUniqueBackgrounds(List<BackgroundDataSO> source, int count, System.Random rng)
    {
        List<BackgroundDataSO> copy = new List<BackgroundDataSO>();
        for (int i = 0; i < source.Count; i++)
            if (source[i] != null) copy.Add(source[i]);

        Shuffle(copy, rng);
        return copy.GetRange(0, Mathf.Min(count, copy.Count));
    }

    private List<AvatarDataSO> PickUniqueAvatars(List<AvatarDataSO> source, int count, System.Random rng)
    {
        List<AvatarDataSO> copy = new List<AvatarDataSO>();
        for (int i = 0; i < source.Count; i++)
            if (source[i] != null) copy.Add(source[i]);

        Shuffle(copy, rng);
        return copy.GetRange(0, Mathf.Min(count, copy.Count));
    }

    private void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = rng.Next(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private BackgroundDataSO FindBackground(string id)
    {
        if (backgroundCatalog == null || backgroundCatalog.backgroundDataSO == null || string.IsNullOrEmpty(id))
            return null;

        return backgroundCatalog.backgroundDataSO.Find(b => b != null && b.id == id);
    }

    private AvatarDataSO FindAvatar(string id)
    {
        if (avatarStorePool == null || avatarStorePool.possibleAvatars == null || string.IsNullOrEmpty(id))
            return null;

        return avatarStorePool.possibleAvatars.Find(a => a != null && a.id == id);
    }
}
