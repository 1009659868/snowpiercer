using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Railway : MonoBehaviour
{
    // 单例访问
    private static Railway _instance;
    public static Railway Instance => _instance;
    [SerializeField] private Transform startingRails;
    [SerializeField] private Transform railParent;
    [SerializeField] private Rail prefab;
    // 存储所有生成的铁路对象
    public List<Rail> rails { get; private set; } = new List<Rail>();
    private void Awake()
    {
        _instance = this;
    }
    private void Start()
    {
        SpawnRails();
    }
    // 添加轨道到系统
    public void AddRail(Rail rail)
    {
        if (!rails.Contains(rail))
        {
            rails.Add(rail);
        }
    }
    private void SpawnRails()
    {
        Stack<Rail> spawnedRails = new Stack<Rail>();
        foreach (Transform rail in startingRails)
        {
            // 根据预制体生成新的 Rail 对象，放置到 railParent 下
            var newRail = Instantiate(prefab, MyGrid._instance.GroundGridToWorld(MyGrid._instance.WorldToGroundGrid(rail.position)), Quaternion.identity, railParent);
            SetupRailConnection(spawnedRails, newRail);
            AddRail(newRail);
        }
    }
    private void SetupRailConnection(Stack<Rail> spawnedRails, Rail newRail)
    {
        if (spawnedRails.TryPeek(out Rail lastRail))
        {
            newRail.previous = lastRail;
            lastRail.next = newRail;
        }
        else
        {
            newRail.previous = newRail;
        }
        spawnedRails.Push(newRail);
    }
    
    public int GetRailIndex(Rail rail)
    {
        return rails.IndexOf(rail);
    }
}
