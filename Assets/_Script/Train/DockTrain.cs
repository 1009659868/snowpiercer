using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SlotStatus
{
    Empty,      // 空闲（未被占用）
    Occupied,   // 已占用（放置了防御塔）
    Locked,     // 锁定（不可操作）
    Cooldown    // 冷却中（暂时无法使用）
}
public class DockTrain : Car
{
    [Header("放置槽位")]
    [SerializeField] private Transform[] slots; // 每个槽位的Transform
    [SerializeField] private SlotStatus[] slotStatus; // 每个槽位的状态
    [Header("防御塔对象及需旋转的枪头")]
    public GameObject towerPrefab; // 防御塔的预制体
    public Tower[] towers; // 每个槽位上的防御塔
    void Awake()
    {
        // 初始化槽位状态
        slotStatus = new SlotStatus[slots.Length];
        for (int i = 0; i < slotStatus.Length; i++)
        {
            slotStatus[i] = SlotStatus.Empty;
        }
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
    }
    // 放置防御塔到槽位
    public void PlaceTower(int slotIndex)
    {
        
    }
}
