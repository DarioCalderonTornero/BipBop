using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class DailyStoreManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private StoreOfferSlotUI[] backgroundSlots; // 3
    [SerializeField] private StoreOfferSlotUI[] avatarSlots;     // 3
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Localization")]
    [SerializeField] private LocalizedString countdownPrefix;
    private string cachedPrefix = "Nuevas ofertas en:";

    [Header("Catalogs / Pools")]
    [SerializeField] private BackgroundCatalogSO backgroundCatalog;
    [SerializeField] private AvatarCatalogSO avatarCatalog;

    [Tooltip("Pool SOLO de avatares que se pueden comprar en tienda.")]
    [SerializeField] private DailyStoreAvatarPoolSO avatarStorePool;

    private const string PREF_DAY_KEY = "DailyStore_DayKey";
    private const string PREF_BG_OFFER_PREFIX = "DailyStore_BG_"; // + i
    private const string PREF_AV_OFFER_PREFIX = "DailyStore_AV_"; // + i

    private const string DEFAULT_BACKGROUND_ID = "DefaultBackground";
    private const string DEFAULT_AVATAR_ID = "NormalAvatar";


    private void OnEnable()
    {
        RefreshCountdownPrefix();
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(UnityEngine.Localization.Locale _)
    {
        RefreshCountdownPrefix();
        ApplyOffersToUI(); // repinta nombres
    }

    private async void RefreshCountdownPrefix()
    {
        if (countdownPrefix.IsEmpty)
            return;

        var handle = countdownPrefix.GetLocalizedStringAsync();
        await handle.Task;

        if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            cachedPrefix = handle.Result;
    }

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
                int price = data.price;

                int slotIndex = i; // capturar índice en variable local

                backgroundSlots[i].SetContent(
                    data.sprite,
                    title,
                    price,
                    () =>
                    {
                        TryBuyBackground(slotIndex, data);
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
                int price = data.price;
                int slotIndex = i;

                avatarSlots[i].SetContent(
                    data.sprite,
                    data.GetDisplayName(),
                    price,
                    () =>
                    {
                        TryBuyAvatar(slotIndex, data);
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

        countdownText.text = $"{cachedPrefix} {remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
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

    private void TryBuyBackground(int slotIndex, BackgroundDataSO data)
    {
        if (data == null) return;

        // Ya poseído, por seguridad
        if (IsBackgroundOwned(data.id))
        {
            Debug.Log("[DailyStore] Ya tienes este fondo: " + data.id);
            return;
        }

        int price = data.price;

        // Comprobar monedas
        if (!CurrencyManager.Instance.TrySpendCoins(price))
        {
            Debug.Log("[DailyStore] No hay monedas suficientes para fondo: " + data.id);
            // Aquí podrías mostrar un popup de "no tienes monedas"
            return;
        }

        // Marcar como comprado (mismo patrón que DailyLuckManager)
        PlayerPrefs.SetInt("Purchased_" + data.id, 1);
        PlayerPrefs.Save();

        XPUIAnimation ui = FindFirstObjectByType<XPUIAnimation>();
        if (ui != null)
        {
            ui.SyncAllPurchasedAvatarsToPlayFab(avatarCatalog);
            ui.SyncAllPurchasedBackgroundsToPlayFab(backgroundCatalog);
        }

        Debug.Log("[DailyStore] Fondo comprado: " + data.id);

        // Actualizar solo este slot a estado "owned"
        if (slotIndex >= 0 && slotIndex < backgroundSlots.Length && backgroundSlots[slotIndex] != null)
        {
            backgroundSlots[slotIndex].SetOwned(data.sprite);
        }
    }

    private void TryBuyAvatar(int slotIndex, AvatarDataSO data)
    {
        if (data == null) return;

        if (IsAvatarOwned(data.id))
        {
            Debug.Log("[DailyStore] Ya tienes este avatar: " + data.id);
            return;
        }

        int price = data.price;

        if (!CurrencyManager.Instance.TrySpendCoins(price))
        {
            Debug.Log("[DailyStore] No hay monedas suficientes para avatar: " + data.id);
            return;
        }

        PlayerPrefs.SetInt("AvatarPurchased_" + data.id, 1);
        PlayerPrefs.Save();

        XPUIAnimation ui = FindFirstObjectByType<XPUIAnimation>();
        if (ui != null)
        {
            ui.SyncAllPurchasedAvatarsToPlayFab(avatarCatalog);
            ui.SyncAllPurchasedBackgroundsToPlayFab(backgroundCatalog);
        }

        Debug.Log("[DailyStore] Avatar comprado: " + data.id);

        if (slotIndex >= 0 && slotIndex < avatarSlots.Length && avatarSlots[slotIndex] != null)
        {
            avatarSlots[slotIndex].SetOwned(data.sprite);
        }
    }
}
