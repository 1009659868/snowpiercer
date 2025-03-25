using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyGrid : MonoBehaviour
{
    public static MyGrid _instance { get; private set; }
    public Transform player;

    [Header("大网格配置")]
    public Vector3Int largerGridSize=new Vector3Int(10,5,10);
    public Vector3 largerCellSize = new Vector3Int(4,4,4);
    public bool showLargeGrid = true;
    [Header("小网格配置")]
    public Vector3Int detailGridSize=new Vector3Int(40,20,40);
    public Vector3 detailCellSize = new Vector3(1,1,1);
    public bool showDetailGrid = true;
    [Header("地面网格配置")]
    public Vector2Int groundGridSize=new Vector2Int(10,10);
    public int cellSize = 4;
    public bool showGroundGrid = true;

    private static Vector3 origin;
    

    private void Awake()
    {
        _instance = this;
        origin = transform.position;
    }
    #region 大网格系统
    public GVector3Int WorldToLargeGrid(Vector3 worldPos)
    {
        return new GVector3Int(
            Mathf.FloorToInt((worldPos.x - origin.x) / largerCellSize.x),
            Mathf.FloorToInt((worldPos.y - origin.y) / largerCellSize.y),
            Mathf.FloorToInt((worldPos.z - origin.z) / largerCellSize.z)
        );
    }
    public Vector3 LargeGridToWorld(GVector3Int gridPos)
    {
        return new Vector3(
            gridPos.x * largerCellSize.x + origin.x + largerCellSize.x * 0.5f,
            gridPos.y * largerCellSize.y + origin.y + largerCellSize.y * 0.5f,
            gridPos.z * largerCellSize.z + origin.z + largerCellSize.z * 0.5f
        );
    }
    public bool IsValidLargeGridPosition(GVector3Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < largerGridSize.x &&
               gridPosition.y >= 0 && gridPosition.y < largerGridSize.y &&
               gridPosition.z >= 0 && gridPosition.z < largerGridSize.z;
    }
    #endregion
    #region 小网格系统
    public GVector3Int WorldToDetailGrid(Vector3 worldPos)
    {
        return new GVector3Int(
            Mathf.FloorToInt((worldPos.x - origin.x) / detailCellSize.x),
            Mathf.FloorToInt((worldPos.y - origin.y) / detailCellSize.y),
            Mathf.FloorToInt((worldPos.z - origin.z) / detailCellSize.z)
        );
    }
    public Vector3 DetailGridToWorld(GVector3Int gridPos)
    {
        return new Vector3(
            gridPos.x * detailCellSize.x + origin.x + detailCellSize.x * 0.5f,
            gridPos.y * detailCellSize.y + origin.y + detailCellSize.y * 0.5f,
            gridPos.z * detailCellSize.z + origin.z + detailCellSize.z * 0.5f
        );
    }
    public bool IsValidDetailGridPosition(GVector3Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < detailGridSize.x &&
               gridPosition.y >= 0 && gridPosition.y < detailGridSize.y &&
               gridPosition.z >= 0 && gridPosition.z < detailGridSize.z;
    }
    #endregion
    #region 地面网格系统
    public GVector2Int WorldToGroundGrid(Vector3 worldPos)
    {
        return new GVector2Int(
            Mathf.FloorToInt((worldPos.x - origin.x) / cellSize),
            Mathf.FloorToInt((worldPos.z - origin.z) / cellSize)
        );
    }
    public Vector3 GroundGridToWorld(GVector2Int gridPos)
    {
        return new Vector3(
            gridPos.x * cellSize + origin.x + cellSize * 0.5f,
            origin.y,
            gridPos.y * cellSize + origin.z + cellSize * 0.5f
        );
    }
    public bool IsValidGroundGridPosition(GVector2Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < groundGridSize.x &&
               gridPosition.y >= 0 && gridPosition.y < groundGridSize.y;
    }
    #endregion
    #region 网格坐标转换
    //大网格转地面网格
    public GVector2Int LargeGridToGroundGrid(GVector3Int largeGridPos)
    {
        return new GVector2Int(largeGridPos.x, largeGridPos.z);
    }
    //小网格转大网格
    public GVector3Int DetailGridToLargeGrid(GVector3Int detailGridPos)
    {
        return new GVector3Int(
            (int)(detailGridPos.x / largerCellSize.x),
            (int)(detailGridPos.y / largerCellSize.y),
            (int)(detailGridPos.z / largerCellSize.z)
        );
    }
    //地面坐标转大网格
    
    #endregion
    #region 网格可视化
    
    private void OnDrawGizmos()
    {
        if (player == null) return;
        if (showLargeGrid) DrawLargeGrid();
        if (showDetailGrid) DrawDetailGrid();
        if (showGroundGrid) DrawGroundGrid();
    }
    //绘制大网格
    private void DrawLargeGrid(){
        Gizmos.color = Color.green;
        for (int x = 0; x < largerGridSize.x; x++)
        {
            for (int y = 0; y < largerGridSize.y; y++)
            {
                for (int z = 0; z < largerGridSize.z; z++)
                {
                    int posX = -largerGridSize.x/2+x;
                    int posY = y-4;
                    int posZ = -largerGridSize.z/2+z;
                    //在player附近的大网格绘制为绿色，否则为红色
                    Vector3 worldPos = LargeGridToWorld(new GVector3Int(posX, posY, posZ));

                    if (Vector3.Distance(worldPos, player.position) < 10 * largerCellSize.x)
                    {
                        Gizmos.color = Color.green;
                    }
                    else
                    {
                        Gizmos.color = Color.red;
                    }
                    
                    
                    Gizmos.DrawWireCube(worldPos, new Vector3(largerCellSize.x, largerCellSize.y, largerCellSize.z));
                }
            }
        }
    }
    //绘制小网格
    private void DrawDetailGrid(){
        Gizmos.color = Color.yellow;
        for (int x = 0; x < detailGridSize.x; x++)
        {
            for (int y = 0; y < detailGridSize.y; y++)
            {
                for (int z = 0; z < detailGridSize.z; z++)
                {
                    int posX = -detailGridSize.x/2+x;
                    int posY = y-4;
                    int posZ = -detailGridSize.z/2+z;
                    //在player附近的小网格绘制为绿色，否则为黄色
                    Vector3 worldPos = DetailGridToWorld(new GVector3Int(posX, posY, posZ));

                    // 在player附近的小网格绘制为绿色，否则为黄色
                    if (Vector3.Distance(worldPos, player.position) < 10 * 4 * detailCellSize.x) {
                        Gizmos.color = Color.green;
                    } else {
                        Gizmos.color = Color.yellow;
                    }

                    Gizmos.DrawWireCube(worldPos, new Vector3(detailCellSize.x, detailCellSize.y, detailCellSize.z));
                }
            }
        }
    }
    //绘制地面网格
    private void DrawGroundGrid(){
        Gizmos.color = Color.white;
        for (int x = 0; x < groundGridSize.x; x++)
        {
            for (int y = 0; y < groundGridSize.y; y++)
            {
                int posX = -groundGridSize.x/2+x;
                int posY = -groundGridSize.y/2+y;

                //在player附近的地面网格绘制为蓝色，否则为蓝色
                Vector3 worldPos = GroundGridToWorld(new GVector2Int(posX, posY));

                // 在player附近的地面网格绘制为蓝色，否则为白色
                if (Vector3.Distance(worldPos, player.position) < cellSize) {
                    Gizmos.color = Color.blue;
                } else {
                    Gizmos.color = Color.white;
                }

                Gizmos.DrawWireCube(worldPos, new Vector3(cellSize, 0, cellSize));
            }
        }
    }
    #endregion

    

}
public enum GridType
{
    LargeGrid,
    DetailGrid,
    GroundGrid
}

