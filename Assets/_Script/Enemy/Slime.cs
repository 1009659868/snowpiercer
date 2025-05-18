using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slime : Monster
{
    [Header("Slime Settings")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float splashDamage = 20f;
    [SerializeField] private GameObject miniSlimePrefab;
    [SerializeField] private int splitCount = 2;
    private bool isJumping;
    protected override void Awake()
    {
        base.Awake();
        type = MonsterType.Slime;
    }
    public override void PerformAttack()
    {
        // 随机选择攻击动画类型
        int attackType = Random.Range(0, 2);
        animator.SetInteger(SlimeAnimParams.AttackType, attackType);
        animator.SetTrigger(SlimeAnimParams.AttackTrigger);

        // 实际攻击逻辑通过动画事件触发
    }
    // 分裂技能
    public override void SpecialAbility()
    {
       if(currentHealth <= maxHP * 0.3f)
        {
            SplitIntoMiniSlimes();
        }
    }
    private void SplitIntoMiniSlimes()
    {
        for(int i = 0; i < splitCount; i++)
        {
            
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * 2f;
            Monster miniSlime = MonsterPool._instance.Get(MonsterType.Slime, spawnPos).GetComponent<Monster>();
            miniSlime.Initialize();
            miniSlime.transform.localScale *= 0.6f;
        }
    }
    // 动画事件调用的攻击方法
    public void OnJumpAttack(){
        if(isDead) return;

        Vector3 jumpDirection = (player.position - transform.position).normalized;
        rb.AddForce(jumpDirection * jumpForce + Vector3.up * 2f, ForceMode.Impulse);
        isJumping = true;
    }
    protected override void Die()
    {
        base.Die();
        animator.SetTrigger(SlimeAnimParams.DieTrigger);
    }

    public override void TakeDamage(float damage,Vector3 hitDirection)
    {
        base.TakeDamage(damage,hitDirection);
        animator.SetTrigger(SlimeAnimParams.HitTrigger);
    }

}
// Animator参数对照表
public class SlimeAnimParams
{
    // 基础参数
    public const string Speed = "Speed";
    public const string MoveX = "MoveX";
    public const string MoveZ = "MoveZ";
    public const string IsRunning = "IsRunning";
    public const string IsInCombat = "IsInCombat";
    // 触发器
    public const string AttackTrigger = "Attack";
    public const string AttackType = "AttackType";
    public const string DieTrigger = "Die";
    public const string HitTrigger = "GetHit";
    public const string IdleNormalTrigger = "ToIdleNormal";
    public const string IdleBattleTrigger = "ToIdleBattle";
}