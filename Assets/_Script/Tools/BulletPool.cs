using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool _instance;

    [System.Serializable]
    public class PoolConfig
    {
        public BulletType type;
        public Bullet prefab;
        public int initialSize = 10;
    }

    [SerializeField] private List<PoolConfig> poolConfigs;

    private Dictionary<BulletType, Queue<Bullet>> pools;
    private Dictionary<BulletType, Bullet> prefabMap;

    private void Awake()
    {
        _instance = this;
        InitializePools();
    }
    void Update() 
    {
        foreach(var pool in pools.Values)
        {
            foreach(var bullet in pool)
            {
                if(bullet.gameObject.activeSelf && 
                   Time.time - bullet.GetActivateTime() > bullet.GetLifeTime())
                {
                    ReturnToPool(bullet);
                }
            }
        }
    }
    private void InitializePools()
    {
        pools = new Dictionary<BulletType, Queue<Bullet>>();
        prefabMap = new Dictionary<BulletType, Bullet>();

        foreach (var config in poolConfigs)
        {
            var queue = new Queue<Bullet>();
            prefabMap[config.type] = config.prefab;

            for (int i = 0; i < config.initialSize; i++)
            {
                Bullet bullet = CreateNewBullet(config.type);
                queue.Enqueue(bullet);
            }
            pools[config.type] = queue;
        }
    }

    public Bullet GetBullet(BulletType type, Vector3 position, Quaternion rotation, Transform target = null)
    {
        if(pools.TryGetValue(type, out Queue<Bullet> bullets)){
            if (bullets.Count == 0)
            {
                ExpandPool(type);
            }
        }
        

        Bullet bullet = pools[type].Dequeue();
        bullet.Activate(position, rotation, target);
        return bullet;
    }

    public void ReturnToPool(Bullet bullet)
    {
        bullet.Deactivate();
        pools[bullet.GetBulletType()].Enqueue(bullet);
        
    }

    private Bullet CreateNewBullet(BulletType type)
    {
        Bullet newBullet = Instantiate(prefabMap[type], transform);
        newBullet.Deactivate();
        return newBullet;
    }

    private void ExpandPool(BulletType type)
    {
        Bullet newBullet = CreateNewBullet(type);
        pools[type].Enqueue(newBullet);
    }
}

// 子弹类型枚举
public enum BulletType
{
    Basic,
    Laser,
    Bounce,
    Homing
}