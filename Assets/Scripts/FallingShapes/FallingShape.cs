using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingShape : MonoBehaviour
{
    private Rigidbody2D rb;
    private float rotationSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Rotación aleatoria entre -200 y 200 grados por segundo
        rotationSpeed = Random.Range(-200f, 200f);
        // Escala aleatoria (opcional)
        transform.localScale = Vector3.one * Random.Range(0.5f, 1.2f);
        // Color aleatorio
        GetComponent<SpriteRenderer>().color = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.9f, 1f);
    }

    private void Update()
    {
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}
