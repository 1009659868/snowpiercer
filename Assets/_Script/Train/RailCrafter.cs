using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RailCrafter : Car
{
    [Header("Crafting Settings")]
    public List<Recipe> availableRecipes = new List<Recipe>();
    public Transform spawnPoint;         // 轨道生成点
    public ParticleSystem craftingEffect; // 制作特效
    [Header("References")]
    
    public LayerMask playerLayer;
    private PlayerStack player;

    private bool isUIOpen;
    private bool isCrafting;

    public bool CanCraft(Recipe recipe)
    {
        foreach(var req in recipe.requirements)
        {
            if(GetAvailableAmount(req.type) < req.amount)
                return false;
        }
        return true;
    }
    public void TryCraft(Recipe recipe)
    {
        if(!CanCraft(recipe)) return;

        // 扣除材料
        foreach(var req in recipe.requirements)
        {
            ConsumeMaterials(req.type, req.amount);
        }

        // 生成物品
        Instantiate(recipe.resultPrefab, GetSpawnPosition(), Quaternion.identity);
    }
    int GetAvailableAmount(StackableType type)
    {
        int total = 0;
        
        // 玩家携带的材料
        total += player.GetItemCount(type);
        
        // 所有存储车厢中的材料
        foreach(var car in TrainManager._instance.GetAllCars<RailSupply>())
        {
            total += car.GetStoredCount(type);
        }
        
        return total;
    }
    void ConsumeMaterials(StackableType type, int amount)
    {
        // 先扣除玩家携带的
        int fromPlayer = Mathf.Min(amount, player.GetItemCount(type));
        player.ConsumeItems(type, fromPlayer);
        amount -= fromPlayer;

        // 再从存储车厢扣除
        if(amount > 0)
        {
            foreach(var car in TrainManager._instance.GetAllCars<RailSupply>())
            {
                int fromCar = Mathf.Min(amount, car.GetStoredCount(type));
                car.ConsumeItems(type, fromCar);
                amount -= fromCar;
                if(amount <= 0) break;
            }
        }
    }
    Vector3 GetSpawnPosition()
    {
        Vector3 spawnPos = transform.position + transform.forward * 3f;
        if(Physics.CheckSphere(spawnPos, 1f))
            spawnPos += transform.up * 2f;
        return spawnPos;
    }
    public override void Explode()
    {
        base.Explode(); // 调用父类爆炸逻辑
        StopAllCoroutines(); // 中断制作流程
    }

}
