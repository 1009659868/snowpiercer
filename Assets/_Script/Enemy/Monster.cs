using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public abstract class Monster : MonoBehaviour
{
    [Header("Base Settings")]
    public MonsterType type;
    [SerializeField] protected float maxHp = 100;       //最大血量
    [SerializeField] protected float attackPower = 10;  //攻击力
    [SerializeField] protected float moveSpeed = 3f;    //移动速度
    [SerializeField] protected float attackRange = 2f;  //攻击范围
    [SerializeField] protected float detectionRange = 10f;//追击范围

    [Header("Events")]
    public UnityEvent OnDeath;
    public UnityEvent OnTakeDamage;

    protected NavMeshAgent navAgent;
    protected Animator animator;
    protected float currentHp;
    protected bool isDead;
    protected Transform player;
    protected Rigidbody rb;
    public float MaxHp=>maxHp;
    public float CurrentHp => currentHp;
    public float AttackPower => attackPower;
    public float MoveSpeed => moveSpeed;
    public float AttackRange => attackRange;
    public float DetectionRange => detectionRange;

    public bool IsDead => isDead;
    public Animator MonsterAnimator => animator;

    protected virtual void Awake(){
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        rb=GetComponent<Rigidbody>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        if(navAgent !=null){
            navAgent.speed = moveSpeed;
            navAgent.stoppingDistance = attackRange-0.5f;
        }
    }
    protected virtual void Start(){
        currentHp = maxHp;
        isDead = false;
    }
    public virtual void Initialize(){
        //初始化血量、Ai等
        currentHp = maxHp;
        isDead = false;
        navAgent.enabled = true;
        animator.Rebind();
        gameObject.SetActive(true);
    }
    public virtual void ResetState(){
        //重置血量、状态等
        currentHp = maxHp;
        isDead = false;
        navAgent.enabled = false;
        animator.Rebind();
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
    public virtual void TakeDamage(float damage)
    {
        if(isDead) return;

        currentHp -= damage;

        OnTakeDamage?.Invoke();

        animator.SetTrigger("GetHit");

        if(currentHp <= 0)
        {
            Die();
        }
    }
    protected virtual void Die()
    {
        isDead = true;
        OnDeath?.Invoke();
        animator.SetTrigger("Die");
        navAgent.enabled = false;
        
        // 通知管理器回收
        MonsterManager._instance.ReturnMonster(this);
    }
    
    // 供子类实现的抽象方法
    public abstract void PerformAttack();   //执行攻击
    public abstract void SpecialAbility();  //特殊能力
}
