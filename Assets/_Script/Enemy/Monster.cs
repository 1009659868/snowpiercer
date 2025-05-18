using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public abstract class Monster : HealthManager
{
    [Header("Base Settings")]
    public MonsterType type;
    [SerializeField] protected float attackPower = 10;  //攻击力
    [SerializeField] protected float moveSpeed = 3f;    //移动速度
    [SerializeField] protected float attackRange = 1f;  //攻击范围
    [SerializeField] protected float detectionRange = 30f;//追击范围
    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float knockbackUpwardForce = 2f; // 添加垂直分量
    public NavMeshAgent navAgent;
    protected Animator animator;
    protected bool isDead;
    protected Transform player;
    protected Rigidbody rb;
    public float AttackPower => attackPower;
    public float MoveSpeed => moveSpeed;
    public float AttackRange => attackRange;
    public float DetectionRange => detectionRange;

    public bool IsDead => isDead;
    public Animator MonsterAnimator => animator;

    protected override void Awake(){
        base.Awake();
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        rb=GetComponent<Rigidbody>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        if(navAgent !=null){
            navAgent.speed = moveSpeed;
            navAgent.stoppingDistance = attackRange-0.5f;
        }
        // 初始化事件监听
        OnTakeDamage.AddListener(HandleTakeDamage);
        OnDeath.AddListener(HandleDeath);
    }
    protected override void Start(){
        
        currentHealth = maxHP;
        isDead = false;
    }
    public void UpdateMovementAnimation()
    {
        if(navAgent == null || !navAgent.enabled) return;

        // 获取速度向量在本地坐标系中的分量
        Vector3 localVelocity = transform.InverseTransformDirection(navAgent.velocity);
        float moveX = localVelocity.x;
        float moveZ = localVelocity.z;

        // 标准化数值并应用平滑
        float smoothTime = 0.1f;
        float currentX = animator.GetFloat("MoveX");
        float currentZ = animator.GetFloat("MoveZ");
        
        animator.SetFloat("MoveX", Mathf.Lerp(currentX, moveX, smoothTime));
        animator.SetFloat("MoveZ", Mathf.Lerp(currentZ, moveZ, smoothTime));
    }
    public virtual void Initialize(){
        gameObject.SetActive(true);
        //初始化血量、Ai等
        currentHealth = maxHP;
        isDead = false;
        // 重置刚体状态
        if(rb != null){
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
        }
        // 启用导航代理
        if(navAgent != null){
            navAgent.enabled = true;
            navAgent.speed = moveSpeed;
            navAgent.stoppingDistance = attackRange - 0.5f;
            navAgent.Warp(transform.position);
            navAgent.isStopped = false;
        }
        
        animator.Rebind();
        animator.Update(0f);
        
        StartCoroutine(GetComponent<MonsterAI>().AIUpdate());
    }
    public virtual void ResetState(){
        StopAllCoroutines();
        //重置血量、状态等
        currentHealth = maxHP;
        isDead = false;

        if(rb != null){
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
        }

        if(navAgent != null){
            navAgent.enabled = true;
            navAgent.ResetPath();
            navAgent.isStopped = true;
            navAgent.enabled = false;
        }
        
        animator.Rebind();
        animator.Update(0f);  // 强制立即更新动画状态机
        animator.ResetTrigger("Die");
        animator.ResetTrigger("GetHit");
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        
        CancelInvoke();
    }
    public override void TakeDamage(float damage,Vector3 hitDirection)
    {   
        base.TakeDamage(damage,hitDirection); 
        if(currentHealth <= 0 || isDead) return;
        ApplyKnockback(hitDirection);
        
    }
    private void ApplyKnockback(Vector3 direction)
    {
        if (navAgent != null )
        {
            navAgent.enabled = false;
            navAgent.ResetPath();
        }
        direction = direction.normalized;
        direction.y = knockbackUpwardForce; // 添加垂直方向击退
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.None;
        rb.AddForce(direction.normalized * knockbackForce, ForceMode.VelocityChange);
        StartCoroutine(ClampKnockbackVelocity());
        StartCoroutine(ResetNavAgent());
    }
    private IEnumerator ClampKnockbackVelocity()
    {
        float elapsed = 0f;
        while(elapsed < knockbackDuration)
        {
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, knockbackForce * 1.5f);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    private IEnumerator ResetNavAgent()
    {
        yield return new WaitForSeconds(knockbackDuration);

        if(this == null || !gameObject.activeInHierarchy) yield break;

        if(rb!=null){
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.isKinematic =true;
        }
        
        if (navAgent != null)
        {
            int retryCount =0;
            while(!navAgent.isOnNavMesh && retryCount < 3)
            {
                NavMeshHit hit;
                if(NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
                {
                    navAgent.Warp(hit.position);
                    retryCount++;
                    yield return null;
                }
                else
                {
                    break;
                }
            }
        }
        navAgent.enabled = true;
        navAgent.isStopped = false;
        navAgent.ResetPath();

        if(IsPlayerInRange(detectionRange)){
            navAgent.SetDestination(player.position);
        }else{
            // 如果不在战斗状态，触发巡逻
            GetComponent<SlimeAI>()?.UpdateAIState();
        }
    }
    protected virtual bool IsPlayerInRange(float range)
    {
        return Vector3.Distance(transform.position, player.position) <= range;
    }
    private void HandleTakeDamage(float damage)
    {
        animator.SetTrigger("GetHit");
    }
    private void HandleDeath()
    {
        if(isDead || !gameObject.activeSelf) return;
        isDead = true;
        // 立即停止所有行为
        StopAllCoroutines();
        if(navAgent != null){
            navAgent.isStopped = true;
            navAgent.ResetPath();
            navAgent.enabled = false;
        }
        // 重置动画触发器
        animator.ResetTrigger("GetHit");
        animator.SetTrigger("Die");
        Debug.Log("die");
        // StopAllCoroutines();
        StartCoroutine(DelayedReturn());
    }
    private IEnumerator DelayedReturn(){
        yield return new WaitForSeconds(2f); // 匹配死亡动画时间
        if(this == null || 
            gameObject == null || 
                !gameObject.activeInHierarchy) yield break;
        // 再次确认对象状态
        if(this != null && gameObject.activeSelf){
            MonsterManager._instance.ReturnMonster(this);
        }else{
            Debug.Log("?????");
        }
    }
    // 供子类实现的抽象方法

    public abstract void PerformAttack();   //执行攻击
    public abstract void SpecialAbility();  //特殊能力
}
