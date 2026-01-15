using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public static bool GlobalFreeze = false;

    public float speed = 3f;

    [Header("Rotación")]
    public float rotationSpeed = 90f;   // grados por segundo
    private float rotationDirection = 1f;

    [Header("Explosion")]
    public int explosionIndex = 0;

    private Transform player;

    private SpriteRenderer sr;
    private Color originalColor;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            originalColor = sr.color;
    }

    private void Start()
    {
        if (EnemyIndicator.Instance != null)
            EnemyIndicator.Instance.RegisterEnemy(transform);
    }

    public void Init(Transform playerTarget, float newSpeed, float rotSpeed, float rotDir)
    {
        player = playerTarget;
        speed = newSpeed;
        rotationSpeed = rotSpeed;
        rotationDirection = rotDir;
    }

    private void Update()
    {
        // ❄️ Si estamos en cámara lenta de muerte, no se mueve ningún enemigo
        if (GlobalFreeze)
            return;

        if (player == null) return;

        Vector3 dir = (player.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        // Rotación constante
        transform.Rotate(0f, 0f, rotationSpeed * rotationDirection * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // Colisión entre enemigos (suma score y los destruye)
            if (DodgeManager.Instance != null)
            {
                DodgeManager.Instance.EnemiesCollided(this.gameObject, other.gameObject);
                SoundManager.Instance.PlayDodgeSound();
            }

            if (EnemyIndicator.Instance != null)
                EnemyIndicator.Instance.UnregisterEnemy(transform);
        }
        else if (other.CompareTag("Player"))
        {
            if (DodgeManager.Instance != null)
            {
                DodgeManager.Instance.PlayerHit(this);
            }

            // Opcional: ya podemos quitar el indicador de este enemigo
            if (EnemyIndicator.Instance != null)
                EnemyIndicator.Instance.UnregisterEnemy(transform);
        }
    }

    // 🔴 Corrutina de parpadeo en rojo usada por DodgeManager
    public IEnumerator FlashRedCoroutine(int times, float interval)
    {
        if (sr == null)
            yield break;

        for (int i = 0; i < times; i++)
        {
            sr.color = Color.red;
            yield return new WaitForSecondsRealtime(interval);
            sr.color = originalColor;
            yield return new WaitForSecondsRealtime(interval);
        }

        sr.color = originalColor;
    }
}
