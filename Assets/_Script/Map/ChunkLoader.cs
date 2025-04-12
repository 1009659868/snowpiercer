using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#region 需完成
//完成两个功能,存储Prefab和加载地图块
//额外性能优化:
//      1.合并物体网格,减少碰撞检测;
//      2.内部面不渲染,只渲染可见的面!!!
#endregion

public class ChunkLoader : MonoBehaviour
{
    public static ChunkLoader _instance;
    public Transform _mapHolder;
    public Transform _boundaryHolder;
    public Transform _chunkHolder;
    [Header("Blocks")]
    [SerializeField] private Block[] blocks;
    private GameObjectPool blockPool;
    private Dictionary<BlockType, Block> _prefabMap = new Dictionary<BlockType, Block>();
    private Dictionary<Vector3, Block> _activeBlocks = new Dictionary<Vector3, Block>();
    private Dictionary<Vector3, Chunk> _activeChunks = new Dictionary<Vector3, Chunk>();
    private Dictionary<Vector3, List<Vector3>> _chunkBlockMap = new Dictionary<Vector3, List<Vector3>>();
    private Dictionary<Vector3, GameObject> _activeBoundary = new Dictionary<Vector3, GameObject>();

    [Header("Load area")]
    public Transform player;
    public int loadDistance = 12;//加载距离(半径,大网格系统,区块)
    public int unloadDistance = 20;//卸载距离(半径,大网格系统,区块)
    public int unInstallDistance=50;//真正的卸载距离

