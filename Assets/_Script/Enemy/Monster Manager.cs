using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MonsterManager : MonoBehaviour
{
    public static MonsterManager _instance;
    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int maxActiveMonsters = 20;
    [SerializeField] private float spawnRadius = 15f;

    private List<Monster> activeMonsters = new List<Monster>();
    private Transform playerTransform;

    void Awake()
    {
        if(_instance == null){
            _instance=this;
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        }
        else{
            Destroy(gameObject);
        }
        
    }

    private void Start(){
        StartCoroutine(SpawnRoutine());
    }
    private IEnumerator SpawnRoutine()
    {
        yield return new WaitUntil(() => MapManager._instance.isBaked);
        while(true)
        {
            yield return new WaitForSeconds(spawnInterval);
            
            if(activeMonsters.Count < maxActiveMonsters)
            {
                SpawnRandomMonster();
            }
        }
    }
    /// <summary>
    /// 随机怪物生成
    /// </summary>
    /// 生成位置还需要通过噪声和玩家位置共同决定
    private void SpawnRandomMonster()
    {
        Vector3 randomPos = GetRandomSpawnPosition();
        MonsterType randomType = (MonsterType)Random.Range(0, System.Enum.GetValues(typeof(MonsterType)).Length);
        
        // Monster monster = MonsterPool.Instance.Get(randomType, randomPos).GetComponent<Monster>();
        Monster monster = MonsterPool._instance.Get(MonsterType.Slime, randomPos).GetComponent<Monster>();
        if(monster != null){
            monster.transform.position = randomPos;
            monster.transform.rotation = Quaternion.identity;
            
            if(monster.navAgent != null)
            {
                monster.navAgent.enabled = true;
                monster.navAgent.Warp(randomPos);
            }
            monster.Initialize();
            activeMonsters.Add(monster);
        }else{
            MonsterPool._instance.Return(monster.gameObject);
        }
        
    }
    private Vector3 GetRandomSpawnPosition()
    {
        Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 spawnPos = new Vector3(
            playerTransform.position.x + randomCircle.x*2,
            playerTransform.position.y+8f,
            playerTransform.position.z + randomCircle.y*2 
        );
        
        NavMeshHit hit;
        NavMesh.SamplePosition(spawnPos, out hit, spawnRadius, NavMesh.AllAreas);
        return hit.position;
    }
    public void ReturnMonster(Monster monster)
    {
        // 确保从激活列表移除（新增防御性编程）
        if(monster == null) return;
        // 使用while循环确保完全移除
        while(activeMonsters.Contains(monster))
        {
            activeMonsters.Remove(monster);
        }
        if(monster.gameObject.activeSelf)
        {
            Debug.LogWarning($"正在回收仍处于激活状态的怪物: {monster.name}");
            monster.gameObject.SetActive(false);
        }
        MonsterPool._instance.Return(monster.gameObject);
    }
    public List<Monster> GetActiveMonsters()
    {
        return new List<Monster>(activeMonsters);
    }
}
