using System;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private int MUSIC_VOLUME_MAX = 10;
    public static int musicVolume = 4;

    private static float musicTime;

    public event EventHandler OnMusicVolumeChanged;

    private AudioSource musicAudioSource;

    [SerializeField] private AudioClip mainMusicAudioClip;
    [SerializeField] private AudioClip gamePausedMusicAudioClip;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        musicAudioSource = GetComponent<AudioSource>();
        musicAudioSource.volume = GetSoundVolumeNormalized();

        musicAudioSource.time = musicTime;

        musicAudioSource.clip = mainMusicAudioClip;

    }

    private void GameManager_OnGameResumed(object sender, EventArgs e)
    {
        PlayMusic(mainMusicAudioClip);
    }

    private void GameManager_OnGamePaused(object sender, EventArgs e)
    {
        PlayMusic(gamePausedMusicAudioClip);
    }

    private void PlayMusic(AudioClip clip)
    {
        if (clip == gamePausedMusicAudioClip)
        {
            musicAudioSource.clip = clip;
            musicAudioSource.time = 0f;
        }
        else
        {
            musicTime = musicAudioSource.time;
            musicAudioSource.clip = clip;
            musicAudioSource.time = musicTime;
        }

        musicAudioSource.Play();
    }
    void Update()
    {
        musicTime = musicAudioSource.time;
    }

    public void ChangeSoundVolume()
    {
        musicVolume = (musicVolume + 1) % MUSIC_VOLUME_MAX;
        musicAudioSource.volume = GetSoundVolumeNormalized();
        OnMusicVolumeChanged?.Invoke(this, EventArgs.Empty);
    }

    public int GetSoundVolume()
    {
        return musicVolume;
    }

    public float GetSoundVolumeNormalized()
    {
        return ((float)musicVolume) / MUSIC_VOLUME_MAX;
    }
}
