using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("基础设置")]
    private GameObject target;             // 跟随目标
    private Camera _camera;
    public float followSpeed = 5f;       // 跟随速度
    public float rotationSpeed = 2f;    // 旋转速度

    [Header("视角参数")]
    [Range(30, 80)] public float pitchAngle = 45f; // 俯视角（X轴旋转）
    public float baseHeight = 15f;       // 基础高度
    public float orthographicSize = 20f;  // 正交视口尺寸
    public float zoomSensitivity = 5f;  // 缩放灵敏度
    public float minZoom = 5f;           // 最小缩放距离
    public float maxZoom = 20f;          // 最大缩放距离

    [Header("边界限制")]
    private Vector3 _currentOffset;      // 当前偏移量
    private float _currentZoom = 10f;    // 当前缩放值
    private Vector3 dir;

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player");

        if (target == null)
        {
            Debug.LogError("Player with tag 'Player' not found!");
        }
        _camera=GetComponent<Camera>();
        if(_camera==null){
            Debug.LogError("camera get Null");
        }
        dir=target.transform.position-transform.position;
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
        if(scroll!=0){
            orthographicSize=Mathf.Clamp(
                orthographicSize-scroll*2f,
                minZoom,
                maxZoom
            );
            ApplyCameraSettings();
        }
    }

    // 平滑跟随目标
    void FollowTarget()
    {
        Vector3 targetPosition = target.transform.position + _currentOffset + Vector3.up * baseHeight;
        
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
    void ApplyCameraSettings(){
        if(_camera==null) return;

        //设置正交尺寸
        if(_camera.orthographic){
            _camera.orthographicSize = orthographicSize;
        }

    }
}