using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private const int MUSIC_VOLUME_MAX = 10;
    private const string PREFS_VOLUME = "MusicVolume";
    private const string PREFS_PREVIOUS_VOLUME = "PreviousMusicVolume";
    private const string PREFS_MUTED = "MusicIsMuted";

    // Mantengo tu estático para no romper nada externo
    public static int musicVolume = 5;

    private static float musicTime;

    public event EventHandler OnMusicVolumeChanged;

    private AudioSource musicAudioSource;

    [Header("Music Clips")]
    [SerializeField] private AudioClip menuSceneMusicClip;
    [SerializeField] private AudioClip bipbopSceneMusicClip;
    [SerializeField] private AudioClip colorSceneMusicClip;
    [SerializeField] private AudioClip geometricSceneMusicClip;
    [SerializeField] private AudioClip dodgeSceneMusicClip;
    [SerializeField] private AudioClip gridSceneMusicClip;

    [Header("Global Music Multiplier (Design)")]
    [SerializeField, Range(0f, 1f)]
    private float masterMusicMultiplier = 0.5f; 

    // Cancel Music stuff
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

        // ---- CARGA DE PREFERENCIAS ----
        if (PlayerPrefs.HasKey(PREFS_VOLUME))
        {
            musicVolume = PlayerPrefs.GetInt(PREFS_VOLUME);
            musicVolume = Mathf.Clamp(musicVolume, 0, MUSIC_VOLUME_MAX);
        }
        else
        {
            PlayerPrefs.SetInt(PREFS_VOLUME, musicVolume);
            PlayerPrefs.Save();
        }

        if (PlayerPrefs.HasKey(PREFS_PREVIOUS_VOLUME))
            previousVolume = PlayerPrefs.GetInt(PREFS_PREVIOUS_VOLUME);

        if (PlayerPrefs.HasKey(PREFS_MUTED))
            isMusicCancel = PlayerPrefs.GetInt(PREFS_MUTED) == 1;
    }

    private void Start()
    {
        musicAudioSource = GetComponent<AudioSource>();

        ApplyMusicVolume();

        musicAudioSource.clip = menuSceneMusicClip;
        musicAudioSource.Play();

        SceneManager.sceneLoaded += SceneManager_OnSceneLoaded;

        // Notificamos el volumen actual para que la UI pueda actualizarse al inicio
        OnMusicVolumeChanged?.Invoke(this, EventArgs.Empty);
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
            case "GeometricScene":
                PlayMusic(geometricSceneMusicClip);
                break;
            case "DodgeScene":
                PlayMusic(dodgeSceneMusicClip);
                break;
            case "GridScene":
                PlayMusic(gridSceneMusicClip);
                break;
            default:
                Debug.Log("Music default state");
                break;
        }
    }

    private void PlayMusic(AudioClip clip)
    {
        if (musicAudioSource == null || clip == null) return;

        // Por si el volumen cambió antes de cargar la escena o por prefs
        ApplyMusicVolume();

        float previousTime = musicAudioSource.time;

        if (clip == menuSceneMusicClip)
        {
            musicAudioSource.clip = clip;
            musicAudioSource.time = previousTime;
            musicAudioSource.Play();
            return;
        }

        musicAudioSource.clip = clip;
        musicAudioSource.time = 0f;
        musicAudioSource.Play();
    }

    private void Update()
    {
        if (musicAudioSource != null)
            musicTime = musicAudioSource.time;
    }

    /// <summary>
    /// Aplica el volumen final: (0..10 -> 0..1) * multiplicador global de diseño (por defecto 0.5)
    /// </summary>
    private void ApplyMusicVolume()
    {
        if (musicAudioSource == null) return;

        float normalized = GetMusicVolumeNormalized();
        float finalVol = normalized * masterMusicMultiplier;

        // Seguridad por si tocas el multiplier en inspector a valores raros
        musicAudioSource.volume = Mathf.Clamp01(finalVol);
    }

    public void ChangeMusicVolume()
    {
        // 0..MUSIC_VOLUME_MAX (incluido)
        musicVolume = (musicVolume + 1) % (MUSIC_VOLUME_MAX + 1);
        SaveVolume();

        ApplyMusicVolume();

        OnMusicVolumeChanged?.Invoke(this, EventArgs.Empty);
    }

    public int CancelMusicVolume()
    {
        // Si el volumen es 0, consideramos que está muteado
        if (GetMusicVolumeNormalized() == 0)
            isMusicCancel = true;

        if (!isMusicCancel)
        {
            // Muteamos
            previousVolume = musicVolume;
            musicVolume = 0;
            isMusicCancel = true;

            PlayerPrefs.SetInt(PREFS_PREVIOUS_VOLUME, previousVolume);
            PlayerPrefs.SetInt(PREFS_MUTED, 1);
            SaveVolume();

            ApplyMusicVolume();

            OnMusicVolumeChanged?.Invoke(this, EventArgs.Empty);
            return musicVolume;
        }
        else
        {
            // Desmuteamos
            if (previousVolume >= 0)
                musicVolume = previousVolume;

            previousVolume = -1;
            isMusicCancel = false;

            PlayerPrefs.DeleteKey(PREFS_PREVIOUS_VOLUME);
            PlayerPrefs.SetInt(PREFS_MUTED, 0);
            SaveVolume();

            ApplyMusicVolume();

            OnMusicVolumeChanged?.Invoke(this, EventArgs.Empty);
            return musicVolume;
        }
    }

    public void RestoreVolumeTo(int value)
    {
        musicVolume = Mathf.Clamp(value, 0, MUSIC_VOLUME_MAX);
        isMusicCancel = (musicVolume == 0);
        SaveVolume();

        ApplyMusicVolume();

        OnMusicVolumeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SaveVolume()
    {
        PlayerPrefs.SetInt(PREFS_VOLUME, musicVolume);
        PlayerPrefs.Save();
    }

    public int GetMusicVolume()
    {
        return musicVolume;
    }

    public float GetMusicVolumeNormalized()
    {
        return (float)musicVolume / MUSIC_VOLUME_MAX;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneManager_OnSceneLoaded;
    }
}
