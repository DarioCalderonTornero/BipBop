using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingShapesManager : MonoBehaviour
{
    public static FallingShapesManager Instance { get; private set; }

    [Header("Falling Shape Settings")]
    public GameObject fallingShapePrefab;
    public int minShapesPerScore = 2;
    public int maxShapesPerScore = 5;
    public float spawnRangeX = 3.5f; // Ajusta al ancho de la pantalla
    public float spawnHeight = 6f;   // Altura desde donde caen (ajústalo según la cámara)

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnFallingShapes()
    {
        int count = Random.Range(minShapesPerScore, maxShapesPerScore + 1);

        for (int i = 0; i < count; i++)
        {
            float randomX = Random.Range(-spawnRangeX, spawnRangeX);
            Vector3 spawnPos = new Vector3(randomX, spawnHeight, 0f);
            GameObject shape = Instantiate(fallingShapePrefab, spawnPos, Quaternion.identity);

            // Pequeño impulso horizontal aleatorio
            Rigidbody2D rb = shape.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.AddForce(new Vector2(Random.Range(-2f, 2f), 0f), ForceMode2D.Impulse);
        }
    }
}
