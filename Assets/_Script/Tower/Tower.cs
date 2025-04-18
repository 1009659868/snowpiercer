using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

//防御塔的基类,后续防御塔会根据这个类完成多样的防御塔
// 防御塔类，每个防御塔独立处理目标锁定和旋转
public abstract class Tower :MonoBehaviour
{
    public Transform partRotate; // 旋转部分
    public float range = 10f; // 索敌范围
    public string enemyTag = "Enemy"; // 敌人标签
    public float rotSpeed=10f; // 旋转速度
    

    [Header("Bullet Settings")]
    public GameObject bulletPrefab;//子弹的预制体
    public Transform bulletPoint;//子弹生成的位置
    public float bulletRate=2f; //发射子弹的速率
    [SerializeField] protected BulletType bulletType = BulletType.Basic;
    [SerializeField] protected float bulletSpeed = 20f;
    [SerializeField] protected int bulletDamage = 1;
    protected Transform target; // 当前锁定的目标
    protected float fireCountdown=0f;
    
    protected virtual void Start(){
        InvokeRepeating("UpdateTarget",0,0.5f);
        fireCountdown=1/bulletRate;
    }
    protected virtual void Update(){
        if(target==null) return;
        LockTarget();
        // //倒计时发射子弹
        // fireCountdown-=Time.deltaTime;
        // if(fireCountdown<=0){
        //     //发射子弹
        //     Shoot();
        //     fireCountdown=1/bulletRate;
        // }

    }
    // 射击方法，子类可重写射击逻辑
    protected virtual void Shoot(){
        var bullet= BulletPool.Instance.GetBullet(
            bulletType,
            bulletPoint.position,
            bulletPoint.rotation,
            target
        );
        bullet.SetDamage(bulletDamage);
        bullet.SetSpeed(bulletSpeed);

    }
    // 更新目标
    protected void UpdateTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        if (enemies.Length == 0)
        {
            target = null;
            return;
        }

        // 按距离排序敌人
        System.Array.Sort(enemies, (a, b) =>
            Vector3.Distance(a.transform.position, partRotate.position)
                .CompareTo(Vector3.Distance(b.transform.position, partRotate.position)));

        float minDistance = Vector3.Distance(enemies[0].transform.position, partRotate.position);
        // 找到最近的敌人
        if (minDistance < range)
        {
            target = enemies[0].transform;
        }
        else
        {
            target = null;
        }
    }

    // 锁定目标
    public void LockTarget()
    {
        if (target == null || partRotate == null) return;

        Vector3 dir = target.position - partRotate.position;
        Quaternion targetRotation = Quaternion.LookRotation(dir);
        Quaternion lerpRot = Quaternion.Lerp(partRotate.rotation, targetRotation, Time.deltaTime * rotSpeed);
        partRotate.rotation = Quaternion.Euler(new Vector3(0, lerpRot.eulerAngles.y, 0));
    }
    // 在Scene视图中绘制索敌范围（仅在选中物体时显示）
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red; // 设置Gizmos颜色
        Gizmos.DrawWireSphere(transform.position, range); // 绘制圆形线框
    }
}

