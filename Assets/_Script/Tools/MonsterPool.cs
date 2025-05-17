using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MonsterPool :Singleton<MonsterPool>
{
    public static MonsterPool _instance;

    [System.Serializable]
    public class PoolConfig{
        public MonsterType type;
        public GameObject prefab;
        public int initialSize;
    }

    [Header("Pool Settings")]
    [SerializeField] private List<PoolConfig> poolConfigs;
    private Dictionary<MonsterType,Queue<GameObject>> pools;
    private Dictionary<MonsterType,GameObject> prefabMap;

    protected override void Awake()
    {
        base.Awake();
        _instance = this;
        StartCoroutine(DelayedInitialize());
    }
     private IEnumerator DelayedInitialize(){
        // 等待直到NavMesh烘焙完成
        yield return new WaitUntil(() => MapManager._instance.isBaked);
        InitializePools();
     }
    private void InitializePools(){
        pools=new Dictionary<MonsterType, Queue<GameObject>>();
        prefabMap=new Dictionary<MonsterType,GameObject>();
        foreach(var config in poolConfigs){
            var queue= new Queue<GameObject>();
            config.prefab.GetComponent<NavMeshAgent>().enabled=false;
            prefabMap[config.type] = config.prefab;
            for(int i=0;i<config.initialSize;i++){
                GameObject obj = CreateNewObj(config.type);
                queue.Enqueue(obj); 
            }
            pools[config.type] = queue;
        }
    }
    public GameObject Get(MonsterType type,Vector3 position){
        // 确保池中存在该类型
        if (!pools.ContainsKey(type))
        {
            Debug.LogError($"No pool config found for {type}");
            return null;
        }

        // 自动扩展逻辑
        if (pools[type].Count == 0)
        {
            ExpandPool(type, 2);
        }
        GameObject obj = pools[type].Dequeue();
        PrepareObj(obj,position);
        return obj;
    }
    public void Return(GameObject obj){
        MonsterType type = obj.GetComponent<Monster>().type;
        if (!pools.ContainsKey(type))
        {
            Debug.LogWarning($"Trying to return unmanaged type: {type}");
            return;
        }

        ResetObj(obj);
        pools[type].Enqueue(obj);
    }
    public GameObject CreateNewObj(MonsterType type){
        if (!prefabMap.ContainsKey(type))
        {
            Debug.LogError($"No prefab found for {type}");
            return null;
        }

        GameObject newObj = Instantiate(prefabMap[type], this.transform);
        newObj.SetActive(false);
        newObj.GetComponent<Monster>().type = type; // 确保Monster脚本有type字段
        return newObj;
    }
    private void PrepareObj(GameObject obj,Vector3 position){
        obj.transform.position = position;
        obj.SetActive(true);

        //初始化怪物状态
        Monster monster= obj.GetComponent<Monster>();
        monster?.Initialize();
    }
    private void ResetObj(GameObject obj){
        obj.transform.SetParent(this.transform);
        obj.SetActive(false);

        // 重置怪物状态
        Monster monster = obj.GetComponent<Monster>();
        monster?.ResetState();
        
        // 重置位置和旋转
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
    }
    private void ExpandPool(MonsterType type, int expandAmount){
        for(int i=0;i<expandAmount;i++){
            GameObject obj=CreateNewObj(type);
            pools[type].Enqueue(obj);
        }
    }


}
public enum MonsterType {
    Slime,
    Turtle
}