    void Awake()
    {
        _instance = this;
        blockPool = new GameObjectPool(transform);

        //预热对象池
        foreach (var block in blocks)
        {
            //预先生成400个备用
            _prefabMap.Add(block.type, block);
            if (block.HasVisual())
                blockPool.Prewarm(block.type, block.blockPrefab, 100);
        }
        // Debug.Log("prewarm over");
    }
    //清空地图
    public void ClearAll()
    {
        // 清空所有地图块
        foreach (var block in _activeBlocks.Values)
        {
            if (block.blockObject != null)
            {
                Destroy(block.blockObject); // 删除游戏对象
            }
        }
        _activeBlocks.Clear(); // 清空字典

        // 清空所有区块
        _activeChunks.Clear();
        _chunkBlockMap.Clear();

        // Debug.Log("地图已清空！");
    }
    #region 区块管理
    //检测当前Chunk周围一chunk的范围,
    //如果其他chunk存在则什么都不做
    //如果不存在其他Chunk,则判断Chunk位置+一chunk位置的位置为地图边界
    //如果在Chunk生成时的位置与某一Boundary重合,那么销毁这个Boundary,再重新生成边界
    //当ChunkLoadDetector检测到这个地图边界时,则生成地图
    //所以此时需要生成一个边界GameObject,并为其添加tag=boundary
    private void RegisterBoundary(Vector3 chunkPosition)
    {
        // 检查当前区块周围的四个方向（前后左右）
        Vector3[] neighborOffsets = new Vector3[] {
            new Vector3(MyGrid._instance.largerCellSize.x, 0, 0), new Vector3(-MyGrid._instance.largerCellSize.x, 0, 0),  // 左右
            new Vector3(0, 0, MyGrid._instance.largerCellSize.z), new Vector3(0, 0, -MyGrid._instance.largerCellSize.z),  // 前后
        };
        if (HasBoundary(chunkPosition))
        {
            DestroyBoundary(chunkPosition);
        }
        foreach (var offset in neighborOffsets)
        {
            Vector3 neighborPosition = chunkPosition + offset;
            if (HasChunk(neighborPosition))
                if (HasBoundary(neighborPosition))
                    DestroyBoundary(neighborPosition);

            if (!HasChunk(neighborPosition)) // 如果周围某个位置有其他区块
                if (!HasBoundary(neighborPosition))
                    CreateBoundary(neighborPosition);  // 创建新的边界                  
        }

    }
    private void UnRegisterBoundary(Vector3 chunkPosition)
    {

    }
    // 创建边界
    private void CreateBoundary(Vector3 position)
    {
        GameObject boundary = new GameObject("Boundary_" + position);
        boundary.transform.position = position;
        boundary.transform.localScale = new Vector3(MyGrid._instance.largerCellSize.x, MyGrid._instance.largerCellSize.y, MyGrid._instance.largerCellSize.z);
        boundary.tag = "Boundary";  // 设置标签为 Boundary
        boundary.transform.SetParent(_boundaryHolder);
        BoxCollider collider = boundary.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        // 将边界添加到字典
        _activeBoundary.Add(position, boundary);

        // 可根据需求添加其他边界表现（例如加上模型、颜色等）
        //Debug.Log("Boundary created at position: " + position);
    }
    private void DestroyBoundary(Vector3 position)
    {
        if (_activeBoundary.ContainsKey(position))
        {
            Destroy(_activeBoundary[position]);
            _activeBoundary.Remove(position);
            //Debug.Log("DestroyBoundary:" + position);
        }
    }
    // 注册区块
    public void RegisterChunk(Chunk chunk)
    {
        Vector3 chunkPosition=chunk.transform.position;
        if (_activeChunks.ContainsKey(chunkPosition))
        {
            //Debug.LogError("Chunk already exists at position: " + chunkPosition);
            
            return;
        }
        if (chunkPosition.y >= player.transform.position.y - MyGrid._instance.largerCellSize.y)
            RegisterBoundary(chunkPosition);
        
        chunk.transform.SetParent(_chunkHolder);
        // 添加自动加载组件
        // newChunk.ChunkObject.AddComponent<ChunkAutoLoader>();

        _activeChunks.Add(chunkPosition, chunk);
        _chunkBlockMap[chunkPosition] = new List<Vector3>();
    }
    // 卸载区块
    public void UnregisterChunk(Vector3 chunkPosition)
    {
        if (!_activeChunks.TryGetValue(chunkPosition, out Chunk chunk))
        {
            //Debug.LogError("No chunk found at position: " + chunkPosition);
            return;
        }

        if (_chunkBlockMap.TryGetValue(chunkPosition, out List<Vector3> blockPositions))
        {
            //批量移除_activeBlocks记录
            foreach (var blockPos in blockPositions.ToList())
            {
                UnregisterBlock(blockPos);
            }
            _chunkBlockMap.Remove(chunkPosition);
        }
        _activeChunks.Remove(chunkPosition);

        if (chunk.ChunkObject != null)
        {
            GameObject.Destroy(chunk.ChunkObject);
        }

    }
    // 获取区块
    public Chunk GetChunk(Vector3 chunkPosition)
    {
        if (!_activeChunks.TryGetValue(chunkPosition, out Chunk chunk))
        {
            //Debug.LogError("No chunk found at position: " + chunkPosition);
            return null;
        }
        return chunk;
    }
    //获取已注册区块
    public Dictionary<Vector3, Chunk> GetActiveChunks()
    {
        if (_activeChunks == null || _activeChunks.Count == 0)
        {
            //Debug.LogError("No active Chunk found");
            return new Dictionary<Vector3, Chunk>();
        }

        return _activeChunks;
    }
    // 获取区块位置（根据地块位置计算所属区块）
    public Vector3 GetChunkPosition(Vector3 blockPosition)
    {
        int chunkX = Mathf.FloorToInt(blockPosition.x / MyGrid._instance.largerCellSize.x) * (int)MyGrid._instance.largerCellSize.x;
        int chunkY = Mathf.FloorToInt(blockPosition.y / MyGrid._instance.largerCellSize.y) * (int)MyGrid._instance.largerCellSize.y;
        int chunkZ = Mathf.FloorToInt(blockPosition.z / MyGrid._instance.largerCellSize.z) * (int)MyGrid._instance.largerCellSize.z;
        return new Vector3(chunkX, chunkY, chunkZ);
    }
    //判断某位置是否存在区块
    public bool HasChunk(Vector3 chunkPosition)
    {
        if (_activeChunks == null || _activeChunks.Count == 0)
        {
            //Debug.LogError("No active Chunk found");
            return false;
        }
        return _activeChunks.ContainsKey(chunkPosition);
    }
    //判断Chunk生成位置是否存在边界,如存在则销毁
    private bool HasBoundary(Vector3 chunkPosition)
    {
        if (_activeBoundary == null || _activeBoundary.Count == 0)
        {
            //Debug.LogError("No active Boundary");
            return false;
        }
        return _activeBoundary.ContainsKey(chunkPosition);
    }
    #endregion
    #region 方块管理
    //获取activeBlocks
    public Dictionary<Vector3, Block> GetActiveBlocks()
    {
        if (_activeBlocks == null || _activeBlocks.Count == 0)
        {
            //Debug.LogError("No active blocks found");
            return new Dictionary<Vector3, Block>();
        }

        return _activeBlocks;
    }
    //动态注册加载地图块
    public void RegisterBlock(Chunk newChunk, Vector3 position, BlockType type, GridType gridType)
    {
        Vector3 worldPosition = MyGrid._instance.DetailGridToWorld(MyGrid._instance.WorldToDetailGrid(position));
        if (_activeBlocks.ContainsKey(worldPosition))
        {
            // Debug.LogError("block already exists at position: " + position);
            Vector3 registedChunk = GetChunkPosition(worldPosition);
            if (_activeChunks.TryGetValue(registedChunk, out Chunk chunk))
            {
                if(!chunk.ChunkObject.activeSelf){
                    chunk.ChunkObject.SetActive(true);
                    return;
                }    
            }
            return;
        }
        if (!_prefabMap.TryGetValue(type, out Block blockPrefab))
        {
            //Debug.LogError("No prefab found for block type: " + type);
            return;
        }
        Vector3 chunkPos = GetChunkPosition(worldPosition);

        //如果chunk为空则,注册区块
        RegisterChunk(newChunk);
        if (_chunkBlockMap.ContainsKey(chunkPos))
        {
            _chunkBlockMap[chunkPos].Add(worldPosition);
        }
        Block prefab = _prefabMap[type];

        var newBlock = new Block(worldPosition, prefab.size, prefab.blockPrefab, null, type, prefab.isDestroyable, prefab.isWalkable, prefab.isBuildable, prefab.isHarvestable);
        if (newBlock.HasVisual())
        {
            GameObject blockObject = LoadBlock(newBlock);
            newBlock.blockObject = blockObject;
            AdaptGrid(blockObject, gridType);
            blockObject.transform.SetParent(newChunk.transform);
        }

        _activeBlocks.Add(worldPosition, newBlock);
        // 更新相邻方块的面可见性
        UpdateBlockAndNeighborsFaces(worldPosition);
    }
    //卸载地图块
    public void UnregisterBlock(Vector3 position)
    {
        if (!_activeBlocks.TryGetValue(position, out Block block))
        {
            //Debug.LogError("No block found at position: " + position);
            return;
        }
        // 先记录邻居位置
        Vector3[] neighborOffsets = new Vector3[]
        {
            Vector3.up, Vector3.down,
            Vector3.left, Vector3.right,
            Vector3.forward, Vector3.back
        };
        
        List<Vector3> neighborsToUpdate = new List<Vector3>();
        foreach (var offset in neighborOffsets)
        {
            Vector3 neighborPos = position + offset * block.size.x;
            if (_activeBlocks.ContainsKey(neighborPos))
            {
                neighborsToUpdate.Add(neighborPos);
            }
        }

        Vector3 blockSize = block.size;
        if (block.blockObject != null)
        {
            //将对象返还到对象池
            blockPool.Return(block.type, block.blockObject);
        }

        _activeBlocks.Remove(position);
        Vector3 chunkPos = GetChunkPosition(position);
        if (_chunkBlockMap.ContainsKey(chunkPos))
        {
            _chunkBlockMap[chunkPos].Remove(position);
        }

        //Debug.Log("UnregisterBlock success! :" + position);
        // 更新邻居的面
        foreach (var neighborPos in neighborsToUpdate)
        {
            UpdateBlockFaces(neighborPos);
        }

    }
    //适应网格大小
    private void AdaptGrid(GameObject blockObject, GridType gridType)
    {
        switch (gridType)
        {
            case GridType.DetailGrid:
                blockObject.transform.localScale = new Vector3(MyGrid._instance.detailCellSize.x, MyGrid._instance.detailCellSize.y, MyGrid._instance.detailCellSize.z);
                break;
            case GridType.LargeGrid:
                blockObject.transform.localScale = new Vector3(MyGrid._instance.largerCellSize.x, MyGrid._instance.largerCellSize.y, MyGrid._instance.largerCellSize.z);
                break;
        }

    }
    //加载地图块
    private GameObject LoadBlock(Block block)
    {

        // return Instantiate(block.blockPrefab,block.position,Quaternion.identity,transform);
        return blockPool.Get(block.type, block.blockPrefab, block.position, Quaternion.identity, transform);
    }
    
