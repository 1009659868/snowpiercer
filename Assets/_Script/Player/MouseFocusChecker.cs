using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseFocusChecker : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayer;
    // 最大检测距离，可根据需要调整
    [SerializeField] private float maxDistance = 100f;

    // 当前鼠标指向交互对象对应的世界坐标
    public Vector3 worldPosition { get; private set; }

    /// <summary>
    /// 通过鼠标射线检测返回屏幕上点击或悬停的交互对象
    /// </summary>
    public GameObject mouseFocus
    {
        get
        {
            // 根据鼠标当前屏幕位置生成射线
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.red);

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, targetLayer))
            {
                // 若需要网格对齐，可调用类似 MyGrid 的方法进行转换
                // 例如：worldPosition = MyGrid._instance.GroundGridToWorld(MyGrid._instance.WorldToGroundGrid(hit.point));
                worldPosition = MyGrid._instance.GroundGridToWorld(MyGrid._instance.WorldToGroundGrid(hit.point));
                // Debug.Log("_____________-");
                // Debug.Log(worldPosition);
                // Debug.Log(hit.point);
            
                GameObject hitObject = hit.collider.gameObject;
                if (hitObject.TryGetComponent(out IStackable stackable))
                {
                    if (!stackable.isGrabbed&&!stackable.isFlying)
                    {
                        // Debug.Log("stackable");
                        return hitObject;
                    }
                }
                // else if(hitObject.TryGetComponent(out IInteractable interactable)){
                //     // Debug.Log("interactable");
                //     return hitObject;
                // }
                else if (hitObject.TryGetComponent(out IHarvestable harvestable))
                {
                    // Debug.Log("IHarvestable");
                    return hitObject;
                }
                
            }
            return null;
        }
    }

    private void OnDrawGizmos()
    {
        // 如果检测到交互目标则在对应的 worldPosition 绘制一个小球，可用于调试
        if (mouseFocus != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(worldPosition, 1f);
        }
    }
}
