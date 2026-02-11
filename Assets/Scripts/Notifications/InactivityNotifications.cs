using System;
using UnityEngine;
using UnityEngine.Android;
using Unity.Notifications.Android;
using UnityEngine.Localization.Settings;

public class InactivityNotifications : MonoBehaviour
{
    private const string ChannelId = "bipbop_inactivity";

    private const string NotifId1Key = "InactivityNotifId_1";
    private const string NotifId2Key = "InactivityNotifId_2";

    [Header("Delays (hours)")]
    [SerializeField] private int firstReminderHours = 24;
    [SerializeField] private int secondReminderHours = 72;

    private void Start()
    {
        // 1) Permiso (Android 13+)
        if (!Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
            Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");

        // 2) Channel
        var channel = new AndroidNotificationChannel
        {
            Id = ChannelId,
            Name = "BipBop reminders",
            Importance = Importance.Default,
            Description = "Come back reminders"
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);

        // 3) Si el jugador ha abierto el juego, cancelamos las nuestras
        CancelScheduledReminders();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            ScheduleInactivityReminders();
        }
        else
        {
            // Volvió a la app → cancela recordatorios
            CancelScheduledReminders();
        }
    }

    private void OnApplicationQuit()
    {
        ScheduleInactivityReminders();
    }

    private bool CanSendNotifications()
    {
        // En Android < 13 esto devolverá true aunque no pidas permiso.
        // En Android 13+ si el usuario deniega, será false.
        return Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS");
    }

    private void ScheduleInactivityReminders()
    {
        // Si no hay permiso, no hacemos nada
        if (!CanSendNotifications())
            return;

        // Limpia solo las nuestras antes de programar
        CancelScheduledReminders();

        bool isSpanish = LocalizationSettings.SelectedLocale != null &&
                         LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("es");

        // 24h
        int id1 = SendInHours(
            firstReminderHours,
            "BipBop",
            isSpanish ? "¡Te echamos de menos! ¿Una partida rápida?" : "We miss you! Want a quick round?"
        );
        PlayerPrefs.SetInt(NotifId1Key, id1);

        // 72h
        int id2 = SendInHours(
            secondReminderHours,
            isSpanish ? "Tu récord te espera 🏆" : "Your high score is waiting 🏆",
            isSpanish ? "Vuelve y trata de superarlo." : "Come back and beat it."
        );
        PlayerPrefs.SetInt(NotifId2Key, id2);

        PlayerPrefs.Save();
    }

    private int SendInHours(int hoursFromNow, string title, string text)
    {
        var n = new AndroidNotification
        {
            Title = title,
            Text = text,
            FireTime = DateTime.Now.AddHours(hoursFromNow),
            SmallIcon = "ic_notification"
            // LargeIcon = "ic_notification_large" // si lo añades al androidlib
        };

        return AndroidNotificationCenter.SendNotification(n, ChannelId);
    }

    private void CancelScheduledReminders()
    {
        // Cancelar solo las nuestras (no todas las del juego)
        if (PlayerPrefs.HasKey(NotifId1Key))
        {
            int id1 = PlayerPrefs.GetInt(NotifId1Key);
            AndroidNotificationCenter.CancelScheduledNotification(id1);
        }

        if (PlayerPrefs.HasKey(NotifId2Key))
        {
            int id2 = PlayerPrefs.GetInt(NotifId2Key);
            AndroidNotificationCenter.CancelScheduledNotification(id2);
        }

        // Opcional: también limpia las que estén mostradas (si quieres)
        AndroidNotificationCenter.CancelAllDisplayedNotifications();
    }
}
