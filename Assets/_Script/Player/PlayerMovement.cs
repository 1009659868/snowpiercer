using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody body;
    [SerializeField] private GroundChecker groundChecker;
    [SerializeField] private PlayerKeyBinding binding;
    [SerializeField] private Dashing dashing;
    [SerializeField] private float speed;
    [SerializeField] private bool clampDiagonalSpeed;
    // 跳跃相关字段
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 8f;     // 跳跃力度
    [SerializeField] private float jumpCooldown = 0.2f; // 跳跃冷却时间
    [SerializeField] private float airControlMultiplier = 0.8f; // 空中移动控制系数
    [Header("Jump Buffer")]
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float coyoteTime = 0.1f;      // 新增土狼时间
    [Header("Jump Height")]
    [SerializeField] private float jumpCutMultiplier = 0.5f; // 跳跃衰减系数
    [SerializeField] private float minJumpHeight = 1f;      // 最低跳跃高度

    [SerializeField]private bool isJumping;
    private float lastJumpPressTime;
    private float lastJumpTime;
    
    private bool isUp => PlayerKeyBinding.isPressed(binding.moveUpKeys);
    private bool isDown => PlayerKeyBinding.isPressed(binding.moveDownKeys);
    private bool isRight => PlayerKeyBinding.isPressed(binding.moveRightKeys);
    private bool isLeft => PlayerKeyBinding.isPressed(binding.moveLeftKeys);
    private bool isJump => PlayerKeyBinding.isPressed(binding.JumpKeys);
    private bool isDash => PlayerKeyBinding.isPressed(binding.dashKeys);
    private bool isDashing { get => dashing.isDashing; set => dashing.isDashing = value; }
    public Vector3 velocity { get => body.velocity; set => body.velocity = value; }
    // public Direction? blockedDirection => movementChecker.blockedDirection;


    private void Update()
    {
        HandleDashing();
        HandleJump();
        HandleMovement();
        HandleRotation();
    }
    private void FixedUpdate()
    {
        HandleJumpPhysics();
    }
    private void HandleJump(){
        
        if(groundChecker.isGrounded){
            isJumping=false;
        }
        // 记录按键时间
        if (isJump)
        {
            lastJumpPressTime = Time.time;
        }

        // 允许缓冲时间内触发跳跃
        bool canCoyoteJump = Time.time - groundChecker.lastGroundedTime < coyoteTime;
        if ((groundChecker.isGrounded || canCoyoteJump) && !isJumping)
        {
            bool hasBufferedInput = Time.time - lastJumpPressTime <= jumpBufferTime;
            if (CanJump()&&hasBufferedInput)
            {
                ExecuteJump();
                lastJumpPressTime = -1; // 重置
            }
        }else if(isJumping){
            return;
        }
        
    }
    private void HandleJumpPhysics()
    {
        // 短按跳跃时衰减高度
        if (!groundChecker.isGrounded && 
            body.velocity.y > minJumpHeight &&
            !PlayerKeyBinding.isPressed(binding.JumpKeys))
        {
            body.velocity += Vector3.up * Physics.gravity.y * 
                           jumpCutMultiplier * Time.fixedDeltaTime;
        }

        // 强制最低下落速度
        if (body.velocity.y < -20f)
        {
            body.velocity = new Vector3(
                body.velocity.x, 
                -20f, 
                body.velocity.z
            );
        }
    }

    private void HandleMovement()
    {
        if (isDashing) return;

        // Vector3 moveInput = Vector3.zero;
        // 不再重置速度！改为累加速度
        Vector3 moveInput = Vector3.zero;

        if (isUp) moveInput.z = 1;
        else if (isDown) moveInput.z = -1;
        if (isRight) moveInput.x = 1;
        else if (isLeft) moveInput.x = -1;

        // 计算实际移动方向
        Vector3 moveDirection = moveInput.normalized;
        if (clampDiagonalSpeed) moveDirection = moveDirection.normalized;

        // 应用速度（保留垂直分量）
        float currentSpeed = groundChecker.isGrounded ? speed : speed * airControlMultiplier;
        velocity = new Vector3(
            moveDirection.x * currentSpeed,
            body.velocity.y, // 保持原有的Y轴速度（跳跃/重力）
            moveDirection.z * currentSpeed
        );
    }

    private void HandleDashing()
    {
        if (isDashing)
        {
            if ((Time.time - dashing.lastDashTime) >= dashing.duration)
            {
                isDashing = false;
                return;
            }
        }

        if (isDash)
        {
            if (isDashing) return;

            if ((Time.time - dashing.lastDashTime) < dashing.cooldown) return;

            isDashing = true;
            dashing.lastDashTime = Time.time;
            velocity = this.transform.forward * dashing.speed;
        }
    }

    private void HandleRotation()
    {
        if (isDashing) return;

        if (isUp && isRight)
        {
            this.transform.rotation = Quaternion.Euler(0, 45, 0);
        }
        else if (isUp && isLeft)
        {
            this.transform.rotation = Quaternion.Euler(0, -45, 0);
        }
        else if (isDown && isRight)
        {
            this.transform.rotation = Quaternion.Euler(0, 135, 0);
        }
        else if (isDown && isLeft)
        {
            this.transform.rotation = Quaternion.Euler(0, -135, 0);
        }
        else if (isUp)
        {
            this.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else if (isDown)
        {
            this.transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        else if (isRight)
        {
            this.transform.rotation = Quaternion.Euler(0, 90, 0);
        }
        else if (isLeft)
        {
            this.transform.rotation = Quaternion.Euler(0, -90, 0);
        }
    }
    private bool CanJump()
    {
        bool canCoyoteJump = Time.time - groundChecker.lastGroundedTime < coyoteTime;
        return (groundChecker.isGrounded||canCoyoteJump) && 
               !isJumping &&
               Time.time - lastJumpTime > jumpCooldown;
    }

    private void ExecuteJump()
    {
        isJumping = true;
        lastJumpTime = Time.time;
        // Debug.Log("jump");
        // 应用跳跃速度
        Vector3 currentVelocity= velocity;
        currentVelocity.y = 0;
        body.AddForce(Vector3.up*jumpForce,ForceMode.VelocityChange);
    }
}

public enum Direction { UP, DOWN, RIGHT, LEFT }

[System.Serializable]
public struct Dashing
{
    [HideInInspector] public float lastDashTime;
    [HideInInspector] public bool isDashing;
    public float cooldown;
    public float speed;
    public float duration;
}
