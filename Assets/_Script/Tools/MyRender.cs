using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MyRender : MonoBehaviour
{
    // void Start()
    // {
    //     // 创建仅包含顶面的网格
    //     MeshFilter meshFilter = GetComponent<MeshFilter>();
    //     Mesh mesh = new Mesh();
    //     MeshRenderer renderer = GetComponent<MeshRenderer>();
    //     Material material = renderer.material;
    //     material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off); // 禁用剔除
    //     mesh.name = "TopFaceOnly";

    //     // 顶点坐标（基于Unity默认Cube的中心坐标系）
    //     Vector3[] vertices = new Vector3[4]
    //     {
    //         new Vector3(-0.5f, 0.5f, -0.5f), // 左上
    //         new Vector3(0.5f, 0.5f, -0.5f),  // 右上
    //         new Vector3(0.5f, 0.5f, 0.5f),   // 右下
    //         new Vector3(-0.5f, 0.5f, 0.5f)   // 左下
    //     };

    //     // 三角形索引（顺时针方向）
    //     int[] triangles = new int[6]
    //     {
    //         0, 1, 2, // 第一个三角形
    //         0, 2, 3  // 第二个三角形
    //     };

    //     // UV坐标（简单展开）
    //     Vector2[] uv = new Vector2[4]
    //     {
    //         new Vector2(0, 0),
    //         new Vector2(1, 0),
    //         new Vector2(1, 1),
    //         new Vector2(0, 1)
    //     };

    //     // 法线方向（全部朝上）
    //     Vector3[] normals = new Vector3[4]
    //     {
    //         Vector3.up,
    //         Vector3.up,
    //         Vector3.up,
    //         Vector3.up
    //     };

    //     mesh.vertices = vertices;
    //     mesh.triangles = triangles;
    //     mesh.uv = uv;
    //     mesh.normals = normals;

    //     mesh.RecalculateBounds();
    //     meshFilter.mesh = mesh;
    // }


    public static Dictionary<Vector3Int, MyRender> AllCubes = new Dictionary<Vector3Int, MyRender>();
    public Vector3Int GridPosition;
    
    private MeshFilter _meshFilter;
    private Mesh _mesh;
    private static readonly Vector3[] _faceDirections = 
    {
        Vector3.up, Vector3.down, 
        Vector3.left, Vector3.right, 
        Vector3.forward, Vector3.back
    };

    void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _mesh = new Mesh();
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        Material material = renderer.material;
        material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off); // 禁用剔除
        _mesh.name = "DynamicCube";
        AllCubes[GridPosition] = this;
    }

    void Start() => UpdateMesh();
    
    void OnDestroy()
    {
        AllCubes.Remove(GridPosition);
        UpdateNeighbors();
    }

    public void UpdateMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();

        int vertexIndex = 0;
        
        foreach (var direction in _faceDirections)
        {
            if (!HasNeighbor(GridPosition + Vector3Int.RoundToInt(direction)))
            {
                AddFace(direction, vertices, triangles, uvs, normals, ref vertexIndex);
            }
        }

        _mesh.Clear();
        _mesh.vertices = vertices.ToArray();
        _mesh.triangles = triangles.ToArray();
        _mesh.uv = uvs.ToArray();
        _mesh.normals = normals.ToArray();
        _mesh.RecalculateBounds();
        _meshFilter.mesh = _mesh;
    }

    private bool HasNeighbor(Vector3Int checkPos)
    {
        return AllCubes.ContainsKey(checkPos);
    }

    private void AddFace(Vector3 direction, List<Vector3> vertices, List<int> triangles, 
                        List<Vector2> uvs, List<Vector3> normals, ref int vertexIndex)
    {
        float halfSize = 0.5f;
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
        // 添加顶点数据
        vertices.AddRange(faceVertices);
        normals.AddRange(new[] { direction, direction, direction, direction });
        uvs.AddRange(new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
        
        // 添加三角形
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
        
        vertexIndex += 4;
    }

    private void UpdateNeighbors()
    {
        foreach (var direction in _faceDirections)
        {
            Vector3Int neighborPos = GridPosition + Vector3Int.RoundToInt(direction);
            if (AllCubes.TryGetValue(neighborPos, out var neighbor))
            {
                neighbor.UpdateMesh();
            }
        }
    }
}
