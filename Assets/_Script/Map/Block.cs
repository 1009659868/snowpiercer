using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

#region Block
[System.Serializable]
public class Block :Node
{
    public Vector3 position { get; set; }
    //地块大小,占地范围
    public Vector3 size= new Vector3(4,4,4);
    public GameObject prefab;
    public GameObject blockObject;
    public BlockType type;
    //是否允许破坏
    public bool isDestroyable;
    public bool isWalkable;
    public bool isBuildable;
    public bool isHarvestable;
    public bool isHaveHarvest=false;
    // 6个面是否可见（上、下、左、右、前、后）
    public bool[] visibleFaces = new bool[6] { true, true, true, true, true, true };
    // 记录每个面被哪些邻居遮挡（用于精确恢复）
    private Dictionary<int, List<Vector3>> _occlusionRecords = new Dictionary<int, List<Vector3>>();


    protected override void OnMouseEnter()
    {
        base.OnMouseEnter();
    }
    protected override void OnMouseExit()
    {
        base.OnMouseExit();
    }
    protected override void OnMouseDown()
    {
        // Debug.Log("创建并放置炮塔");
        if(EventSystem.current.IsPointerOverGameObject()) return;
        if(BuildManager._instance.Selected==null || BuildManager._instance.type!=RecipeType.MachineGun) return;
        
        GameObject building = Instantiate(BuildManager._instance.Selected,transform.position+offset/2,Quaternion.identity);
        building.transform.localScale*=2;
        base.OnMouseDown();
    }


    public void Initialize(Vector3 position,BlockType type,GameObject obj)
    {
        this.position = MyGrid._instance.DetailGridToWorld(MyGrid._instance.WorldToDetailGrid(position));
        this.size = Vector3.one;
        this.blockObject = obj;
        this.type = type;
    }
    public bool HasVisual()
    {
        return type != BlockType.Air && blockObject != null;
    }
    public void UpdateFaceVisibility(Dictionary<Vector3, Block> allBlocks)
    {
        Vector3[] faceDirections = new Vector3[]
        {
            Vector3.up, Vector3.down,
            Vector3.left, Vector3.right,
            Vector3.forward, Vector3.back
        };

        for (int faceIndex = 0; faceIndex < 6; faceIndex++)
        {
            Vector3 checkDir = faceDirections[faceIndex];
            Vector3 neighborPos = position + checkDir * size.x;

            // 清除旧记录
            if (!_occlusionRecords.ContainsKey(faceIndex))
                _occlusionRecords[faceIndex] = new List<Vector3>();
            else
                _occlusionRecords[faceIndex].Clear();

            // 检测所有可能遮挡的方块（考虑不同尺寸方块）
            bool isOccluded = false;
            foreach (var offset in GetPotentialOcclusionOffsets(checkDir))
            {
                Vector3 checkPos = position + offset;
                if (allBlocks.TryGetValue(checkPos, out Block neighbor) && 
                    neighbor.type != BlockType.Air)
                {
                    _occlusionRecords[faceIndex].Add(checkPos);
                    isOccluded = true;
                }
            }

            // 只有完全被遮挡时才隐藏面
            visibleFaces[faceIndex] = !isOccluded;
        }
    }
    // 获取可能遮挡当前面的所有偏移位置
    private IEnumerable<Vector3> GetPotentialOcclusionOffsets(Vector3 direction)
    {
        yield return direction * size.x*MyGrid._instance.detailCellSize.x; // 标准相邻位置
        // 如果是大网格系统，可能需要检查更多位置
        if (size.x > 1f)
        {
            // 添加对大尺寸方块的额外检测点
            // 示例：对2x2x2方块的额外检测
            if (direction == Vector3.right)
            {
                yield return direction * size.x + Vector3.forward;
                yield return direction * size.x + Vector3.back;
                yield return direction * size.x + Vector3.up;
                yield return direction * size.x + Vector3.down;
            }else if(direction == Vector3.left){
                yield return direction * size.x + Vector3.forward;
                yield return direction * size.x + Vector3.back;
                yield return direction * size.x + Vector3.up;
                yield return direction * size.x + Vector3.down;
            }else if(direction == Vector3.up){
                yield return direction * size.x + Vector3.right;
                yield return direction * size.x + Vector3.left;
                yield return direction * size.x + Vector3.forward;
                yield return direction * size.x + Vector3.back;
            }else if(direction == Vector3.down){
                yield return direction * size.x + Vector3.right;
                yield return direction * size.x + Vector3.left;
                yield return direction * size.x + Vector3.forward;
                yield return direction * size.x + Vector3.back;
            }else if(direction == Vector3.forward){
                yield return direction * size.x + Vector3.right;
                yield return direction * size.x + Vector3.left;
                yield return direction * size.x + Vector3.up;
                yield return direction * size.x + Vector3.down;
            }else if(direction == Vector3.back){
                yield return direction * size.x + Vector3.right;
                yield return direction * size.x + Vector3.left;
                yield return direction * size.x + Vector3.up;
                yield return direction * size.x + Vector3.down;
            }
        }
        
    }
    // 当邻居被移除时恢复面
    public void RestoreFace(Vector3 removedNeighborPos)
    {
        for (int faceIndex = 0; faceIndex < 6; faceIndex++)
        {
            if (_occlusionRecords.ContainsKey(faceIndex) && 
                _occlusionRecords[faceIndex].Contains(removedNeighborPos))
            {
                _occlusionRecords[faceIndex].Remove(removedNeighborPos);
                visibleFaces[faceIndex] = _occlusionRecords[faceIndex].Count == 0;
            }
        }
    }
    // 应用面可见性到网格 - 更通用的版本
    public void ApplyFaceVisibility()
    {
        if (blockObject == null || type == BlockType.Air) return;

        MeshFilter meshFilter = blockObject.GetComponent<MeshFilter>();

        if (meshFilter == null) return;

        Mesh originalMesh = meshFilter.sharedMesh;
        Mesh newMesh = new Mesh();
        MeshRenderer renderer = blockObject.GetComponent<MeshRenderer>();
        Material material = renderer.material;
        material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off); // 禁用剔除
        
        
        // 收集可见面的顶点数据
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();

