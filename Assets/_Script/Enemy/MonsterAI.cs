using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class MonsterAI : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] protected float updateInterval = 0.5f;
    [SerializeField] protected float attackCooldown = 2f;

    protected Monster monster;
    protected NavMeshAgent navAgent;
    protected Transform player;
    protected float lastAttackTime;
    protected bool isActive = true;
    protected virtual void Awake()
    {
        monster = GetComponent<Monster>();
        navAgent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }
    protected virtual void Start()
    {
        StartCoroutine(AIUpdate());
    }

    protected virtual IEnumerator AIUpdate()
    {
        while(isActive)
        {
            UpdateAIState();
            yield return new WaitForSeconds(updateInterval);
        }
    }
    protected abstract void UpdateAIState();
    protected virtual void ChasePlayer()
    {
        if(navAgent.isActiveAndEnabled)
        {
            navAgent.SetDestination(player.position);
            navAgent.isStopped = false;
        }
    }
    protected virtual void StopMovement()
    {
        if(navAgent.isActiveAndEnabled)
        {
            navAgent.isStopped = true;
        }
    }
    protected virtual bool IsPlayerInRange(float range)
    {
        return Vector3.Distance(transform.position, player.position) <= range;
    }

    public virtual void DisableAI()
    {
        isActive = false;
        StopMovement();
    }
}