    //获取地图块
    public Block GetBlock(Vector3 position)
    {
        if (!_activeBlocks.TryGetValue(position, out Block block))
        {
            Debug.LogError("No block found at position: " + position);
            return new Block();
        }
        return block;
    }
    public bool HasBlock(Vector3 position)
    {
        if (_activeBlocks == null || _activeBlocks.Count == 0)
        {
            //Debug.LogError("No blocks");
            return false;
        }
        return _activeBlocks.ContainsKey(position);
    }


    //获取所有地图块
    public Dictionary<Vector3, Block> GetAllBlocks()
    {
        return _activeBlocks;
    }
    //获取地图块类型
    public BlockType GetBlockType(Vector3 position)
    {
        if (!_activeBlocks.TryGetValue(position, out Block block))
        {
            //Debug.LogError("No block found at position: " + position);
            return BlockType.Grass;
        }
        return block.type;
    }
    //获取地图块是否可破坏
    public bool IsBlockDestroyable(Vector3 position)
    {
        if (!_activeBlocks.TryGetValue(position, out Block block))
        {
            //Debug.LogError("No block found at position: " + position);
            return false;
        }
        return block.isDestroyable;
    }
    //获取地图块是否可行走
    public bool IsBlockWalkable(Vector3 position)
    {
        if (!_activeBlocks.TryGetValue(position, out Block block))
        {
            //Debug.LogError("No block found at position: " + position);
            return false;
        }
        return block.isWalkable;
    }
    //获取地图块是否可建造
    public bool IsBlockBuildable(Vector3 position)
    {
        if (!_activeBlocks.TryGetValue(position, out Block block))
        {
            //Debug.LogError("No block found at position: " + position);
            return false;
        }
        return block.isBuildable;
    }
    //获取地图块是否可采集
    public bool IsBlockHarvestable(Vector3 position)
    {
        if (!_activeBlocks.TryGetValue(position, out Block block))
        {
            //Debug.LogError("No block found at position: " + position);
            return false;
        }
        return block.isHarvestable;
    }
    //获取地图块大小
    public Vector3 GetBlockSize(Vector3 position)
    {
        if (!_activeBlocks.TryGetValue(position, out Block block))
        {
            //Debug.LogError("No block found at position: " + position);
            return Vector3.zero;
        }
        return block.size;
    }
    //获取地图块位置
    public Vector3 GetBlockPosition(Vector3 position)
    {
        if (!_activeBlocks.TryGetValue(position, out Block block))
        {
            //Debug.LogError("No block found at position: " + position);
            return Vector3.zero;
        }
        return block.position;
    }
    //获取地图块Prefab
    public GameObject GetBlockPrefab(Vector3 position)
    {
        if (!_activeBlocks.TryGetValue(position, out Block block))
        {
            //Debug.LogError("No block found at position: " + position);
            return null;
        }
        return block.type == BlockType.Air ? null : block.blockPrefab;
    }
    #endregion
    public int GetLoadRadius()
    {
        return loadDistance;
    }
    public int GetUnloadRadius()
    {
        return unloadDistance;
    }
    public int GetUnInstallRadius(){
        return unInstallDistance;
    }
    public Vector3 GetPlayerPosition()
    {
        return player.position;
    }
    public Transform GetPlayerTransform(){
        return player;
    }
    // 更新相邻方块的面可见性
    private void UpdateBlockAndNeighborsFaces(Vector3 position)
    {
        UpdateBlockFaces(position);
        
        Block block = _activeBlocks[position];
        Vector3[] neighborOffsets = new Vector3[]
        {
            Vector3.up, Vector3.down,
            Vector3.left, Vector3.right,
            Vector3.forward, Vector3.back
        };
        
        foreach (var offset in neighborOffsets)
        {
            Vector3 neighborPos = position + offset * block.size.x*MyGrid._instance.detailCellSize.x;
            if (_activeBlocks.ContainsKey(neighborPos))
            {
                UpdateBlockFaces(neighborPos);
            }
        }
    }
    private void UpdateBlockFaces(Vector3 position)
    {
        if (!_activeBlocks.TryGetValue(position, out Block block)) return;
        
        block.UpdateFaceVisibility(_activeBlocks);
        block.ApplyFaceVisibility();
    }
}

