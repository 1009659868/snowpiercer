using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HealthManager : MonoBehaviour,IDamageable
{
    [Header("Health Settings")]
    public float maxHP = 100f;
    [SerializeField] public float currentHealth;

    [Header("Damage Popup")]
    [SerializeField] protected GameObject damagePopupPrefab;

    [Header("Events")]
    public UnityEvent<float> OnTakeDamage; // 传递伤害值
    public UnityEvent OnDeath;
    protected virtual void Awake(){
        
    }
    protected virtual void Start()
    {
        currentHealth = maxHP;
    }
    protected virtual void Update(){
        if(currentHealth<=0){
            Die();
        }
    }
    public virtual void TakeDamage(float damage, Vector3 hitDirection)
    {
        if (currentHealth <= 0) return;

        currentHealth = Mathf.Max(currentHealth - damage, 0);
        ShowDamagePopup(damage);
        OnTakeDamage?.Invoke(damage);
        // Debug.Log("Take");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    protected virtual void ShowDamagePopup(float damage)
    {
        if (damagePopupPrefab)
        {
            // Debug.Log("show");
            var popup = Instantiate(damagePopupPrefab, transform.position + Vector3.up, Quaternion.identity);
            popup.GetComponent<DamagePopup>().SetDamage(damage);
            popup.transform.SetParent(transform);
        }
    }
    protected virtual void Die()
    {
        if(currentHealth > 0) return; // 防止多次触发
        OnDeath?.Invoke();
        currentHealth = -1; // 设置为无效值防止重复触发
        
    }
}
