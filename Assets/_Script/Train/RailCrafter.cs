using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RailCrafter : Car
{
    [Header("Crafting Settings")]
    public int woodRequired = 3;         // 制作所需木材
    public float craftTime = 5f;         // 制作耗时
    public float throwForce = 8f;        // 轨道抛出力度
    public float spawnCheckRadius = 1f;  // 生成检测半径
    
    [Header("References")]
    public Transform spawnPoint;         // 轨道生成点
    public GameObject railPrefab;       // 轨道预制体
    public ParticleSystem craftingEffect; // 制作特效

    private int currentWood;
    private bool isCrafting;


    void TryStartCrafting()
    {
        if (isCrafting || !CanAcceptWood()) return;
        
        StartCoroutine(CraftProcess());
    }

    IEnumerator CraftProcess()
    {
        // 收取木材
        // 收集方式???
        // 吸附还是放置???
        int woodToTake = Mathf.Min(0, woodRequired - currentWood);
        
        currentWood += woodToTake;

        // 开始制作
        isCrafting = true;
        craftingEffect.Play();
        
        float timer = 0;
        while (timer < craftTime && !isExploded)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // 生成轨道
        if (!isExploded) SpawnRail();
        
        craftingEffect.Stop();
        currentWood = 0;
        isCrafting = false;
    }

    void SpawnRail()
    {
        
    }

    Vector3 FindValidSpawnPosition(Vector3 baseDirection)
    {
        return Vector3.zero;
    }

    bool CanAcceptWood()
    {
        return false;
    }

    public override void Explode()
    {
        base.Explode(); // 调用父类爆炸逻辑
        StopAllCoroutines(); // 中断制作流程
    }

}
