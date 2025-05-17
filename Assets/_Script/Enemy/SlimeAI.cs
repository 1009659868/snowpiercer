using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class SlimeAI : MonsterAI
{
    [Header("Slime AI Settings")]
    [SerializeField] private float jumpAttackCooldown = 5f;
    [SerializeField] private float splitHealthThreshold = 0.3f;
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float runThreshold = 3f; // 切换奔跑的速度阈值

    private float lastJumpTime;
    private Slime slime;

    protected override void Awake()
    {
        base.Awake();
        slime = GetComponent<Slime>();
    }
    protected override void UpdateAIState()
    {
        if(monster.IsDead) return;
        // 计算移动方向
        Vector3 moveDirection = navAgent.velocity.normalized;
        monster.MonsterAnimator.SetFloat(SlimeAnimParams.MoveX, moveDirection.x);
        monster.MonsterAnimator.SetFloat(SlimeAnimParams.MoveZ, moveDirection.z);
        // 设置移动状态
        float currentSpeed = navAgent.velocity.magnitude;
        monster.MonsterAnimator.SetFloat(SlimeAnimParams.Speed, currentSpeed);
        monster.MonsterAnimator.SetBool(SlimeAnimParams.IsRunning, currentSpeed > runThreshold);
        
        // 设置战斗状态
        bool isInCombat = IsPlayerInRange(monster.DetectionRange);
        monster.MonsterAnimator.SetBool(SlimeAnimParams.IsInCombat, isInCombat);
        //更新Ai行为
        if(isInCombat){
            // HandleCombatState();
            HandleAttackState();
            monster.MonsterAnimator.SetTrigger(SlimeAnimParams.IdleBattleTrigger);
        }
        else{
            PatrolBehavior();
            monster.MonsterAnimator.SetTrigger(SlimeAnimParams.IdleNormalTrigger);
        }

        // 控制移动速度
        navAgent.speed = currentSpeed > runThreshold ? runSpeed : walkSpeed;
    }
    private void HandleAttackState()
    {
        if(Time.time - lastAttackTime >= attackCooldown)
        {
            transform.LookAt(player.position);
            monster.PerformAttack();
            lastAttackTime = Time.time;
        }
        
        // 跳跃攻击冷却
        if(Time.time - lastJumpTime >= jumpAttackCooldown)
        {
            slime.PerformAttack();
            lastJumpTime = Time.time;
        }
    }
    private void CheckSpecialAbility()
    {
        if(slime.CurrentHp <= slime.MaxHp * splitHealthThreshold)
        {
            slime.SpecialAbility();
        }
    }
    private void PatrolBehavior()
    {
        if(!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * 10f;
            NavMeshHit hit;
            if(NavMesh.SamplePosition(randomPoint, out hit, 10f, NavMesh.AllAreas))
            {
                navAgent.SetDestination(hit.position);
            }
        }
    }
    protected override void ChasePlayer()
    {
        if(navAgent.isActiveAndEnabled)
        {
            navAgent.SetDestination(player.position);
            navAgent.speed = monster.MoveSpeed * 1.5f; // 追击时加速
        }
    }
}
