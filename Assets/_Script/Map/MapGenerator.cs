using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

// MapGenerator.cs
//完成x个功能,
// 1.生成随机地图,利用ChunkLoader注册地图块,从MapManager获取地图大小,获取噪声值,生成地图块;
// 2.生成地图块,利用ChunkLoader加载地图块;
// 3.生成地图块的时候,根据NoiseGenerator噪声值判断地图块的类型;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator _instance;

    private MapManager _mapManager => MapManager._instance;
    private NoiseGenerator _noiseGenerator => NoiseGenerator._instance;
    private ChunkLoader _chunkLoader => ChunkLoader._instance;

    [Header("Generation Settings")]
    [SerializeField] private bool _generateOnStart = true;
    [SerializeField] private float _generationInterval = 0.000000001f;
    // 在MapGenerator类中添加高度控制参数
    [Header("Height Settings")]
    [SerializeField] private float _seaLevel = -2f;      // 海平面Y坐标
    [SerializeField] private float _baseHeight = 0f;    // 基础高度偏移
    [SerializeField] private float _heightScale = 1000f;   // 高度缩放系数
    [SerializeField] private int _minWorldY=-3;
    private Vector3 _lastPlayerPosition;
    private Coroutine _generationCoroutine;
    public bool initedMap;
    private void Awake()
    {
        _instance = this;
        initedMap = false;
    }
    private void Start()
    {
        //初始化地图
        if (_generateOnStart)
        {
            StartCoroutine(DelayedInit());
        }
    }
    private IEnumerator DelayedInit()
    {
        yield return new WaitForSeconds(0.000000001f); // 延迟初始化
        Debug.Log("init map");
        initMap();
    }
    private void Update()
    {
        if (playerMoved()) {UpdateActiveChunks(); }
        //UpdateActiveChunks();
    }
    //初始化地图
    public void initMap()
    {
        _lastPlayerPosition = _chunkLoader.player.position;
        GenerateMap();
        initedMap = true;
    }


    //重新生成地图
    public void ReGenerateMap()
    {
        //检查地图是否为空
        //不为空,清空地图,然后在生成地图
        if (CheckMap())
        {
            GenerateMap();
        }
    }
    //判断加载范围
    public bool IsInLoadArea(Vector3 chunkPosition)
    {
        float loadRadius = _chunkLoader.GetLoadRadius();
        Vector3 playerPosition = _chunkLoader.GetPlayerPosition();
        return Vector3.Distance(chunkPosition, playerPosition) <= loadRadius * MyGrid._instance.largerCellSize.x;
    }
    public bool IsOutOfLoadArea(Vector3 chunkPosition)
    {
        float unloadRadius = _chunkLoader.GetUnloadRadius();
        Vector3 playerPosition = _chunkLoader.GetPlayerPosition();
        return Vector3.Distance(chunkPosition, playerPosition) > unloadRadius * MyGrid._instance.largerCellSize.x;
    }
    //卸载加载范围外的区块
    public IEnumerator UnloadChunksOutsideLoadArea()
    {
        Debug.Log("---UnloadChunks---");
        List<Vector3> chunksToUnload = new List<Vector3>();
        Dictionary<Vector3, Chunk> activeChunks = _chunkLoader.GetActiveChunks();
        // 使用缓存列表避免修改集合时迭代
        var keys = new List<Vector3>(activeChunks.Keys);
        // 先收集需要卸载的区块
        for (int i = 0; i < keys.Count; i++)
        {
            var chunk = keys[i];
            if (IsOutOfLoadArea(chunk))
            {
                chunksToUnload.Add(chunk);
            }
        }
        // 分帧卸载
        int unloadsPerFrame = 5; // 每帧最多卸载5个区块
        int unloadedCount = 0;

        while (unloadedCount < chunksToUnload.Count)
        {
            int endIndex = Mathf.Min(unloadedCount + unloadsPerFrame, chunksToUnload.Count);

            for (int i = unloadedCount; i < endIndex; i++)
            {
                _chunkLoader.UnregisterChunk(chunksToUnload[i]);
            }

            unloadedCount = endIndex;
            yield return null; // 每帧结束后 yield
        }
        Debug.Log($"Unloaded {unloadedCount} chunks");
    }
    public void LoadMap() => GenerateMap();

    public void UnloadMap()
    {
        //卸载地图
        _chunkLoader.ClearAll();
    }
    public void UpdateActiveChunks()
    {
        //if (!CheckMap()) return;
        Debug.Log("Update Chunks");
        //实现更新区块,包括加载和卸载
        if (_generationCoroutine != null) StopCoroutine(_generationCoroutine);
        _generationCoroutine = StartCoroutine(DynamicGeneration());
    }
    private void GenerateMap()
    {
        //这里的逻辑需要修改,每次地图生成就只生成周围一片区域,然后结束
        //这里原来持续的协程逻辑修改到利用UpdateActiveChunks()实现
        if (!CheckMap()) return;
        Debug.Log("Generating map...");
        if (_generationCoroutine != null) StopCoroutine(_generationCoroutine);
        _generationCoroutine = StartCoroutine(ProcessChunkGeneration(
            GetAlignedGridPosition(
                _chunkLoader.player.position
            )));
    }
    public IEnumerator DynamicGeneration()
    {
        while (true)
        {
            Vector3 currentPlayerWorldPos = _chunkLoader.player.position;
            // 计算XY平面移动距离（忽略Y轴）
            float xzMovement = Vector2.Distance(
                new Vector2(_lastPlayerPosition.x, _lastPlayerPosition.z),
                new Vector2(currentPlayerWorldPos.x, currentPlayerWorldPos.z)
            );
            // 当移动距离超过大网格尺寸时触发加载
            if (xzMovement > MyGrid._instance.largerCellSize.x)
            {
                // 获取对齐后的网格坐标
                Vector3 alignedGridPos = GetAlignedGridPosition(currentPlayerWorldPos);
                // 只生成新进入加载范围的区块
                yield return StartCoroutine(GenerateChunksAroundPosition(alignedGridPos));
                yield return StartCoroutine(UnloadChunksOutsideLoadArea());
                // 更新记录位置时保持原始坐标精度
                if(Vector3.Distance(currentPlayerWorldPos,_lastPlayerPosition)>MyGrid._instance.largerCellSize.x*4)
                _lastPlayerPosition = currentPlayerWorldPos;
            }
            yield return new WaitForSeconds(_generationInterval);
        }
    }
    private IEnumerator GenerateChunksAroundPosition(Vector3 centerPosition)
    {
        // 计算需要生成的新区块
        HashSet<Vector3> chunksToGenerate = CalculateNewChunks(centerPosition);

        const int chunksPerFrame = 4;
        int processed = 0;
        var chunksArray = chunksToGenerate.ToArray();
        System.Array.Sort(chunksArray, (a, b) =>
            Vector3.Distance(a, centerPosition).CompareTo(Vector3.Distance(b, centerPosition)));

        while (processed < chunksArray.Length)
        {
            int endIndex = Mathf.Min(processed + chunksPerFrame, chunksArray.Length);

            for (int i = processed; i < endIndex; i++)
            {
                if (!_chunkLoader.HasChunk(chunksArray[i]))
                {
                    GenerateSingleChunk(chunksArray[i]);
                }
                else
                {
                    Debug.LogError("gen failed");
                }
            }

            processed = endIndex;
            yield return null;
        }
    }
    private HashSet<Vector3> CalculateNewChunks(Vector3 centerPosition)
    {
        HashSet<Vector3> result = new HashSet<Vector3>();
        int loadDistance = _chunkLoader.GetLoadRadius();

        for (int x = -loadDistance; x <= loadDistance; x++)
        {
            for (int z = -loadDistance; z <= loadDistance; z++)
            {
                Vector3 chunkPos = MyGrid._instance.LargeGridToWorld(
                    MyGrid._instance.WorldToLargeGrid(
                        centerPosition +
                        new Vector3(
                            x * MyGrid._instance.largerCellSize.x,
                            0,
                            z * MyGrid._instance.largerCellSize.z)
                    )
                );

                if (!_chunkLoader.HasChunk(chunkPos) &&
                    Vector3.Distance(chunkPos, centerPosition) <= loadDistance * MyGrid._instance.largerCellSize.x)
                {
                    // Debug.Log("Add ...");
                    result.Add(chunkPos);
                }
            }
        }

        return result;
    }
    private Vector3 GetAlignedGridPosition(Vector3 playerWorldPos)
    {
        // 获取对齐后的网格坐标
        return MyGrid._instance.GroundGridToWorld(
            MyGrid._instance.WorldToGroundGrid(playerWorldPos)
        );
    }
    public IEnumerator ProcessChunkGeneration(Vector3 worldPosition)
    {
        yield return StartCoroutine(GenerateChunksAroundPosition(worldPosition));
    }
    // 修改MapGenerator中的GenerateSingleChunk方法
    private void GenerateSingleChunk(Vector3 chunkWorldPosition)
    {
        //生成地形包括平原,水洼,高山
        if (_chunkLoader.HasChunk(chunkWorldPosition)) return;

        // 获取区块边界范围（世界坐标）
        Vector3 chunkStart = chunkWorldPosition - MyGrid._instance.largerCellSize * 0.5f;
        Vector3 chunkEnd = chunkWorldPosition + MyGrid._instance.largerCellSize * 0.5f;

        // 计算区块内小网格的起始和结束坐标
        int startX = Mathf.FloorToInt(chunkStart.x / MyGrid._instance.detailCellSize.x);
        int startZ = Mathf.FloorToInt(chunkStart.z / MyGrid._instance.detailCellSize.z);

        int endX = Mathf.CeilToInt(chunkEnd.x / MyGrid._instance.detailCellSize.x);
        int endZ = Mathf.CeilToInt(chunkEnd.z / MyGrid._instance.detailCellSize.z);

        
        // 三维遍历区块空间
        for (int gridX = startX; gridX <= endX; gridX++)
        {
            for (int gridZ = startZ; gridZ <= endZ; gridZ++)
            {
                // 计算世界XZ坐标
                float worldX = gridX * MyGrid._instance.detailCellSize.x;
                float worldZ = gridZ * MyGrid._instance.detailCellSize.z;

                float combinNoise= _noiseGenerator.GetNoiseValue(new Vector3(worldX, 0, worldZ));
                float heightNoise=_noiseGenerator.GetHeightNoise(new Vector3(worldX, 0, worldZ));
                float moistureNoise = _noiseGenerator.GetMoistureNoise(new Vector3(worldX, 0, worldZ));
                float temperatureNoise = _noiseGenerator.GetTemperatureNoise(new Vector3(worldX, 0, worldZ));
                // Debug.Log("combinNoise:"+combinNoise);
                // Debug.Log("heightNoise:"+heightNoise);
                // 获取地表高度（使用多种噪声混合）
                int surfaceHeight = Mathf.FloorToInt(_baseHeight + heightNoise * _heightScale);

                // Debug.Log("surfaceHeight:"+surfaceHeight);
                // 从最低点到最高点遍历垂直方向
                for (int worldY = _minWorldY; worldY <= surfaceHeight; worldY++)
                {
                    Vector3 blockWorldPos = new Vector3(worldX, worldY-4.5f, worldZ);

                    // 跳过已有方块
                    if (_chunkLoader.HasBlock(blockWorldPos)) continue;

                    //  跳过初始层
                    if(worldY==_chunkLoader.GetPlayerPosition().y) continue;
                    // Debug.Log("worldY:"+worldY);
                    // 确定方块类型
                    blockType type = DetermineBlockType(
                        worldY,
                        surfaceHeight,
                        temperatureNoise,
                        moistureNoise
                    );

                    // 注册非空气方块
                    if (type != blockType.Air)
                    {
                        _chunkLoader.RegisterBlock(
                            blockWorldPos,
                            type,
                            GridType.DetailGrid
                        );
                    }
                }
            }
        }
    }


    private blockType DetermineBlockType(int currentHeight,int surfaceHeight,float temperature,float humidity)
    {
        // 全局参数（这些参数可根据需求配置或通过外部变量传入）
        int seaLevel = (int)_seaLevel;               // 海平面高度，例如4
        int surfaceLayerThickness = 4;          // 表层厚度（单位为方块高度）
        float lowTemperatureThreshold = 0.3f;   // 温度较低的阈值
        float lowHumidityThreshold = 0.3f;      // 湿度较低的阈值

        // 1. 如果当前高度高于地表，则该位置为空气
        if (currentHeight > surfaceHeight)
        {
            return blockType.Air;
        }

        // 2. 如果地表低于海平面，则说明整个区域处于水下
        
        if (currentHeight <= seaLevel)
        {
            return blockType.Water;
        }
        
        // 3. 表层：定义为从 (surfaceHeight - surfaceLayerThickness + 1) 到 surfaceHeight 的区间
        if (currentHeight >= surfaceHeight - surfaceLayerThickness*MyGrid._instance.largerCellSize.y )
        {
            // Debug.Log("temperature:"+temperature);
            // 当温度低时生成雪块
            if (-temperature*100 < lowTemperatureThreshold)
            {
                return blockType.Stone;
            }
            // 当湿度低时生成沙块（干旱区域）
            if (-humidity*100 < lowHumidityThreshold)
            {
                return blockType.Sand;
            }
            // 默认生成草块
            return blockType.Grass;
        }
        else
        // 4. 地下层（表层以下）生成石头块
            return blockType.Stone;
        //确定水方块:通过高度,温度和湿度确定

        //确定草方块:只可以在地表,通过高度,温度确定

        //确定沙方块:可以在地表,水下,地面下岩石上,通过高度和湿度确定

        //确定石头方块:可以在地表和岩石层,通过高度,温度和湿度确定

        // 默认返回空气（如果未匹配任何条件）
        //return blockType.Air;
    }
    // private void GenerateSingleChunk(Vector3 chunkWorldPosition)
    // {
    //     // 检查区块是否已存在
    //     if (_chunkLoader.HasChunk(chunkWorldPosition))
    //     {
    //         return;
    //     }
    //     Vector3 chunkSize = MyGrid._instance.largerCellSize;
    //     int waterLevel = 4;
    //     for (int x = 0; x < chunkSize.x; x++)
    //     {
    //         for (int z = 0; z < chunkSize.z; z++)
    //         {
    //             float worldX = chunkWorldPosition.x + x;
    //             float worldZ = chunkWorldPosition.z + z;

    //             // 生成地形高度
    //             //float noiseValue = Mathf.PerlinNoise(worldX * 0.1f, worldZ * 0.1f);
    //             float noiseValue = _noiseGenerator.GetNoiseValue(new Vector3(worldX * 0.1f, 0, worldZ * 0.1f), NoiseType.Height_mid);
    //             Debug.Log("noiseValue:" + noiseValue);
    //             int groundHeight = Mathf.FloorToInt(noiseValue * 10);
    //             Debug.Log(groundHeight);
    //             Debug.Log(waterLevel);
    //             bool isWater = groundHeight < waterLevel;
    //             // 生成方块
    //             for (int y = 0; y < groundHeight; y++)
    //             {
    //                 Vector3 blockPos = new Vector3(worldX, y - 4.5f, worldZ);
    //                 // 添加方块存在检查
    //                 if (!_chunkLoader.GetActiveBlocks().ContainsKey(
    //                         MyGrid._instance.DetailGridToWorld(MyGrid._instance.WorldToDetailGrid(blockPos))
    //                         )
    //                     )
    //                 {
    //                     blockType type = DetermineBlockType(y, groundHeight, isWater);
    //                     _chunkLoader.RegisterBlock(blockPos, type, GridType.DetailGrid);
    //                 }
    //             }

    //             // 补充水面
    //             if (isWater)
    //             {
    //                 for (int y = groundHeight; y <= waterLevel; y++)
    //                 {
    //                     Vector3 waterPos = new Vector3(worldX, y - 4.5f, worldZ);
    //                     if (!_chunkLoader.GetActiveBlocks().ContainsKey(
    //                             MyGrid._instance.DetailGridToWorld(MyGrid._instance.WorldToDetailGrid(waterPos))
    //                             )
    //                         )
    //                     {
    //                         _chunkLoader.RegisterBlock(waterPos, blockType.Water, GridType.DetailGrid);
    //                     }

    //                 }
    //             }
    //         }
    //     }
    // }


    // private blockType DetermineBlockType(int currentY, int groundHeight, bool isWater)
    // {

    //     if (currentY >= groundHeight - 1)
    //     {
    //         return isWater ? blockType.Water : blockType.Grass;
    //     }
    //     else if (currentY >= groundHeight - 3)
    //     {
    //         return blockType.Sand;
    //     }
    //     else
    //     {
    //         return blockType.Stone;
    //     }
    // }
    private Vector3 FollowPlyaer(Vector3 chunkPos, Vector3 playerPos)
    {
        return new Vector3(
            -(int)_mapManager.mapSize.x / 2 + chunkPos.x + playerPos.x,
            chunkPos.y + playerPos.y - 4.5f,
            -(int)_mapManager.mapSize.z / 2 + chunkPos.z + playerPos.z);
    }
    private bool playerMoved()
    {
        if (Vector3.Distance(_chunkLoader.player.position, _lastPlayerPosition) > MyGrid._instance.largerCellSize.x) return true;
        return false;
    }
    //生成地图前,先判断是否可以生成地图
    //地图生成检查
    private bool CheckMap()
    {
        //检查地图是否需要重新生成
        if (!IsEmptyMap())
        {
            Debug.Log("Unloading existing map...");
            UnloadMap();
        }
        return _mapManager != null && _noiseGenerator != null;
    }
    private bool IsEmptyMap()
    {
        if (_chunkLoader)
        {
            Debug.Log("Active chunks: " + _chunkLoader.GetActiveBlocks().Count);
        }
        return _chunkLoader.GetActiveBlocks().Count == 0;
    }
}