        int vertexOffset = 0;

        // 上下面
        if (visibleFaces[0]) AddFace(Vector3.up, vertices, triangles, uvs, normals, ref vertexOffset);
        if (visibleFaces[1]) AddFace(Vector3.down, vertices, triangles, uvs, normals, ref vertexOffset);

        // 左右面
        if (visibleFaces[2]) AddFace(Vector3.left, vertices, triangles, uvs, normals, ref vertexOffset);
        if (visibleFaces[3]) AddFace(Vector3.right, vertices, triangles, uvs, normals, ref vertexOffset);

        // 前后面
        if (visibleFaces[4]) AddFace(Vector3.forward, vertices, triangles, uvs, normals, ref vertexOffset);
        if (visibleFaces[5]) AddFace(Vector3.back, vertices, triangles, uvs, normals, ref vertexOffset);

        newMesh.vertices = vertices.ToArray();
        newMesh.triangles = triangles.ToArray();
        newMesh.uv = uvs.ToArray();
        newMesh.normals = normals.ToArray();

        meshFilter.mesh = newMesh;
    }

    private void AddFace(Vector3 direction, List<Vector3> vertices, List<int> triangles,
                        List<Vector2> uvs, List<Vector3> normals, ref int vertexOffset)
    {
        float halfSize = size.x * 0.5f;
        Vector3[] faceVertices = new Vector3[4];

        // 根据方向定义4个顶点
        if (direction == Vector3.up) // Top
        {
            faceVertices = new[]
            {
                new Vector3(-halfSize, halfSize, -halfSize),
                new Vector3(halfSize, halfSize, -halfSize),
                new Vector3(halfSize, halfSize, halfSize),
                new Vector3(-halfSize, halfSize, halfSize)
            };
        }
        else if (direction == Vector3.down) // Bottom
        {
            faceVertices = new[]
            {
                new Vector3(-halfSize, -halfSize, halfSize),
                new Vector3(halfSize, -halfSize, halfSize),
                new Vector3(halfSize, -halfSize, -halfSize),
                new Vector3(-halfSize, -halfSize, -halfSize)
            };
        }
        else if (direction == Vector3.left) // Left
        {
            faceVertices = new[]
            {
                new Vector3(-halfSize, halfSize, halfSize),
                new Vector3(-halfSize, halfSize, -halfSize),
                new Vector3(-halfSize, -halfSize, -halfSize),
                new Vector3(-halfSize, -halfSize, halfSize)
            };
        }
        else if (direction == Vector3.right) // Right
        {
            faceVertices = new[]
            {
                new Vector3(halfSize, halfSize, -halfSize),
                new Vector3(halfSize, halfSize, halfSize),
                new Vector3(halfSize, -halfSize, halfSize),
                new Vector3(halfSize, -halfSize, -halfSize)
            };
        }
        else if (direction == Vector3.forward) // Front (Z+)
        {
            faceVertices = new[]
            {
                new Vector3(-halfSize, halfSize, halfSize),
                new Vector3(halfSize, halfSize, halfSize),
                new Vector3(halfSize, -halfSize, halfSize),
                new Vector3(-halfSize, -halfSize, halfSize)
            };
        }
        else if (direction == Vector3.back) // Back (Z-)
        {
            faceVertices = new[]
            {
                new Vector3(halfSize, halfSize, -halfSize),
                new Vector3(-halfSize, halfSize, -halfSize),
                new Vector3(-halfSize, -halfSize, -halfSize),
                new Vector3(halfSize, -halfSize, -halfSize)
            };
        }

        vertices.AddRange(faceVertices);
        normals.AddRange(new[] { direction, direction, direction, direction });
        uvs.AddRange(new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });

        triangles.Add(vertexOffset);
        triangles.Add(vertexOffset + 1);
        triangles.Add(vertexOffset + 2);
        triangles.Add(vertexOffset);
        triangles.Add(vertexOffset + 2);
        triangles.Add(vertexOffset + 3);

        vertexOffset += 4;

    }
}
#endregion


