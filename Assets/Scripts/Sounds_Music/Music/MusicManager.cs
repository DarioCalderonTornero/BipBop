using DG.Tweening.Core.Easing;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private int MUSIC_VOLUME_MAX = 10;
    public static int musicVolume = 6;

    private static float musicTime;

    public event EventHandler OnMusicVolumeChanged;

    private AudioSource musicAudioSource;

    [SerializeField] private AudioClip menuSceneMusicClip;
    [SerializeField] private AudioClip bipbopSceneMusicClip;
    [SerializeField] private AudioClip colorSceneMusicClip;

    //Cancel Music stuff
    private bool isMusicCancel = false;
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

    }

    void Start()
    {
        musicAudioSource = GetComponent<AudioSource>();
        musicAudioSource.volume = GetMusicVolumeNormalized();
        musicAudioSource.clip = menuSceneMusicClip;
        musicAudioSource.Play();
        SceneManager.sceneLoaded += SceneManager_OnSceneLoaded;
    }

    private void SceneManager_OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case "Menu":
                PlayMusic(menuSceneMusicClip);
                break;
            case "GameScene":
                PlayMusic(bipbopSceneMusicClip);
                break;
            case "ColorScene":
                PlayMusic(colorSceneMusicClip);
                break;
            default:
                //musicAudioSource.Stop();
                Debug.Log("Music default state");
                break;
        }
    }

    private void PlayMusic(AudioClip clip)
    {
        musicAudioSource.clip = clip;
        musicAudioSource.time = 0f;
        musicAudioSource.Play();
    }
    void Update()
    {
        musicTime = musicAudioSource.time;
    }

    public void ChangeMusicVolume()
    {
        musicVolume = (musicVolume + 1) % MUSIC_VOLUME_MAX;
        musicAudioSource.volume = GetMusicVolumeNormalized();
        OnMusicVolumeChanged?.Invoke(this, EventArgs.Empty);
    }

    public int CancelMusicVolume()
    {
        if (GetMusicVolumeNormalized() == 0)
            isMusicCancel = true;

        if (!isMusicCancel)
        {
            previousVolume = musicVolume;
            musicVolume = 0;
            isMusicCancel = true;

            OnMusicVolumeChanged?.Invoke(this, EventArgs.Empty);

            return musicVolume;
        }

        else
        {
            if (previousVolume >= 0)
                musicVolume = previousVolume;

            previousVolume = -1;
            isMusicCancel = false;

            return musicVolume;
        }
    }

    public int GetMusicVolume()
    {
        return musicVolume;
    }

    public float GetMusicVolumeNormalized()
    {
        return ((float)musicVolume) / MUSIC_VOLUME_MAX;
    }
}
