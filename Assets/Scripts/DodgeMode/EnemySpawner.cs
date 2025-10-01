using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnInterval = 2f;
    public Transform player;

    private void Start()
    {
        InvokeRepeating(nameof(SpawnEnemy), 1f, spawnInterval);
    }

    void SpawnEnemy()
    {
        Vector2 spawnPos = GetRandomSpawnPosition();
        GameObject enemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        float enemySpeed = DodgeManager.Instance.CurrentEnemySpeed;
        enemy.Init(player, enemySpeed);
    }

    Vector2 GetRandomSpawnPosition()
    {
        Camera cam = Camera.main;
        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        int side = Random.Range(0, 4); // 0=izq,1=der,2=arriba,3=abajo
        switch (side)
        {
            case 0: return new Vector2(-camWidth / 2f - 2f, Random.Range(-camHeight / 2f, camHeight / 2f));
            case 1: return new Vector2(camWidth / 2f + 2f, Random.Range(-camHeight / 2f, camHeight / 2f));
            case 2: return new Vector2(Random.Range(-camWidth / 2f, camWidth / 2f), camHeight / 2f + 2f);
            case 3: return new Vector2(Random.Range(-camWidth / 2f, camWidth / 2f), -camHeight / 2f - 2f);
        }
        return Vector2.zero;
    }
}
