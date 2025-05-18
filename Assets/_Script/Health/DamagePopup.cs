using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float fadeDuration = 1f;

    private Color originalColor;
    private float timer;
    private void Awake()
    {
        originalColor = textMesh.color;
        textMesh = GetComponent<TextMeshPro>();
    }

    public void SetDamage(float damage)
    {
        textMesh.text = "-"+damage.ToString("F0");
        timer = fadeDuration;
    }
    private void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        
        timer -= Time.deltaTime;
        float alpha = timer / fadeDuration;
        textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

        if (timer <= 0)
        {
            Destroy(gameObject);
        }
    }
}