[System.Serializable]
public struct GVector3Int
{
    public int x;
    public int y;
    public int z;

    public GVector3Int(int x, int y,int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static GVector3Int operator +(GVector3Int a, GVector3Int b)
    {
        return new GVector3Int(a.x + b.x, a.y + b.y, a.z + b.z);
    }
    
    public static GVector3Int operator -(GVector3Int a, GVector3Int b)
    {
        return new GVector3Int(a.x - b.x, a.y - b.y, a.z - b.z);
    }
    public static GVector3Int operator *(GVector3Int a, int b)
    {
        return new GVector3Int(a.x * b, a.y * b, a.z * b);
    }
    public static GVector3Int operator /(GVector3Int a, int b)
    {
        return new GVector3Int(a.x / b, a.y / b, a.z / b);
    }

    public static bool operator ==(GVector3Int a, GVector3Int b)
    {
        return a.x == b.x && a.y == b.y && a.z == b.z;
    }
    public static bool operator !=(GVector3Int a, GVector3Int b)
    {
        return a.x != b.x || a.y!=b.y || a.z != b.z;
    }

    public override bool Equals(object obj)
    {
        return obj is GVector3Int && this == (GVector3Int)obj;
    }
    public override int GetHashCode()
    {
        return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
    }

    public override string ToString()
    {
        return $"({x},{y},{z})";
    }

    public static GVector3Int zero { get; } = new GVector3Int(0, 0,0);
    public static GVector3Int one { get; } = new GVector3Int(1, 1,1);
    public static GVector3Int up { get; } = new GVector3Int(0, 1, 0);
    public static GVector3Int down { get; } = new GVector3Int(0, -1, 0);
    public static GVector3Int right { get; } = new GVector3Int(1, 0, 0);
    public static GVector3Int left { get; } = new GVector3Int(-1, 0, 0);
    public static GVector3Int forward { get; } = new GVector3Int(0, 0, 1);
    public static GVector3Int back { get; } = new GVector3Int(0, 0, -1);
}
[System.Serializable]
public struct GVector2Int{
    public int x;
    public int y;
    public GVector2Int(int x,int y){
        this.x=x;
        this.y=y;
    }
    public static GVector2Int operator +(GVector2Int a, GVector2Int b)
    {
        return new GVector2Int(a.x + b.x, a.y + b.y);
    }
    