#region Block
[System.Serializable]
public class Block
{
    public Vector3 position { get; set; }
    //地块大小,占地范围
    public Vector3 size;
    public GameObject blockPrefab;
    public GameObject blockObject;
    public BlockType type;
    //是否允许破坏
    public bool isDestroyable;
    public bool isWalkable;
    public bool isBuildable;
    public bool isHarvestable;
    // 6个面是否可见（上、下、左、右、前、后）
    public bool[] visibleFaces = new bool[6] { true, true, true, true, true, true };
    // 记录每个面被哪些邻居遮挡（用于精确恢复）
    private Dictionary<int, List<Vector3>> _occlusionRecords = new Dictionary<int, List<Vector3>>();
    public Block() { }
    public Block(Vector3 position, Vector3 blockSize, GameObject blockPrefab, GameObject blockObject, BlockType type, bool isDestroyable, bool isWalkable, bool isBuildable, bool isHarvestable)
    {
        this.position = MyGrid._instance.DetailGridToWorld(MyGrid._instance.WorldToDetailGrid(position));
        this.size = blockSize;
        this.blockPrefab = blockPrefab;
        this.blockObject = blockObject;
        this.type = type;
        this.isDestroyable = isDestroyable;
        this.isWalkable = isWalkable;
        this.isBuildable = isBuildable;
        this.isHarvestable = isHarvestable;
    }
    public bool HasVisual()
    {
        return type != BlockType.Air && blockPrefab != null;
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

