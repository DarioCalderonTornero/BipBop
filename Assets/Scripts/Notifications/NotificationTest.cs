using System;
using UnityEngine;
using UnityEngine.Android;
using Unity.Notifications.Android;

public class NotificationTest : MonoBehaviour
{
    private const string ChannelId = "test_channel";

    private void Start()
    {
        // 1) Android 13+ permission
        if (!Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
            Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");

        // 2) Channel (Android 8+)
        var channel = new AndroidNotificationChannel
        {
            Id = ChannelId,
            Name = "Test",
            Importance = Importance.High,
            Description = "Test notifications"
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);

        // 3) Limpia anteriores para no duplicar
        AndroidNotificationCenter.CancelAllNotifications();
        AndroidNotificationCenter.CancelAllDisplayedNotifications();

        // 4) Programa una notificación en 10 segundos
        var notif = new AndroidNotification
        {
            Title = "TEST BipBop",
            Text = "Si ves esto, funciona ✅",
            FireTime = DateTime.Now.AddSeconds(10),
            SmallIcon = "ic_notification"
        };

        AndroidNotificationCenter.SendNotification(notif, ChannelId);

        Debug.Log("Notification scheduled for 10 seconds.");
    }
}