    public static GVector2Int operator -(GVector2Int a, GVector2Int b)
    {
        return new GVector2Int(a.x - b.x, a.y - b.y);
    }
    public static GVector2Int operator *(GVector2Int a, int b)
    {
        return new GVector2Int(a.x * b, a.y * b);
    }
    public static GVector2Int operator /(GVector2Int a, int b)
    {
        return new GVector2Int(a.x / b, a.y / b);
    }

    public static bool operator ==(GVector2Int a, GVector2Int b)
    {
        return a.x == b.x && a.y == b.y;
    }
    public static bool operator !=(GVector2Int a, GVector2Int b)
    {
        return a.x != b.x || a.y!=b.y;
    }

    public override bool Equals(object obj)
    {
        return obj is GVector2Int && this == (GVector2Int)obj;
    }
    public override int GetHashCode()
    {
        return x.GetHashCode() ^ y.GetHashCode();
    }

    public override string ToString()
    {
        return $"({x},{y})";
    }

    public static GVector2Int zero { get; } = new GVector2Int(0, 0);
    public static GVector2Int one { get; } = new GVector2Int(1, 1);
    public static GVector2Int up { get; } = new GVector2Int(0, 1);
    public static GVector2Int down { get; } = new GVector2Int(0, -1);
    public static GVector2Int right { get; } = new GVector2Int(1, 0);
    public static GVector2Int left { get; } = new GVector2Int(-1, 0);
}