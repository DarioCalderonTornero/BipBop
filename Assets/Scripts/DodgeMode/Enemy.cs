using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 3f;
    private Transform player;


    private void Start()
    {
        EnemyIndicator.Instance.RegisterEnemy(transform);
    }

    public void Init(Transform playerTarget, float newSpeed)
    {
        player = playerTarget;
        speed = newSpeed;
    }

    private void Update()
    {
        if (player == null) return;

        Vector3 dir = (player.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            DodgeManager.Instance.EnemiesCollided(this.gameObject, other.gameObject);

            if (EnemyIndicator.Instance != null)
                EnemyIndicator.Instance.UnregisterEnemy(transform);
        }
    }
}
