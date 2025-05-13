using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GroundChecker : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private Transform checkPoint;      // 地面检测点
    [SerializeField] private float checkRadius = 0.2f;  // 检测半径
    [SerializeField] private LayerMask groundLayer;    // 地面层级
    [SerializeField] private float checkInterval = 0.1f; // 检测间隔（秒）

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;    // 是否显示检测范围
    [SerializeField] private Color gizmoColor = Color.red;

    // 公开属性
    public bool isGrounded { get; private set; }        // 是否在地面
    public float lastGroundedTime { get; private set; } // 最近一次接地时间
    public bool isJustGrounded { get; private set; }    // 本帧刚接地
    public bool isJustLeftGround { get; private set; }  // 本帧刚离开地面

    // 地面状态变化事件
    public event System.Action OnGrounded;   // 接地时触发
    public event System.Action OnLeftGround; // 离开地面时触发

    private bool previousGroundedState;
    private Coroutine checkCoroutine;

    void Start()
    {
        if (checkPoint == null)
        {
            Debug.LogError("GroundChecker: Missing check point reference!");
            enabled = false;
            return;
        }

        StartDetection();
    }

    // 开始检测
    public void StartDetection()
    {
        if (checkCoroutine == null)
        {
            checkCoroutine = StartCoroutine(GroundCheckRoutine());
        }
    }

    // 停止检测
    public void StopDetection()
    {
        if (checkCoroutine != null)
        {
            StopCoroutine(checkCoroutine);
            checkCoroutine = null;
        }
    }

    // 地面检测协程
    private IEnumerator GroundCheckRoutine()
    {
        while (true)
        {
            UpdateGroundState();
            yield return new WaitForSeconds(checkInterval);
        }
    }

    // 更新地面状态
    private void UpdateGroundState()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(checkPoint.position, checkRadius, groundLayer);

        // 记录最近接地时间
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            // Debug.Log("on Ground");
        }

        // 检测状态变化
        isJustGrounded = !wasGrounded && isGrounded;
        isJustLeftGround = wasGrounded && !isGrounded;

        // 触发事件
        if (isJustGrounded) OnGrounded?.Invoke();
        if (isJustLeftGround) OnLeftGround?.Invoke();
    }

    // 可视化检测范围
    private void OnDrawGizmos()
    {
        if (showGizmos && checkPoint != null)
        {
            if(isGrounded){
                Gizmos.color = Color.green;
            }
            else{
                Gizmos.color = Color.red;
            }
            Gizmos.DrawWireSphere(checkPoint.position, checkRadius);
        }
    }

    // 重置检测状态
    public void ResetState()
    {
        isGrounded = false;
        lastGroundedTime = 0f;
        isJustGrounded = false;
        isJustLeftGround = false;
    }
}
