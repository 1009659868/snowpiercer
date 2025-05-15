using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Resource : MonoBehaviour, IGrid, IStackable, ISelectable
{
    [SerializeField] private Transform stackAnchor;
#region IGrid interface
    public GVector2Int gridPosition => MyGrid._instance.WorldToGroundGrid(transform.position);
    public Vector3 worldPosition => MyGrid._instance.GroundGridToWorld(gridPosition);
    public Vector3 realPosition => transform.position;
    public Vector3 realRotation => transform.eulerAngles;

    public virtual void SnapToGrid(Vector3 position)
    {
        transform.position = position;
        transform.eulerAngles = Vector3.zero;
    }

#endregion

#region IStackable interface
    public abstract StackableType type { get; }
    public Vector3 anchor => stackAnchor.position;
    public bool isGrabbed { get; set; }
    public bool isFlying { get; set; }
    public IStackable upper { get; set; }
    public IStackable lower { get; set; }

    public virtual void Clear()
    {
        this.upper = null;
        this.lower = null;
    }

    public virtual void Flip()
    {
        var temp = this.upper;
        this.upper = this.lower;
        this.lower = temp;
    }

    public virtual IStackable Peek()
    {
        IStackable result = this.GetComponent<IStackable>();
        while (result.upper != null)
        {
            result = result.upper;
        }
        return result;
    }

    public virtual void Reset()
    {
        transform.eulerAngles = Vector3.zero;
    }

    public virtual void SnapToStack(Vector3 position, Vector3 rotation)
    {
        transform.position = position;
        transform.rotation = Quaternion.Euler(rotation);
    }
    public virtual void SnapToStorage(Transform position,Transform parent,Vector3 scale){
        var rb = GetComponent<Rigidbody>();
        if(rb!=null) Destroy(rb);

        // 断开所有连接
        if (this is ILinkable linkable)
        {
            linkable.previous = null;
            linkable.next = null;
        }


        //设置父物体和本地位置/旋转
        transform.SetParent(parent);
        transform.localPosition = position.localPosition;
        transform.localRotation = Quaternion.identity;
        transform.localScale = scale;
        //重置状态
        isGrabbed = false;
        isFlying = false;
        Clear();
        
    }

#endregion

#region ISelectable interface
    public List<Material> oldMaterials { get; set; }

    public virtual void Select(Material material)
    {   
        bool isInit = (oldMaterials == null);

        if (isInit) oldMaterials = new List<Material>();

        foreach (var mesh in this.GetComponentsInChildren<MeshRenderer>())
        {
            if (isInit) oldMaterials.Add(mesh.material);

            mesh.material = material;
        }
    }

    public virtual void Deselect()
    {
        int i = 0;
        foreach (var mesh in this.GetComponentsInChildren<MeshRenderer>())
        {
            mesh.material = oldMaterials[i];
            i++;
        }
    }
    public virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
            IStackable stackable=this.GetComponent<IStackable>();
            // 延迟0.5秒后再允许被拾取
            StartCoroutine(ResetFlying());
        }
    }
    private IEnumerator ResetFlying()
    {
        yield return new WaitForSeconds(0.5f);
        this.isFlying = false;
    }
#endregion
}