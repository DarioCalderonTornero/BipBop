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

    // Volumen actual (0..SOUND_VOLUME_MAX). Lo mantengo NO static porque singleton evita duplicados,
    // pero lo cargamos/guardamos en PlayerPrefs para persistencia.
    private int soundVolume = 6;

    [SerializeField] private AudioClip onColorGameModePoint;

    private bool isVolumeCancel = false;
    private int previousVolume = -1;

    // Marca para que el sonido de inicio solo se reproduzca la primera vez
    private bool startedSoundPlayed = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        // Cargar volumen guardado si existe
        if (PlayerPrefs.HasKey(PREFS_VOLUME))
            soundVolume = PlayerPrefs.GetInt(PREFS_VOLUME);
        else
            PlayerPrefs.SetInt(PREFS_VOLUME, soundVolume);

        // Cargar estado de mute/restaurar si existe
        if (PlayerPrefs.HasKey(PREFS_MUTED))
            isVolumeCancel = PlayerPrefs.GetInt(PREFS_MUTED) == 1;

        // Si había previousVolume guardado (por si algo falló), lo cargamos
        if (PlayerPrefs.HasKey(PREFS_PREVIOUS_VOLUME))
            previousVolume = PlayerPrefs.GetInt(PREFS_PREVIOUS_VOLUME);

        if (PlayerPrefs.HasKey(PREFS_STARTED_SOUND_PLAYED))
            startedSoundPlayed = PlayerPrefs.GetInt(PREFS_STARTED_SOUND_PLAYED) == 1;
    }

    private void Start()
    {        
         AudioSource.PlayClipAtPoint(onColorGameModePoint, Camera.main.transform.position, GetSoundVolumeNormalized());
         startedSoundPlayed = true;
         PlayerPrefs.SetInt(PREFS_STARTED_SOUND_PLAYED, 1);

        ColorGamePuntos.OnColorAddScore += ColorGamePuntos_OnColorAddScore1;
    }

    private void ColorGamePuntos_OnColorAddScore1(object sender, EventArgs e)
    {
        Debug.Log("OnColorPointsSoundAdded");
        AudioSource.PlayClipAtPoint(onColorGameModePoint, Camera.main.transform.position, GetSoundVolumeNormalized());
    }

    // Cambia el volumen en +1 y lo guarda
    public void ChangeSoundVolume()
    {
        soundVolume = (soundVolume + 1) % (SOUND_VOLUME_MAX + 1);
        SaveVolume();
        OnSoundVolumeChanged?.Invoke(this, EventArgs.Empty);
    }

    public int GetSoundVolume()
    {
        return soundVolume;
    }

    // Returns 0f..1f
    public float GetSoundVolumeNormalized()
    {
        return (float)soundVolume / (float)SOUND_VOLUME_MAX;
    }

    // Alterna mute/restaurar y guarda el estado. Devuelve el volumen resultante.
    public int GetCancelVolume()
    {
        if (!isVolumeCancel)
        {
            // Paso a silencio: guardar el volumen actual y poner a 0
            previousVolume = soundVolume;
            soundVolume = 0;
            isVolumeCancel = true;

            // Guardamos previousVolume y estado mute
            PlayerPrefs.SetInt(PREFS_PREVIOUS_VOLUME, previousVolume);
            PlayerPrefs.SetInt(PREFS_MUTED, 1);
            SaveVolume();

            OnSoundVolumeChanged?.Invoke(this, EventArgs.Empty);
            return soundVolume;
        }
        else
        {
            // Restaurar volumen anterior (si existe), si no, dejar en 0
            if (previousVolume >= 0)
            {
                soundVolume = previousVolume;
            }

            // limpiar marcador
            previousVolume = -1;
            isVolumeCancel = false;

            PlayerPrefs.DeleteKey(PREFS_PREVIOUS_VOLUME);
            PlayerPrefs.SetInt(PREFS_MUTED, 0);
            SaveVolume();

            OnSoundVolumeChanged?.Invoke(this, EventArgs.Empty);
            return soundVolume;
        }
    }

    // Guardar volumen en PlayerPrefs
    private void SaveVolume()
    {
        PlayerPrefs.SetInt(PREFS_VOLUME, soundVolume);
        PlayerPrefs.Save();
    }

    // Método público para forzar restaurar el volumen (útil desde UI)
    public void RestoreVolumeTo(int value)
    {
        soundVolume = Mathf.Clamp(value, 0, SOUND_VOLUME_MAX);
        isVolumeCancel = (soundVolume == 0);
        SaveVolume();
        OnSoundVolumeChanged?.Invoke(this, EventArgs.Empty);
    }
}
