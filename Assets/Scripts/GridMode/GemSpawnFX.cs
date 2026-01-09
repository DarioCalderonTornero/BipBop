using UnityEngine;

public class GemSpawnFX : MonoBehaviour
{
    [SerializeField] private ParticleSystem mainPs;
    [SerializeField] private ParticleSystem sparklesPs;

    public void Play(Color color)
    {
        if (mainPs != null)
        {
            var m = mainPs.main;
            m.startColor = color;
            mainPs.Play();
        }

        if (sparklesPs != null)
        {
            var m2 = sparklesPs.main;
            m2.startColor = color;
            sparklesPs.Play();
        }

        float maxLifetime = 0f;
        if (mainPs != null)
            maxLifetime = Mathf.Max(maxLifetime, mainPs.main.duration + mainPs.main.startLifetime.constantMax);
        if (sparklesPs != null)
            maxLifetime = Mathf.Max(maxLifetime, sparklesPs.main.duration + sparklesPs.main.startLifetime.constantMax);

        Destroy(gameObject, maxLifetime);
    }
}
