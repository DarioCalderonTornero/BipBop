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

    private int soundVolume = 5;

    private float soundVolumeNormalized => (float)soundVolume / SOUND_VOLUME_MAX;

    [Header("ColorGameSounds")]
    [SerializeField] private AudioClip onColorGameModePoint;

    [Header("DodgeGameSounds")]
    [SerializeField] private AudioClip dodgeEnemyCollideSound;

    [SerializeField] private AudioClip flyingCoinsAudioClip;

    [Header("AudioSources")]
    [SerializeField] private AudioSource soundAudioSource;

    private bool isVolumeCancel = false;
    private int previousVolume = -1;

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

        // Load volume
        if (PlayerPrefs.HasKey(PREFS_VOLUME))
        {
            soundVolume = PlayerPrefs.GetInt(PREFS_VOLUME);
            soundVolume = Mathf.Clamp(soundVolume, 0, SOUND_VOLUME_MAX);
        }
        else
        {
            PlayerPrefs.SetInt(PREFS_VOLUME, soundVolume);
            PlayerPrefs.Save();
        }

        // Load previous volume (for unmute)
        if (PlayerPrefs.HasKey(PREFS_PREVIOUS_VOLUME))
            previousVolume = PlayerPrefs.GetInt(PREFS_PREVIOUS_VOLUME);

        // Load muted flag
        if (PlayerPrefs.HasKey(PREFS_MUTED))
            isVolumeCancel = PlayerPrefs.GetInt(PREFS_MUTED) == 1;

        // Apply master volume to AudioSource (settings)
        ApplyMasterToSource();
    }

    private void Start()
    {
        ColorGamePuntos.OnColorAddScore += ColorGamePuntos_OnColorAddScore;

        // Ensure master volume is applied (in case AudioSource was assigned late)
        ApplyMasterToSource();

        OnSoundVolumeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyMasterToSource()
    {
        if (soundAudioSource == null) return;

        // Master volume from settings (0..1)
        soundAudioSource.volume = soundVolumeNormalized;
    }

    private void ColorGamePuntos_OnColorAddScore(object sender, EventArgs e)
    {
        PlaySound(onColorGameModePoint, 1.0f);
    }

    /// <summary>
    /// Plays a sound using:
    /// - AudioSource.volume as MASTER (settings)
    /// - baseVolume as per-clip relative volume (mixing)
    /// Final output = master * baseVolume.
    /// </summary>
    public void PlaySound(AudioClip audioClip, float baseVolume)
    {
        if (audioClip == null || soundAudioSource == null) return;

        soundAudioSource.PlayOneShot(audioClip, Mathf.Clamp01(baseVolume));
    }

    public void PlayDodgeSound()
    {
        PlaySound(dodgeEnemyCollideSound, 1f);
    }

    public void PlayFlyingCoinSound()
    {
        PlaySound(flyingCoinsAudioClip, 1f);
    }

    public void ChangeSoundVolume()
    {
        soundVolume = (soundVolume + 1) % (SOUND_VOLUME_MAX + 1);
        SaveVolume();

        ApplyMasterToSource();

        OnSoundVolumeChanged?.Invoke(this, EventArgs.Empty);
    }

    public int GetSoundVolume()
    {
        return soundVolume;
    }

    public float GetSoundVolumeNormalized()
    {
        return soundVolumeNormalized;
    }

    public int GetCancelVolume()
    {
        // Safety: if already 0, consider muted
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
            ApplyMasterToSource();

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
            ApplyMasterToSource();

            OnSoundVolumeChanged?.Invoke(this, EventArgs.Empty);
            return soundVolume;
        }
    }

    public void RestoreVolumeTo(int value)
    {
        soundVolume = Mathf.Clamp(value, 0, SOUND_VOLUME_MAX);
        isVolumeCancel = (soundVolume == 0);

        SaveVolume();
        ApplyMasterToSource();

        OnSoundVolumeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SaveVolume()
    {
        PlayerPrefs.SetInt(PREFS_VOLUME, soundVolume);
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        ColorGamePuntos.OnColorAddScore -= ColorGamePuntos_OnColorAddScore;
    }
}
