using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MachineGun : Car
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

    // 移除指定槽位上的防御塔
    public void RemoveTower(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
        {
            Debug.LogError("无效的槽位索引");
            return;
        }

        if (slotStatus[slotIndex] == SlotStatus.Occupied)
        {
            // 销毁槽位上的防御塔
            Transform tower = slots[slotIndex].GetChild(0);
            if (tower != null)
            {
                Destroy(tower.gameObject);
            }
            slotStatus[slotIndex] = SlotStatus.Empty;
            towers[slotIndex] = null;
        }
        else
        {
            Debug.Log("槽位为空或无法移除");
        }
    }
}
public enum SlotStatus
{
    Empty,      // 空闲（未被占用）
    Occupied,   // 已占用（放置了防御塔）
    Locked,     // 锁定（不可操作）
    Cooldown    // 冷却中（暂时无法使用）
}