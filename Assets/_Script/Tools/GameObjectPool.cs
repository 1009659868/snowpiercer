using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectPool 
{
    private Dictionary<blockType, Stack<GameObject>> pool = new Dictionary<blockType, Stack<GameObject>>();
    private Transform poolParent;
    
    public GameObjectPool(Transform parent)
    {
        poolParent = new GameObject("ObjectPool").transform;
        poolParent.SetParent(parent);
        poolParent.gameObject.SetActive(false);
    }
    public GameObject Get(blockType type, GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject obj;
        
        if (!pool.ContainsKey(type) || pool[type].Count == 0)
        {
            obj = Object.Instantiate(prefab, position, rotation, parent);
        }
        else
        {
            obj = pool[type].Pop();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.transform.SetParent(parent);
            obj.SetActive(true);
        }
        return obj;
    }
    public void Return(blockType type, GameObject obj)
    {
        if (!pool.ContainsKey(type))
        {
            pool[type] = new Stack<GameObject>();
        }
        
        obj.SetActive(false);
        obj.transform.SetParent(poolParent);
        pool[type].Push(obj);
    }
    public void Prewarm(blockType type, GameObject prefab, int count)
    {
        // 跳过空气方块的预生成
        if (type == blockType.Air || prefab == null)
        {
            return;
        }
        if (!pool.ContainsKey(type))
        {
            pool[type] = new Stack<GameObject>(count);
        }
        
        for (int i = 0; i < count; i++)
        {
            GameObject obj = Object.Instantiate(prefab);
            if (obj == null) continue;
            Return(type, obj);
        }
    }
    public void Clear()
    {
        foreach (var stack in pool.Values)
        {
            while (stack.Count > 0)
            {
                Object.Destroy(stack.Pop());
            }
        }
        pool.Clear();
        
        if (poolParent != null)
        {
            Object.Destroy(poolParent.gameObject);
        }
    }
}
