using DG.Tweening.Core.Easing;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private int MUSIC_VOLUME_MAX = 10;
    public static int musicVolume = 4;

    private static float musicTime;

    public event EventHandler OnMusicVolumeChanged;

    private AudioSource musicAudioSource;

    [SerializeField] private AudioClip menuSceneMusicAudio;
    [SerializeField] private AudioClip bipbopSceneMusicAudio;
    [SerializeField] private AudioClip colorSceneMusicAudio;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
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

        musicAudioSource = GetComponent<AudioSource>();
        SceneManager.sceneLoaded += SceneManager_OnSceneLoaded;
    }

    private void SceneManager_OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case "Menu":
                PlayMusic(menuSceneMusicAudio);
                break;
            case "GameScene":
                PlayMusic(bipbopSceneMusicAudio);
                break;
            case "ColorScene":
                PlayMusic(colorSceneMusicAudio);
                break;
            default:
                musicAudioSource.Stop();
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

    public int GetMusicVolume()
    {
        return musicVolume;
    }

    public float GetMusicVolumeNormalized()
    {
        return ((float)musicVolume) / MUSIC_VOLUME_MAX;
    }
}
