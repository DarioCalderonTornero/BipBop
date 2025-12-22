using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;

public class DailyLuckPoolPreviewUI : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private DailyLuckPoolSO pool;

    [Header("Slots (3)")]
    [SerializeField] private DailyLuckPreviewSlotUI[] slots = new DailyLuckPreviewSlotUI[3];

    [Header("Timing")]
    [SerializeField] private float changeEverySeconds = 5f;

    [Header("Owned Rules")]
    [SerializeField] private string defaultBackgroundId = "DefaultBackground";

    [Header("Localization (stateText)")]
    [SerializeField] private LocalizedString stateOwnedText; // ej: UI/owned
    [SerializeField] private LocalizedString stateNewText;   // ej: UI/new

    // Lista filtrada (solo válidos)
    private readonly List<BackgroundDataSO> list = new List<BackgroundDataSO>();
    private int startIndex = 0;
    private Coroutine routine;

    private void OnEnable()
    {
        RebuildList();
        Apply3(instant: true);

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(RotateRoutine());
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
    }

    public void RebuildList()
    {
        list.Clear();

        if (pool == null || pool.possibleBackgrounds == null) return;

        for (int i = 0; i < pool.possibleBackgrounds.Count; i++)
        {
            var b = pool.possibleBackgrounds[i];
            if (b != null && b.sprite != null && !string.IsNullOrEmpty(b.id))
                list.Add(b);
        }

        startIndex = 0;
    }

    private IEnumerator RotateRoutine()
    {
        var wait = new WaitForSecondsRealtime(changeEverySeconds);

        while (true)
        {
            yield return wait;

            if (list.Count == 0) continue;

            // avanzar “de 3 en 3” para que cambien los 3 a la vez sin repetirse
            startIndex = (startIndex + 3) % list.Count;
            Apply3(instant: false);
        }
    }

    private async void Apply3(bool instant)
    {
        if (slots == null || slots.Length == 0) return;

        // precargamos textos (para no pedirlos 3 veces)
        string ownedLabel = await GetLocalized(stateOwnedText);
        string newLabel = await GetLocalized(stateNewText);

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            if (list.Count == 0)
            {
                slots[i].SetInstant(null, "");
                continue;
            }

            var data = list[(startIndex + i) % list.Count];

            bool owned = IsOwned(data.id);
            string label = owned ? ownedLabel : newLabel;

            if (instant) slots[i].SetInstant(data.sprite, label);
            else slots[i].SetWithFade(data.sprite, label);
        }
    }

    private bool IsOwned(string id)
    {
        if (id == defaultBackgroundId) return true;
        return PlayerPrefs.GetInt("Purchased_" + id, 0) == 1;
    }

    private async Task<string> GetLocalized(LocalizedString ls)
    {
        if (ls.IsEmpty) return "";

        AsyncOperationHandle<string> handle = ls.GetLocalizedStringAsync();
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
            return handle.Result ?? "";

        return "";
    }
}
