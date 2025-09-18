using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Shake Settings")]
    public float duration = 0.5f;       // Duración del shake
    public float magnitudePos = 10f;    // Magnitud de la posición
    public float magnitudeRot = 5f;     // Magnitud de la rotación en grados
    public float frequency = 20f;       // Velocidad del shake

    private Vector3 initialPos;
    private Quaternion initialRot;
    private float elapsed = 0f;
    private bool shaking = false;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        initialPos = transform.localPosition;
        initialRot = transform.localRotation;
    }

    void Update()
    {
        if (shaking)
        {
            elapsed += Time.deltaTime;
            if (elapsed > duration)
            {
                shaking = false;
                transform.localPosition = initialPos;
                transform.localRotation = initialRot;
                return;
            }

            float noiseX = Mathf.PerlinNoise(Time.time * frequency, 0f) - 0.5f;
            float noiseY = Mathf.PerlinNoise(0f, Time.time * frequency) - 0.5f;
            float noiseRot = Mathf.PerlinNoise(Time.time * frequency, Time.time * frequency) - 0.5f;

            transform.localPosition = initialPos + new Vector3(noiseX, noiseY, 0f) * magnitudePos;
            transform.localRotation = initialRot * Quaternion.Euler(0f, 0f, noiseRot * magnitudeRot);
        }
    }

    // Llamar a este método para iniciar el shake
    public void Shake()
    {
        elapsed = 0f;
        shaking = true;
    }
}
