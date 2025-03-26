using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager _instance;
    public GameObject gridCube;
    public Transform mapHolder;
    public const int cellSize = 4;
    [Header("Map Settings")]
    [SerializeField] private Vector3 _mapSize;
    [SerializeField] private Vector3 _groundSize;
    
    [Header("Noise Settings")]
    [SerializeField] private _NoiseNode[] _noises;
    private Dictionary<NoiseType, NoiseSettings> _noiseSettings = new Dictionary<NoiseType, NoiseSettings>();
    private Dictionary<NoiseType, NoiseSettings> _lastNoiseSettings = new Dictionary<NoiseType, NoiseSettings>();

    private Dictionary<NoiseType, float[,,]> _noiseMaps = new Dictionary<NoiseType, float[,,]>();
    // 记录上一次的值用于检测具体变化来源
    private Vector3 _lastMapSize;
    private Vector3 _lastGroundSize;
    private int _lastCellSize;
    void Awake() {
       _instance = this;
    }
    void Start()
    {
        noisesInit();
        foreach (var noise in _noises)
        {
            _noiseSettings.Add(noise.type, noise.settings);
            // NoiseGenerator._instance.GenerateNoise(noise.type, noise.settings);
        }
    }
    
    public NoiseSettings GetNoiseSettings(NoiseType noiseType)
    {
        if (!_noiseSettings.TryGetValue(noiseType, out NoiseSettings noiseSettings))
        {
            //Debug.LogError("No noise settings found for noise type: " + noiseType);
            return new NoiseSettings();
        }
        return noiseSettings;
    }
    public float[,,] GetNoiseMap(NoiseType noiseType)
    {
        if (!_noiseMaps.TryGetValue(noiseType, out float[,,] noiseMap))
        {
            //Debug.LogError("No noise map found for noise type: " + noiseType);
            return new float[0,0,0];
        }
        return noiseMap;
    }
    public void SetNoiseMap(NoiseType noiseType, float[,,] noiseMap)
    {
        if (_noiseMaps.ContainsKey(noiseType))
        {
            _noiseMaps[noiseType] = noiseMap;
        }
        else
        {
            _noiseMaps.Add(noiseType, noiseMap);
        }
        //Debug.Log("Noise map set for noise type: " + noiseType);
    }
    public Vector3 mapSize
    {
        get => _mapSize;
        set
        {
            if (_mapSize == value) return;
            _mapSize = value;
            SyncFromMapToGround();
        }
    }

    public Vector3 groundSize
    {
        get => _groundSize;
        set
        {
            if (_groundSize == value) return;
            _groundSize = value;
            SyncFromGroundToMap();
        }
    }
    // Inspector 中的值发生变化时调用
    private void OnValidate()
    {
        // 检测具体哪个值被修改
        bool mapChanged = (_mapSize != _lastMapSize);
        bool groundChanged = (_groundSize != _lastGroundSize);
        bool cellSizeChanged = (_lastCellSize != cellSize);
        
        if (mapChanged)
        {
            SyncFromMapToGround();
            _lastMapSize = _mapSize;
            _lastGroundSize = _groundSize;
        }
        else if (groundChanged)
        {
            SyncFromGroundToMap();
            _lastMapSize = _mapSize;
            _lastGroundSize = _groundSize;
        }
        if(mapChanged || groundChanged || cellSizeChanged)
        {
            // 修改cube大小=cellSize*groundSize.x*groundSize.y
            gridCube.transform.localScale = new Vector3(cellSize*groundSize.x, cellSize*groundSize.y, cellSize*groundSize.z);
        }
        if(isNoiseChanged()){
            // 清空地图
            ChunkLoader._instance.ClearAll();
            // 更新噪声配置缓存
            _lastNoiseSettings.Clear();
            foreach (var noise in _noises)
            {
                _lastNoiseSettings[noise.type] = noise.settings; // 复制当前的噪声配置
            }
            
        }
        
    }
    private bool isNoiseChanged(){
        bool noiseChanged = false;

        foreach (var noise in _noises)
        {
            if (_lastNoiseSettings.TryGetValue(noise.type, out NoiseSettings lastSettings))
            {
                if (!NoiseSettings.NoiseSettingsEqual(lastSettings, noise.settings))
                {
                    noiseChanged = true;
                    break;
                }
            }
            else
            {
                noiseChanged = true;
                break;
            }
        }
        return noiseChanged;
    }
    private void ClearMap(){

    }
    // 从 mapSize 同步到 groundSize
    private void SyncFromMapToGround()
    {
        _groundSize = new Vector3(_mapSize.x, _groundSize.y, _mapSize.z);
    }

    // 从 groundSize 同步到 mapSize
    private void SyncFromGroundToMap()
    {
        _mapSize = new Vector3(_groundSize.x, _mapSize.y, _groundSize.z);
    }
    private void noisesInit(){
        _noises=new _NoiseNode[]{
            // 基础地形（低频高度）
            new _NoiseNode {
                type = NoiseType.Height_low,
                settings = new NoiseSettings {
                    scale = 150f,
                    octaves = 4,
                    persistance = 0.5f,
                    lacunarity = 2f,
                    seed = 12345,
                    offset = Vector2.zero,
                    Weight = 1.0f
                }
            },
            // 山脉地形（中频高度）
            new _NoiseNode {
                type = NoiseType.Height_mid,
                settings = new NoiseSettings {
                    scale = 80f,
                    octaves = 6,
                    persistance = 0.6f,
                    lacunarity = 2.5f,
                    seed = 54321,
                    offset = Vector2.zero,
                    Weight = 0.7f
                }
            },
            // 岩石细节（高频高度）
            new _NoiseNode {
                type = NoiseType.Height_high,
                settings = new NoiseSettings {
                    scale = 30f,
                    octaves = 8,
                    persistance = 0.65f,
                    lacunarity = 3f,
                    seed = 13579,
                    offset = Vector2.zero,
                    Weight = 0.3f
                }
            },
            // 湿度分布
            new _NoiseNode {
                type = NoiseType.Moisture,
                settings = new NoiseSettings {
                    scale = 200f,
                    octaves = 3,
                    persistance = 0.4f,
                    lacunarity = 1.8f,
                    seed = 24680,
                    offset = Vector2.zero,
                    Weight = 0.9f
                }
            },
            // 温度分布
            new _NoiseNode {
                type = NoiseType.Temperature,
                settings = new NoiseSettings {
                    scale = 180f,
                    octaves = 5,
                    persistance = 0.55f,
                    lacunarity = 2.2f,
                    seed = 11223,
                    offset = Vector2.zero,
                    Weight = 0.8f
                }
            },
            // 资源分布
            new _NoiseNode {
                type = NoiseType.Resource,
                settings = new NoiseSettings {
                    scale = 25f,
                    octaves = 7,
                    persistance = 0.7f,
                    lacunarity = 3.5f,
                    seed = 33445,
                    offset = Vector2.zero,
                    Weight = 1.2f
                }
            }
        };
    }
}
