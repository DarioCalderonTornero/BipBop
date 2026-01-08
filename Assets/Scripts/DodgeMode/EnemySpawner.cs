using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] enemyPrefabs;      // lista de modelos de meteorito

    public float spawnInterval = 2f;
    public Transform player;

    [Header("Tamaño aleatorio")]
    public float minScale = 0.7f;
    public float maxScale = 1.3f;

    [Header("Rotación")]
    public float minRotSpeed = 60f;
    public float maxRotSpeed = 180f;

    private bool canSpawn = false;

    private void Start()
    {
        DodgeState.Instance.OnPlayingDodgeGame += StartSpawning;
        DodgeState.Instance.OnGameOverDodgeGame += StopSpawning;
    }

    private void OnDestroy()
    {
        if (DodgeState.Instance != null)
        {
            DodgeState.Instance.OnPlayingDodgeGame -= StartSpawning;
            DodgeState.Instance.OnGameOverDodgeGame -= StopSpawning;
        }
    }

    private void StartSpawning(object sender, System.EventArgs e)
    {
        if (canSpawn) return;

        canSpawn = true;
        StartCoroutine(SpawnLoop());
    }

    private void StopSpawning(object sender, System.EventArgs e)
    {
        canSpawn = false;
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(0.4f);

        while (canSpawn)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            return;

        // 1) Prefab aleatorio
        int index = Random.Range(0, enemyPrefabs.Length);

        Vector2 spawnPos;
        int side;
        GetRandomSpawnPosition(out spawnPos, out side);

        GameObject enemyObj = Instantiate(enemyPrefabs[index], spawnPos, Quaternion.identity);

        // 2) Tamaño aleatorio
        float scale = Random.Range(minScale, maxScale);
        enemyObj.transform.localScale = Vector3.one * scale;

        // 3) Rotación aleatoria y sentido según lado
        float rotSpeed = Random.Range(minRotSpeed, maxRotSpeed);
        float rotDir = (side == 0) ? 1f : -1f;
        // aquí side 0 = mitad izquierda, 1 = mitad derecha (ver función de abajo)

        Enemy enemy = enemyObj.GetComponent<Enemy>();
        float enemySpeed = DodgeManager.Instance.CurrentEnemySpeed;
        enemy.Init(player, enemySpeed, rotSpeed, rotDir);
    }

    // Devuelve posición y qué mitad horizontal se ha usado
    private void GetRandomSpawnPosition(out Vector2 pos, out int horizontalSide)
    {
        Camera cam = Camera.main;
        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        int side = Random.Range(0, 4); // 0=izq,1=der,2=arriba,3=abajo

        switch (side)
        {
            case 0: // izquierda
                pos = new Vector2(-camWidth / 2f - 2f, Random.Range(-camHeight / 2f, camHeight / 2f));
                horizontalSide = 0;
                break;
            case 1: // derecha
                pos = new Vector2(camWidth / 2f + 2f, Random.Range(-camHeight / 2f, camHeight / 2f));
                horizontalSide = 1;
                break;
            case 2: // arriba
                pos = new Vector2(Random.Range(-camWidth / 2f, camWidth / 2f), camHeight / 2f + 2f);
                // decidir mitad según X
                horizontalSide = (pos.x >= 0) ? 1 : 0;
                break;
            case 3: // abajo
                pos = new Vector2(Random.Range(-camWidth / 2f, camWidth / 2f), -camHeight / 2f - 2f);
                horizontalSide = (pos.x >= 0) ? 1 : 0;
                break;
            default:
                pos = Vector2.zero;
                horizontalSide = 0;
                break;
        }
    }
}
