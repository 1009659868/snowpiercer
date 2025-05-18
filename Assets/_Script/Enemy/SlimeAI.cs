using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
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
    public override void UpdateAIState()
    {
        if(monster.currentHealth<=0) {
            return;
        }
        if(monster.IsDead || !IsAgentValid()) return;

        monster.UpdateMovementAnimation();
        
        bool isInCombat = IsPlayerInRange(monster.DetectionRange);
        //更新Ai行为
        if(isInCombat){
            ChasePlayer();
            HandleAttackState();
            monster.MonsterAnimator.SetTrigger(SlimeAnimParams.IdleBattleTrigger);
        }
        else{
            PatrolBehavior();
            monster.MonsterAnimator.SetTrigger(SlimeAnimParams.IdleNormalTrigger);
        }
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
        if(slime.currentHealth <= slime.maxHP * splitHealthThreshold)
        {
            slime.SpecialAbility();
        }
    }
    private void PatrolBehavior()
    {
        if (!IsAgentValid()) return;
        float originalStoppingDistance = navAgent.stoppingDistance;
        navAgent.stoppingDistance = 0.5f;

        if(!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * 5f;
            NavMeshHit hit;
            if(NavMesh.SamplePosition(randomPoint, out hit, 10f, NavMesh.AllAreas))
            {
                navAgent.SetDestination(hit.position);
            }
        }
        navAgent.stoppingDistance = originalStoppingDistance;
    }
    protected override void ChasePlayer()
    {
        if(navAgent.isActiveAndEnabled)
        {
            navAgent.SetDestination(player.position);
            navAgent.speed = monster.MoveSpeed * 1.5f; // 追击时加速
        }
    }
    private void OnDrawGizmos()
    {
        if(navAgent != null && navAgent.hasPath)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, navAgent.destination);
        }
    }
}
