using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MGTower : Tower
{
    [Header("MG Settings")]
    [SerializeField] private int burstCount = 10;    // 连发次数
    [SerializeField] private float burstFireRate = 10f; // 连发内射击间隔
    [SerializeField] private float cooldownBetweenBursts = 10f; // 连发组冷却时间
    [SerializeField] private float spreadAngle = 30f; // 子弹散布角度
    private bool isBursting = false;
    private float lastBurstTime;
    private int currentBurst = 0;
    protected override void Update()
    {
        base.Update();
        // 冷却检测逻辑
        if (!isBursting && 
            target != null && 
            Time.time - lastBurstTime >= cooldownBetweenBursts)
        {
            StartCoroutine(BurstFire());
        }
    }
    private IEnumerator BurstFire()
    {
        isBursting = true;
        currentBurst = 0;
        while (currentBurst < burstCount)
        {
            if(target == null) break; // 目标丢失中断
            Shoot();
            currentBurst++;
            yield return new WaitForSeconds(burstFireRate);
        }
        lastBurstTime = Time.time;
        currentBurst = 0;
        isBursting = false;
    }

    protected override void Shoot()
    {
        var bullet = BulletPool.Instance.GetBullet(
            bulletType,
            bulletPoint.position,
            Quaternion.LookRotation(CalculateSpread()),
            target
        );
        
        // 配置子弹参数
        bullet.SetDamage(bulletDamage);
        bullet.SetSpeed(bulletSpeed);
        //bullet.AddBehavior(new TrailDecorator()); // 添加拖尾特效
    }
    //计算散射角度
    private Vector3 CalculateSpread(){
        float randomAngle = Random.Range(-spreadAngle/2, spreadAngle/2);
        Vector3 dir=target.position - bulletPoint.position;
        dir.y=0;

        return Quaternion.Euler(0, randomAngle, 0) * dir.normalized;
    }
}
