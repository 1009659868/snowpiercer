using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStack : MonoBehaviour
{
    public const int MAX_STACK_SIZE = 3;
    [SerializeField] private MouseFocusChecker mouseFocusChecker;
    [SerializeField] private PlayerKeyBinding binding;
    [SerializeField] private Selector selector;
    [SerializeField] private Transform stackOrigin;
    
    private Stack<IStackable> stack = new Stack<IStackable>();
    private StackEvent stackEvent = StackEvent.NONE;
    
    public bool isDropOrGrab => PlayerKeyBinding.isDown(binding.dropOrGrabKeys);
    public bool isHighThrowDown => PlayerKeyBinding.isDown(binding.throwKeys);
    public bool isHighThrowHold => PlayerKeyBinding.isPressed(binding.throwKeys);
    public bool isHighThrowUp => PlayerKeyBinding.isUp(binding.throwKeys);
    public StackableType stackedType => stack.Peek().type;
    public bool isStackEmpty => stack.Count == 0;
    public bool isStackFull => stack.Count >= MAX_STACK_SIZE;

    private GameObject focus { get; set; }
    [Header("Range")]
    [SerializeField] private float interactRange = 5f;
    [Header("High Throw Settings")]
    [SerializeField] private float gravity=-9.81f;
    [SerializeField] private LineRenderer trajectoryRenderer;
    [SerializeField] private float throwForce = 10f;
    [SerializeField] private float maxChargeTime = 2f;
    [SerializeField] private LayerMask groundLayer;

    private float throwChargeTime = 0f;
    private bool isChargingThrow = false;

    void Start()
    {
        trajectoryRenderer.enabled = false;
    }
    private void Update()
    {
        stackEvent = StackEvent.NONE;
        // focus = focusChecker.focus;
        focus = mouseFocusChecker.mouseFocus;

        HandleRailLinking();
        HandleGrab();
        HandleDrop();
        CarryStackables();
        HandleHighThrow();
    }

    private void HandleGrab()
    {
        
        if (stackEvent != StackEvent.NONE) return;

        if (isStackFull) return;

        if (focus == null) return;
        if (focus.TryGetComponent<Rigidbody>(out var rb) && rb != null) return;

        if (focus.TryGetComponent(out IStackable stackable))
        {
            if (!((!isStackEmpty) || isDropOrGrab) || stackable.isFlying) return;
            var grabbed = stackable.Peek();
            try
            {
                if ((!isDropOrGrab) && (((ILinkable)grabbed).previous != null)) return;
    
                if (isDropOrGrab && ((ILinkable)grabbed).next != null) return;
            }
            catch (System.Exception){}

            stackEvent = StackEvent.GRAB;

            while (!isStackFull)
            {
                if (!isStackEmpty)
                {
                    if (grabbed.type != stackedType) break;

                    if (stack.Contains(grabbed)) break;
                }

                if (grabbed.lower == null)
                {
                    grabbed.Clear();
                    if (!isStackEmpty)
                    {
                        grabbed.lower = stack.Peek();
                        stack.Peek().upper = grabbed;
                    }
                    stack.Push(grabbed);
                    grabbed.isGrabbed = true;
                    try
                    {    
                        if (((ILinkable)grabbed).previous != null)
                        {
                            ((ILinkable)grabbed).previous.next = null;
                            ((ILinkable)grabbed).previous = null;
                        }
                    }
                    catch (System.Exception){}
                    grabbed.Reset();
                    break;
                }
                else
                {
                    grabbed.lower.upper = null;
                    var next = grabbed.lower;
                    grabbed.Clear();
                    if (!isStackEmpty)
                    {
                        grabbed.lower = stack.Peek();
                        stack.Peek().upper = grabbed;
                    }
                    stack.Push(grabbed);
                    grabbed.isGrabbed = true;
                    grabbed.Reset();

                    grabbed = next;
                }
            }
        }
    }

    private void HandleDrop()
    {
        if (stackEvent != StackEvent.NONE) return;

        if (!isDropOrGrab) return;

        if (focus != null)
        {
            if (focus.TryGetComponent(out IStackable pivot))
            {
                stackEvent = StackEvent.DROP;

                IStackable peek = pivot.Peek();

                if (peek.type != stackedType) return;

                try
                {
                    if (((ILinkable)peek).previous != null) return;
                }
                catch (System.Exception){}

                while (true)
                {
                    if (!stack.TryPop(out IStackable stackable)) break;
                    
                    stackable.isGrabbed = false;

                    stackable.Clear();
                    stackable.lower = peek;
                    peek.upper = stackable;

                    stackable.SnapToGrid(peek.anchor);

                    stackable.Reset();

                    peek = stackable;
                }
            }
        }
        else
        {
            stackEvent = StackEvent.DROP;

            IStackable peek = null;

            while (true)
            {
                if (!stack.TryPop(out IStackable stackable)) break;

                stackable.isGrabbed = false;

                stackable.Clear();
                stackable.lower = peek;
                if (peek != null)
                {
                    peek.upper = stackable;
                    stackable.SnapToGrid(peek.anchor);
                }
                else
                {
                    stackable.SnapToGrid(mouseFocusChecker.worldPosition);
                    // stackable.SnapToGrid(focusChecker.worldPosition);
                }

                stackable.Reset();

                peek = stackable;
            }
        }
    }

    private void HandleRailLinking()
    {
        if (!selector.isPreviewing) return;

        if (!isDropOrGrab) return;

        if (stack.TryPeek(out IStackable test))
        {
            try
            {
                var temp = (ILinkable)test;
            }
            catch (System.Exception)
            {
                return;
            }
        }

        if (stack.TryPop(out IStackable stackable))
        {
            ILinkable linkable = stackable as ILinkable;

            if (linkable == null) return;
            // 将新轨道添加到系统
            Railway.Instance.AddRail(linkable as Rail);
            linkable.LinkWithPrevious();
            stackEvent = StackEvent.LINK;

            if (stack.TryPeek(out IStackable peek)) peek.upper = null;
            stackable.isGrabbed = false;
            stackable.Clear();
            stackable.SnapToGrid(mouseFocusChecker.worldPosition);
            // stackable.SnapToGrid(focusChecker.worldPosition);
            linkable.LinkWithPrevious();
        }
    }

    private void CarryStackables()
    {
        if (isStackEmpty) return;
        
        foreach (var stackable in stack)
        {
            stackable.SnapToStack(stackable.lower == null ? stackOrigin.position : stackable.lower.anchor, transform.eulerAngles);
        }
    }



    public void ConsumeResource(int amount)
    {
        for (int i = 0; i < amount && stack.Count > 0; i++)
        {
            IStackable item = stack.Pop();
            // if (item != null) Destroy();
        }
    }
     private bool IsInRange(Vector3 targetPos)
    {
        return Vector3.Distance(stackOrigin.position, targetPos) <= interactRange;
    }

    private void HandleHighThrow()
    {
        if (isStackEmpty) return;
        
        if (isHighThrowDown)
        {
            throwChargeTime = 0f;
            isChargingThrow = true;
            trajectoryRenderer.enabled = true;
        }

        if (isHighThrowHold && isChargingThrow)
        {
            
            throwChargeTime += Time.deltaTime;
            throwChargeTime = Mathf.Min(throwChargeTime, maxChargeTime);

            Vector3 velocity = CalculateArcVelocity(CalculateThrowPos(), mouseFocusChecker.worldPosition, throwChargeTime);

            ShowTrajectory(CalculateThrowPos(), velocity);
        }

        if (isHighThrowUp && isChargingThrow)
        {
            
            isChargingThrow = false;
            // trajectoryRenderer.enabled = false;
            
            Vector3 velocity = CalculateArcVelocity(CalculateThrowPos(), mouseFocusChecker.worldPosition, throwChargeTime);

            ThrowTopStackable(CalculateThrowPos(), velocity);
        }
    }
    private Vector3 CalculateThrowPos(){
        GameObject playerObj = transform.parent.gameObject;
        Collider playerCollider = playerObj.GetComponent<Collider>();
        Vector3 position = transform.parent.position;
        float height = playerCollider.bounds.size.y;
        position.y+=height;
        return position;
    }
    private Vector3 CalculateArcVelocity(Vector3 origin, Vector3 target, float chargeRatio)
    {
        float maxYSpeed = 25f;
        float maxTotalSpeed=20f;
        // chargeRatio 范围应是 0 ~ 1，表示蓄力程度
        chargeRatio = Mathf.Clamp01(chargeRatio);

        Vector3 toTarget = target - origin;

        // 水平距离 & 水平方向
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
        float xzDistance = toTargetXZ.magnitude;

        // 高度差
        float heightDifference = toTarget.y;

        // 高抛 Y 方向初速度（强制偏高）
        float ySpeed = Mathf.Lerp(5f, maxYSpeed, chargeRatio); // maxYSpeed 建议 10~20
        float gravity = Mathf.Abs(Physics.gravity.y);

        // 计算飞行时间：使用 Y 方向速度决定时间
        float flightTime = (ySpeed + Mathf.Sqrt(ySpeed * ySpeed + 2 * gravity * Mathf.Max(heightDifference, 0.1f))) / gravity;

        // 水平方向速度
        float xzSpeed = xzDistance / flightTime;

        // 应用速度限制（防穿模）
        float maxSpeed = maxTotalSpeed; // 如 25f
        float totalSpeed = Mathf.Sqrt(xzSpeed * xzSpeed + ySpeed * ySpeed);
        if (totalSpeed > maxSpeed)
        {
            float scale = maxSpeed / totalSpeed;
            xzSpeed *= scale;
            ySpeed *= scale;
        }

        Vector3 velocity = toTargetXZ.normalized * xzSpeed;
        velocity.y = ySpeed;

        return velocity;
    }



    private void ShowTrajectory(Vector3 startPos, Vector3 velocity)
    {
        int resolution = 30;
        Vector3[] points = new Vector3[resolution];
        float usedGravity = Mathf.Abs(gravity);

        // 估算整个飞行时间：注意这里是假设完全对称的上抛落地时间
        float flightTime = (2 * velocity.y) / usedGravity;
        flightTime = Mathf.Max(flightTime, 0.5f);

        for (int i = 0; i < resolution; i++)
        {
            float t = (i / (float)(resolution - 1)) * flightTime;
            Vector3 point = startPos + velocity * t + 0.5f * new Vector3(0, -usedGravity, 0) * t * t;
            points[i] = point;

            // 绘制调试线段帮助检查轨迹点
            Debug.DrawLine(point, point + Vector3.up * 0.1f, Color.red, 1f);
        }
        trajectoryRenderer.enabled=true;
        trajectoryRenderer.positionCount = resolution;
        trajectoryRenderer.SetPositions(points);
    }



    private void ThrowTopStackable(Vector3 position, Vector3 velocity)
    {
        if (stackEvent != StackEvent.NONE) return; // 防止状态冲突

        if (!stack.TryPop(out IStackable stackable)) return;
        // 清除连接关系
        if (stackable.upper != null) stackable.upper.lower = null;
        if (stackable.lower != null) stackable.lower.upper = null;

        stackable.upper = null;
        stackable.lower = null;
        stackable.isGrabbed = false;
        stackable.isFlying =true;
        stackable.Clear(); // 清除路径/状态等
        stackable.Reset();
        GameObject obj = ((MonoBehaviour)stackable).gameObject;
        obj.transform.position = position;
        obj.SetActive(true);
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null) rb = obj.AddComponent<Rigidbody>();

        rb.velocity = velocity;
    }
    

}

public enum StackEvent { NONE, GRAB, DROP, LINK }
