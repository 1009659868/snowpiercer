using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicBullet : Bullet
{
    [Header("Basic Bullet Settings")]
    [SerializeField] private ParticleSystem impactEffect;
    public Vector3 _initialDirection;
    protected override void Update()
    {
        base.Update();
        if (!isActive) return;
        HandleMovement();
        HandleRotation();
        
    }
    public override void Activate(Vector3 position, Quaternion rotation, Transform target = null)
    {
        _initialDirection = rotation*Vector3.forward;
        _initialDirection.y = 0;  // 新增垂直分量清零
        _initialDirection.Normalize();
        base.Activate(position, Quaternion.LookRotation(_initialDirection), target);
    }
    private void HandleMovement()
    {
        if (rb == null) // 非物理模式移动
        {
            transform.position += _initialDirection * speed * Time.deltaTime;
        }
    }
    private void HandleRotation()
    {
        if ( target != null)
        {
            // 仅视觉旋转不影响实际运动方向
            Vector3 dir = target.position - transform.position;
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Lerp(
                transform.rotation, 
                targetRot,
                Time.deltaTime
            );
        }
    }

}
