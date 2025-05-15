using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.PlayerLoop;

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

    public Chunk chunkPrefab;
    public GameObject treePrefab;

    [Header("Generation Settings")]
    [SerializeField] private bool _generateOnStart = true;
    [SerializeField] private float _generationInterval = 0.000000001f;
    // 在MapGenerator类中添加高度控制参数
    [Header("Height Settings")]
    [SerializeField] public float _seaLevel = -2f;      // 海平面Y坐标
    [SerializeField] public float _baseHeight = 0f;    // 基础高度偏移
    [SerializeField] public float _heightScale = 0f;   // 高度缩放系数
    [SerializeField] public int _minWorldY=-3;
    private Vector3 _lastPlayerPosition;
    private Coroutine _generationCoroutine;
    //更新间隔
    private float updateTimer = 0f;
    private const float UPDATE_INTERVAL = 1f; // 1秒间隔
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
        updateTimer += Time.deltaTime;
        if ((playerMoved() || updateTimer >= 1f)&& initedMap)
        {
            UpdateActiveChunks();
            updateTimer = 0f;
        }
    }
    //初始化地图
    public void initMap()
    {
        _lastPlayerPosition = new Vector3(0,0,0);
        GenerateMap();
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
        float unInstallRadius = _chunkLoader.GetUnInstallRadius();
        Vector3 playerPosition = _chunkLoader.GetPlayerPosition();
        return Vector3.Distance(chunkPosition, playerPosition) > unInstallRadius * MyGrid._instance.largerCellSize.x;
    }
    
    //卸载加载范围外的区块
    public IEnumerator UnInstallChunksOutsideLoadArea()
    {
        Debug.Log("---UnloadChunks---");
        List<Vector3> chunksToUnInstall = new List<Vector3>();
        Dictionary<Vector3, Chunk> activeChunks = _chunkLoader.GetActiveChunks();
        // 使用缓存列表避免修改集合时迭代
        
        var keys = new List<Vector3>(activeChunks.Keys);
        // 先收集需要卸载的区块
        for (int i = 0; i < keys.Count; i++)
        {
            var chunk = keys[i];
            if (IsOutOfLoadArea(chunk))
            {
                chunksToUnInstall.Add(chunk);
            }
        }
        // 分帧卸载
        int unloadsPerFrame = 5; // 每帧最多卸载5个区块
        int unloadedCount = 0;

        while (unloadedCount < chunksToUnInstall.Count)
        {
            int endIndex = Mathf.Min(unloadedCount + unloadsPerFrame, chunksToUnInstall.Count);

            for (int i = unloadedCount; i < endIndex; i++)
            {
                _chunkLoader.UnregisterChunk(chunksToUnInstall[i]);
            }

            unloadedCount = endIndex;
            yield return null; // 每帧结束后 yield
        }
        // Debug.Log($"Unloaded {unloadedCount} chunks");
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
        // Debug.Log("Update Chunks");
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
                _lastPlayerPosition
            )));
    }
    public IEnumerator DynamicGeneration()
    {
        while (true)
        {
            // 添加提前退出条件
            if (!Application.isPlaying) yield break;

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
                yield return StartCoroutine(UnInstallChunksOutsideLoadArea());
                // 更新记录位置时保持原始坐标精度
                if(Vector3.Distance(currentPlayerWorldPos,_lastPlayerPosition)>MyGrid._instance.largerCellSize.x*4)
                    _lastPlayerPosition = currentPlayerWorldPos;
                // Debug.Log("in");
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
                if(!_chunkLoader.HasChunk(chunksArray[i])){
                    
                    GenerateSingleChunk(chunksArray[i]);
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
        if(!initedMap) {
            loadDistance*=2;
            initedMap=!initedMap;
        }
        
        for (int x = -loadDistance; x <= loadDistance; x++)
        {
            for (int z=-loadDistance; z <=loadDistance; z++)
            {
                Vector3 chunkPos = MyGrid._instance.LargeGridToWorld(
                    MyGrid._instance.WorldToLargeGrid(
                        centerPosition +
                        new Vector3(
                            x*MyGrid._instance.largerCellSize.x ,
                            0,
                            z*MyGrid._instance.largerCellSize.z )
                    )
                );
                chunkPos=_chunkLoader.GetChunkPosition(chunkPos);
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
        Chunk chunk = _chunkLoader.GetChunk(chunkWorldPosition);
        if(chunk!=null) return;
        // Debug.Log("chunkWorldPosition:"+chunkWorldPosition);
        chunk=(Chunk)Instantiate(chunkPrefab,chunkWorldPosition,Quaternion.identity);
    }


    public BlockType DetermineBlockType(int currentHeight,int surfaceHeight,float temperature,float humidity)
    {
        // 全局参数（这些参数可根据需求配置或通过外部变量传入）
        int seaLevel = (int)_seaLevel;               // 海平面高度，例如4
        int surfaceLayerThickness = 4;          // 表层厚度（单位为方块高度）
        float lowTemperatureThreshold = 0.3f;   // 温度较低的阈值
        float lowHumidityThreshold = 0.3f;      // 湿度较低的阈值

        // 1. 如果当前高度高于地表，则该位置为空气
        if (currentHeight > surfaceHeight)
        {
            return BlockType.Air;
        }

        // 2. 如果地表低于海平面，则说明整个区域处于水下
        
        if (currentHeight <= seaLevel)
        {
            return BlockType.Water;
        }
        
        // 3. 表层：定义为从 (surfaceHeight - surfaceLayerThickness + 1) 到 surfaceHeight 的区间
        if (currentHeight >= surfaceHeight - surfaceLayerThickness*MyGrid._instance.largerCellSize.y )
        {
            // Debug.Log("temperature:"+temperature);
            // 当温度低时生成雪块
            if (-temperature*100 < lowTemperatureThreshold)
            {
                return BlockType.Stone;
            }
            // 当湿度低时生成沙块（干旱区域）
            if (-humidity*100 < lowHumidityThreshold)
            {
                return BlockType.Sand;
            }
            // 默认生成草块
            return BlockType.Grass;
        }
        else
        // 4. 地下层（表层以下）生成石头块
        return BlockType.Stone;
        //确定水方块:通过高度,温度和湿度确定

        //确定草方块:只可以在地表,通过高度,温度确定

        //确定沙方块:可以在地表,水下,地面下岩石上,通过高度和湿度确定

        //确定石头方块:可以在地表和岩石层,通过高度,温度和湿度确定

        // 默认返回空气（如果未匹配任何条件）
        //return blockType.Air;
    } 
    private bool playerMoved()
    {
        if (Vector3.Distance(_chunkLoader.player.position, _lastPlayerPosition) > MyGrid._instance.largerCellSize.x) {
            // _lastPlayerPosition=_chunkLoader.player.position;
            return true;
        }
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






