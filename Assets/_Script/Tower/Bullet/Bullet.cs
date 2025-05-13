using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Bullet : MonoBehaviour, IBullet
{
    [Header("Base Settings")]
    [SerializeField] protected BulletType bulletType;
    [SerializeField] protected float speed = 30f;
    [SerializeField] protected float lifeTime = 3f;
    [SerializeField] protected float damage = 1;

    protected Transform target;
    protected Rigidbody rb;
    protected float activateTime;
    protected bool isActive;

    // 初始化组件
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();

    }

    // 对象池激活方法
    public virtual void Activate(Vector3 bulletPoint, Quaternion rotation, Transform target )
    {
        this.target = target;
        
        transform.SetPositionAndRotation(bulletPoint, rotation);
        gameObject.SetActive(true);
        
        activateTime = Time.time;
        isActive = true;
        
        InitializeMovement();
    }

    // 对象池回收方法
    public virtual void Deactivate()
    {
        isActive=false;
        if(rb!=null) {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        gameObject.SetActive(false);
    }

    // 初始化运动逻辑
    protected virtual void InitializeMovement()
    {
        if(rb!=null)
        rb.velocity = transform.forward * speed;
    }

    // 生命周期检测
    protected virtual void Update()
    {
        if (!isActive) return;

        if (Time.time - activateTime > lifeTime)
        {
            Deactivate();
            BulletPool.Instance.ReturnToPool(this);
        }

    }

    // 触发检测（基础版本）
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        if (other.CompareTag("Enemy"))
        {
            BulletPool.Instance.ReturnToPool(this);
            //ApplyDamage(other.GetComponent<IDamageable>());
            // Debug.Log("fuc");
            
        }
    }

    // 伤害处理
    protected virtual void ApplyDamage(IDamageable target)
    {
        target?.TakeDamage(damage);
    }

    public BulletType GetBulletType() => bulletType;
    public void SetBulletType(BulletType type){
        bulletType = type;
    }
    public void SetDamage(float _damage){
        damage=_damage;
    }
    public void SetSpeed(float _speed){
        speed=_speed;
    }
    public float GetActivateTime(){
        return activateTime;
    }
    public float GetLifeTime(){
        return lifeTime;
    }
}

public interface IBullet
{
    void Activate(Vector3 position, Quaternion rotation, Transform target = null);
    void Deactivate();
    BulletType GetBulletType();
}

public interface IDamageable
{
    void TakeDamage(float damage);
}
