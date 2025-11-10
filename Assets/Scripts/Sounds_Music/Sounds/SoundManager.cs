using System;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    public event EventHandler OnSoundVolumeChanged;

    private const int SOUND_VOLUME_MAX = 10;
    private const string PREFS_VOLUME = "SoundVolume";
    private const string PREFS_PREVIOUS_VOLUME = "PreviousSoundVolume";
    private const string PREFS_MUTED = "SoundIsMuted";
    private const string PREFS_STARTED_SOUND_PLAYED = "StartedSoundPlayed";

    private int soundVolume = 6;

    private float soundVolumeNormalized
    {
        get { return (float)soundVolume / SOUND_VOLUME_MAX; }
    }

    [SerializeField] private AudioClip onColorGameModePoint;

    private bool isVolumeCancel = false;
    private int previousVolume = -1;

    private bool startedSoundPlayed = false;

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
            return;
        }

        if (PlayerPrefs.HasKey(PREFS_VOLUME))
            soundVolume = PlayerPrefs.GetInt(PREFS_VOLUME);
        else
            PlayerPrefs.SetInt(PREFS_VOLUME, soundVolume);

        if (PlayerPrefs.HasKey(PREFS_MUTED))
            isVolumeCancel = PlayerPrefs.GetInt(PREFS_MUTED) == 1;

        if (PlayerPrefs.HasKey(PREFS_PREVIOUS_VOLUME))
            previousVolume = PlayerPrefs.GetInt(PREFS_PREVIOUS_VOLUME);

        if (PlayerPrefs.HasKey(PREFS_STARTED_SOUND_PLAYED))
            startedSoundPlayed = PlayerPrefs.GetInt(PREFS_STARTED_SOUND_PLAYED) == 1;

        ApplyVolumeToAudio();
    }

    private void Start()
    {
        if (onColorGameModePoint != null)
        {
            //AudioSource.PlayClipAtPoint(onColorGameModePoint, Camera.main.transform.position, soundVolumeNormalized);
            startedSoundPlayed = true;
            PlayerPrefs.SetInt(PREFS_STARTED_SOUND_PLAYED, 1);
        }

        ColorGamePuntos.OnColorAddScore += ColorGamePuntos_OnColorAddScore;
    }

    private void ColorGamePuntos_OnColorAddScore(object sender, EventArgs e)
    {
        if (onColorGameModePoint != null && Camera.main != null)
            AudioSource.PlayClipAtPoint(onColorGameModePoint, Camera.main.transform.position, soundVolumeNormalized);
    }

    public void ChangeSoundVolume()
    {
        soundVolume = (soundVolume + 1) % (SOUND_VOLUME_MAX + 1);
        SaveVolume();
        ApplyVolumeToAudio();
        OnSoundVolumeChanged?.Invoke(this, EventArgs.Empty);
    }

    public int GetSoundVolume()
    {
        return soundVolume;
    }

    public float GetSoundVolumeNormalized()
    {
        return (float)soundVolume / (float)SOUND_VOLUME_MAX;
    }

    public int GetCancelVolume()
    {
        if (GetSoundVolumeNormalized() == 0)
            isVolumeCancel = true;
        if (!isVolumeCancel)
        {
            previousVolume = soundVolume;
            soundVolume = 0;
            isVolumeCancel = true;

            PlayerPrefs.SetInt(PREFS_PREVIOUS_VOLUME, previousVolume);
            PlayerPrefs.SetInt(PREFS_MUTED, 1);
            SaveVolume();

            ApplyVolumeToAudio();

            OnSoundVolumeChanged?.Invoke(this, EventArgs.Empty);
            return soundVolume;
        }
        else
        {
            if (previousVolume >= 0)
                soundVolume = previousVolume;

            previousVolume = -1;
            isVolumeCancel = false;

            PlayerPrefs.DeleteKey(PREFS_PREVIOUS_VOLUME);
            PlayerPrefs.SetInt(PREFS_MUTED, 0);
            SaveVolume();

            ApplyVolumeToAudio();

            OnSoundVolumeChanged?.Invoke(this, EventArgs.Empty);
            return soundVolume;
        }
    }

    private void SaveVolume()
    {
        PlayerPrefs.SetInt(PREFS_VOLUME, soundVolume);
        PlayerPrefs.Save();
    }

    public void RestoreVolumeTo(int value)
    {
        soundVolume = Mathf.Clamp(value, 0, SOUND_VOLUME_MAX);
        isVolumeCancel = (soundVolume == 0);
        SaveVolume();
        ApplyVolumeToAudio();
        OnSoundVolumeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyVolumeToAudio()
    {
        AudioListener.volume = soundVolumeNormalized;
    }
}
