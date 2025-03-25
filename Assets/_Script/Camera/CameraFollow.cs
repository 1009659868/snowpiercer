using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("基础设置")]
    public Transform target;             // 跟随目标
    public float followSpeed = 5f;       // 跟随速度
    public float rotationSpeed = 2f;    // 旋转速度

    [Header("视角参数")]
    [Range(30, 80)] public float pitchAngle = 45f; // 俯视角（X轴旋转）
    public float baseHeight = 15f;       // 基础高度
    public float zoomSensitivity = 5f;  // 缩放灵敏度
    public float minZoom = 5f;           // 最小缩放距离
    public float maxZoom = 20f;          // 最大缩放距离

    [Header("边界限制")]
    public bool useBounds = true;        // 启用边界限制
    public Vector2 mapCenter;            // 地图中心点(XZ平面)
    public Vector2 mapSize = new Vector2(50, 50); // 地图尺寸(XZ平面)

    private Vector3 _currentOffset;      // 当前偏移量
    private float _currentZoom = 10f;    // 当前缩放值

    void Start()
    {
        // 初始化相机位置
        UpdateCameraAngle();
        _currentZoom = Mathf.Clamp(_currentZoom, minZoom, maxZoom);
    }

    void LateUpdate()
    {
        if (target == null) return;

        UpdateCameraAngle();
        HandleZoomInput();
        FollowTarget();
        ApplyBoundaryLimits();
    }

    // 更新相机角度计算
    void UpdateCameraAngle()
    {
        // 根据俯仰角计算偏移方向
        Quaternion rotation = Quaternion.Euler(pitchAngle, 0, 0);
        _currentOffset = rotation * Vector3.back * _currentZoom;
    }

    // 处理鼠标滚轮缩放
    void HandleZoomInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        _currentZoom = Mathf.Clamp(_currentZoom - scroll * zoomSensitivity, minZoom, maxZoom);
    }

    // 平滑跟随目标
    void FollowTarget()
    {
        Vector3 targetPosition = target.position + _currentOffset + Vector3.up * baseHeight;
        
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );

        // 保持恒定旋转角度
        Quaternion targetRotation = Quaternion.Euler(pitchAngle, 0, 0);
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    // 应用地图边界限制
    void ApplyBoundaryLimits()
    {
        if (!useBounds) return;

        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(
            clampedPosition.x,
            mapCenter.x - mapSize.x/2,
            mapCenter.x + mapSize.x/2
        );
        clampedPosition.z = Mathf.Clamp(
            clampedPosition.z,
            mapCenter.y - mapSize.y/2,
            mapCenter.y + mapSize.y/2
        );

        transform.position = clampedPosition;
    }

    // 调试显示地图边界
    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3(mapCenter.x, 0, mapCenter.y);
        Vector3 size = new Vector3(mapSize.x, 0.1f, mapSize.y);
        Gizmos.DrawWireCube(center, size);
    }
}