using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// 防御塔类，每个防御塔独立处理目标锁定和旋转
public class Tower :MonoBehaviour
{
    public Transform partRotate; // 旋转部分
    public float range = 10f; // 索敌范围
    public string enemyTag = "Enemy"; // 敌人标签
    public float rotSpeed=10f; // 旋转速度
    public Transform target; // 当前锁定的目标

    void Start(){
        InvokeRepeating("UpdateTarget",0,0.5f);
    }
    void Update(){
        LockTarget();
    }
    // 更新目标
    public void UpdateTarget()
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

