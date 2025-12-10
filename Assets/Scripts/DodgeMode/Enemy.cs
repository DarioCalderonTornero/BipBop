using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    // ✅ Flag global para congelar a TODOS los enemigos
    public static bool GlobalFreeze = false;

    public float speed = 3f;
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

    public void Init(Transform playerTarget, float newSpeed)
    {
        player = playerTarget;
        speed = newSpeed;
    }

    private void Update()
    {
        // ❄️ Si estamos en cámara lenta de muerte, no se mueve ningún enemigo
        if (GlobalFreeze)
            return;

        if (player == null) return;

        Vector3 dir = (player.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // Colisión entre enemigos (suma score y los destruye)
            if (DodgeManager.Instance != null)
                DodgeManager.Instance.EnemiesCollided(this.gameObject, other.gameObject);

            if (EnemyIndicator.Instance != null)
                EnemyIndicator.Instance.UnregisterEnemy(transform);
        }
        else if (other.CompareTag("Player"))
        {
            // 🔥 Toca al jugador -> secuencia de cámara lenta + parpadeo + GameOver
            if (DodgeManager.Instance != null)
                DodgeManager.Instance.PlayerHit(this);

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
