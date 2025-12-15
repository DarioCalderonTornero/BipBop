using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DailyLuckAvatarPoolPreviewUI : MonoBehaviour
{
    [Header("Pool de avatares")]
    [SerializeField] private DailyLuckAvatarPoolSO pool; // <-- tu SO de avatares

    [Header("Slots (3)")]
    [SerializeField] private DailyLuckAvatarPreviewSlotUI[] slots = new DailyLuckAvatarPreviewSlotUI[3];

    [Header("Timing")]
    [SerializeField] private float changeEverySeconds = 5f;

    [Header("Owned Rules")]
    [SerializeField] private string defaultAvatarId = "NormalAvatar";

    private List<AvatarDataSO> list = new List<AvatarDataSO>();
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

        if (pool == null || pool.possibleAvatars == null) return;

        for (int i = 0; i < pool.possibleAvatars.Count; i++)
        {
            var a = pool.possibleAvatars[i];
            if (a != null && a.sprite != null && !string.IsNullOrEmpty(a.id))
                list.Add(a);
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

            // Cambian los 3 a la vez sin repetirse entre slots
            startIndex = (startIndex + 3) % list.Count;
            Apply3(instant: false);
        }
    }

    private void Apply3(bool instant)
    {
        if (slots == null || slots.Length == 0) return;

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
            string label = owned ? "En propiedad" : "¡Nuevo!";

            if (instant) slots[i].SetInstant(data.sprite, label);
            else slots[i].SetWithFade(data.sprite, label);
        }
    }

    private bool IsOwned(string id)
    {
        if (id == defaultAvatarId) return true;
        return PlayerPrefs.GetInt("AvatarPurchased_" + id, 0) == 1;
    }
}
