using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//完成3个功能,
// 1.生成多级噪声:高度(低频,中频,高频),湿度,温度,资源;
// 2.混合噪声功能;
// 3.根据噪声值判断地图块的类型;

public class NoiseGenerator : MonoBehaviour
{
    public static NoiseGenerator _instance;
    private MapManager _mapManager => MapManager._instance;
    [SerializeField] private bool useXoroshiro = true;

    private ImprovedNoise[] _heightNoiseLevels;
    private ImprovedNoise[] _temperatureNoiseLevels;
    private ImprovedNoise[] _moistureNoiseLevels;
    private ImprovedNoise[] _resourceNoiseLevels;

    // 各噪声层权重配置
    [Header("Noise Weights")]
    [SerializeField] private float heightLowWeight = 0.6f;
    [SerializeField] private float heightMidWeight = 0.3f;
    [SerializeField] private float heightHighWeight = 0.1f;
    [SerializeField] private float moistureWeight = 1f;
    [SerializeField] private float temperatureWeight = 1f;
    [Header("Vertical Settings")]
    [SerializeField] private float verticalFalloffStrength = 5f; // 新增垂直衰减强度参数
    private void Awake()
    {
        _instance = this;
    }
    private void InitializeNoiseLayers(NoiseSettings settings, out ImprovedNoise[] noiseLevels)
    {
        noiseLevels = new ImprovedNoise[settings.octaves];
        System.Random rand = new System.Random(settings.seed);
        for (int i = 0; i < settings.octaves; i++)
        {
            // 可以根据 useXoroshiro 切换不同随机数实现
            if (useXoroshiro)
            {
                noiseLevels[i] = new ImprovedNoise(new System.Random(rand.Next()));
            }
            else
            {
                noiseLevels[i] = new ImprovedNoise(rand);
            }
        }
        // noiseLevels = new ImprovedNoise[settings.amplitudes.Length];
        // System.Random rand = new System.Random(settings.GetHashCode());
        
        // for (int i = 0; i < settings.amplitudes.Length; i++)
        // {
        //     if (settings.amplitudes[i] == 0) continue;
        //     if (useXoroshiro)
        //     {
        //         // Xoroshiro实现需要自定义随机类，此处简化为System.Random
        //         noiseLevels[i] = new ImprovedNoise(new System.Random(rand.Next()));
        //     }
        //     else
        //     {
        //         noiseLevels[i] = new ImprovedNoise(rand);
        //     }
        // }
    }
    private float GenerateNoiseValue(ImprovedNoise[] noiseLevels, NoiseSettings settings, Vector3 pos)
    {
        // 将偏移应用到 XZ 坐标上
        double x = pos.x + settings.offset.x;
        double y = pos.y; // 可根据需要调整 y 坐标，比如加上固定偏移
        double z = pos.z + settings.offset.y;  // 注意：这里使用 NoiseSettings.offset.y 作为 Z 方向偏移

        double value = 0.0;
        double frequency = 1.0 / settings.scale;
        double amplitude = 1.0;
        double maxValue = 0.0;

        for (int i = 0; i < noiseLevels.Length; i++)
        {
            if (noiseLevels[i] == null) continue;
            double noise = noiseLevels[i].Noise(x * frequency, y * frequency, z * frequency);
            value += noise * amplitude;
            maxValue += amplitude;

            amplitude *= settings.persistance;
            frequency *= settings.lacunarity;
        }

        // 归一化噪声值
        value /= maxValue;

        // 垂直衰减（根据高度 y 调整，比如海拔较高噪声值降低）
        double verticalFactor = 1.0 - Mathf.Abs((float)y) / verticalFalloffStrength;
        verticalFactor = System.Math.Max(verticalFactor, 0.0);

        return (float)(value * verticalFactor);

        // double value = 0.0;
        // double inputFactor = System.Math.Pow(2, -settings.firstOctave);
        // double valueFactor = System.Math.Pow(2, settings.amplitudes.Length - 1) / (System.Math.Pow(2, settings.amplitudes.Length) - 1);

        // // 应用全局坐标缩放
        // double x = pos.x / settings.coordinateScale;
        // double y = pos.y / settings.heightScale;
        // double z = pos.z / settings.coordinateScale;

        // for (int i = 0; i < noiseLevels.Length; i++)
        // {
        //     if (noiseLevels[i] == null) continue;
            
        //     double scaledX = x * inputFactor;
        //     double scaledY = y * inputFactor;
        //     double scaledZ = z * inputFactor;
            
        //     // 添加高频扰动
        //     if (i > 2) {
        //         scaledX += noiseLevels[0].Noise(scaledX, scaledY, scaledZ) * 0.2;
        //         scaledZ += noiseLevels[1].Noise(scaledX, scaledY, scaledZ) * 0.2;
        //     }

        //     double noise = noiseLevels[i].Noise(scaledX, scaledY, scaledZ);
        //     value += settings.amplitudes[i] * noise * valueFactor;
            
        //     inputFactor *= 2.0;
        //     valueFactor /= 2.0;
        // }

        // // 添加垂直衰减
        // double verticalFactor = 1.0 - System.Math.Abs(y) / 64.0;
        // verticalFactor = System.Math.Max(verticalFactor, 0.0);
        // return (float)(value * verticalFactor);
    }
    public float GetNoiseValue(Vector3 position)
    {
        // 初始化噪声层
        // 初始化各噪声层
        if (_heightNoiseLevels == null)
            InitializeNoiseLayers(_mapManager.GetNoiseSettings(NoiseType.Height_low), out _heightNoiseLevels);
        if (_moistureNoiseLevels == null)
            InitializeNoiseLayers(_mapManager.GetNoiseSettings(NoiseType.Moisture), out _moistureNoiseLevels);
        if (_temperatureNoiseLevels == null)
            InitializeNoiseLayers(_mapManager.GetNoiseSettings(NoiseType.Temperature), out _temperatureNoiseLevels);
        if (_resourceNoiseLevels == null)
            InitializeNoiseLayers(_mapManager.GetNoiseSettings(NoiseType.Resource), out _resourceNoiseLevels);
        // 根据类型获取不同噪声
        // 混合噪声
        // 获取各项噪声值
        float heightNoise = GetHeightNoise(position);
        float moistureNoise = GetMoistureNoise(position);
        float temperatureNoise = GetTemperatureNoise(position);
        // 资源噪声可以单独使用，也可以和其他噪声结合，这里示例单独返回
        float resourceNoise = GetResourceNoise(position);

        // 混合噪声（这里简单采用加权求和方式，后续可根据需求调整混合算法）
        float combined = heightNoise * (heightLowWeight + heightMidWeight + heightHighWeight)
                           + moistureNoise * moistureWeight
                           + temperatureNoise * temperatureWeight;
        return combined;
    }
    //获取高度噪声
    public float GetHeightNoise(Vector3 position){
        return GenerateNoiseValue(_heightNoiseLevels,_mapManager.GetNoiseSettings(NoiseType.Height_low),position);
    }
    //获取温度噪声
    public float GetTemperatureNoise(Vector3 position){
        return GenerateNoiseValue(_temperatureNoiseLevels,_mapManager.GetNoiseSettings(NoiseType.Temperature),position);
    }
    //获取湿度噪声
    public float GetMoistureNoise(Vector3 position){
        return GenerateNoiseValue(_moistureNoiseLevels,_mapManager.GetNoiseSettings(NoiseType.Moisture),position);
    }
    //获取资源噪声
    public float GetResourceNoise(Vector3 position){
        return GenerateNoiseValue(_resourceNoiseLevels,_mapManager.GetNoiseSettings(NoiseType.Resource),position);
    }
    


    // public float GetNoiseValue(Vector3 position, NoiseType type)
    // {
    //     float[,,] noiseMap = _mapManager.GetNoiseMap(type);
    //     int x = Mathf.FloorToInt(position.x);
    //     int y = Mathf.FloorToInt(position.y);
    //     int z = Mathf.FloorToInt(position.z);

    //     return (x >= 0 && x < noiseMap.GetLength(0) &&
    //             y >= 0 && y < noiseMap.GetLength(1) &&
    //             z >= 0 && z < noiseMap.GetLength(2))
    //             ? noiseMap[x, y, z]
    //             : 0f;
    // }
    //生成多级噪声,根据noiseType和noiseSettings生成噪声值,3D噪声
    // public float[,,] GenerateNoise(NoiseType noiseType, NoiseSettings settings)
    // {
    //     Vector3 mapSize = _mapManager.mapSize;
    //     int width = Mathf.FloorToInt(mapSize.x);
    //     int height = Mathf.FloorToInt(mapSize.y);
    //     int depth = Mathf.FloorToInt(mapSize.z);
    //     float[,,] noiseMap = new float[width, height, depth];

    //     Vector2 offset = settings.offset;
    //     //根据噪声类型调整生成策略
    //     switch (noiseType)
    //     {
    //         case NoiseType.Height_low:
    //             GenerateHeightNoise(noiseMap, settings, heightLowWeight);
    //             break;
    //         case NoiseType.Height_mid:
    //             GenerateHeightNoise(noiseMap, settings, heightMidWeight);
    //             break;
    //         case NoiseType.Height_high:
    //             GenerateHeightNoise(noiseMap, settings, heightHighWeight);
    //             break;
    //         case NoiseType.Moisture:
    //             GenerateEnvironmentalNoise(noiseMap, settings);
    //             break;
    //         case NoiseType.Temperature:
    //             GenerateEnvironmentalNoise(noiseMap, settings);
    //             break;
    //         case NoiseType.Resource:
    //             GenerateResourceNoise(noiseMap, settings);
    //             break;
    //     }
    //     _mapManager.SetNoiseMap(noiseType, noiseMap);
    //     return noiseMap;
    // }
    // //生成高度相关的噪声（多层叠加）
    // private void GenerateHeightNoise(float[,,] noiseMap, NoiseSettings settings, float weight)
    // {
    //     int width = noiseMap.GetLength(0);
    //     int height = noiseMap.GetLength(1);
    //     int depth = noiseMap.GetLength(2);

    //     float minNoise = float.MaxValue;
    //     float maxNoise = float.MinValue;

    //     Vector2 offset = settings.offset;
    //     float scale = settings.scale > 0 ? settings.scale : 0.0001f; // 防止 scale 为 0
    //     int octaves = settings.octaves;
    //     float persistence = settings.persistance;
    //     float lacunarity = settings.lacunarity;

    //     for (int x = 0; x < width; x++)
    //     {
    //         for (int z = 0; z < depth; z++)
    //         {
    //             float totalNoise = 0f;
    //             float amplitude = 1f;
    //             float frequency = 1f;
    //             float maxPossibleHeight = 0f;

    //             for (int i = 0; i < octaves; i++)
    //             {
    //                 float sampleX = (x + offset.x) / scale * frequency;
    //                 float sampleZ = (z + offset.y) / scale * frequency;

    //                 float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2 - 1; // 使其范围在 [-1,1]
    //                 totalNoise += perlinValue * amplitude;

    //                 maxPossibleHeight += amplitude;
    //                 amplitude *= persistence;
    //                 frequency *= lacunarity;
    //             }

    //             totalNoise /= maxPossibleHeight; // 归一化噪声值
    //             totalNoise = Mathf.Clamp(totalNoise, -1f, 1f);

    //             for (int y = 0; y < height; y++)
    //             {
    //                 // 使用 y 轴进行高度衰减，使其更符合地形
    //                 float heightFactor = Mathf.InverseLerp(0, height - 1, y);
    //                 float finalNoise = totalNoise * weight * Mathf.Pow(1f - heightFactor, verticalFalloffStrength);

    //                 noiseMap[x, y, z] = finalNoise;

    //                 // 记录最大最小噪声值
    //                 if (finalNoise > maxNoise) maxNoise = finalNoise;
    //                 if (finalNoise < minNoise) minNoise = finalNoise;
    //             }
    //         }
    //     }

    //     // 归一化噪声地图
    //     NormalizeNoiseMap(noiseMap, maxNoise, minNoise);
    // }
    // //生成环境参数噪声（湿度和温度）
    // private void GenerateEnvironmentalNoise(float[,,] noiseMap, NoiseSettings settings)
    // {

    // }
    // //生成资源分布的噪声（离散化）
    // private void GenerateResourceNoise(float[,,] noiseMap, NoiseSettings settings)
    // {

    // }
    // //混合所有噪声层生成最终地形
    // public float[,,] GenerateFinalNoiseMap()
    // {

    //     return null;
    // }
    private void NormalizeNoiseMap(float[,,] noiseMap, float maxNoise, float minNoise)
    {
        for (int x = 0; x < noiseMap.GetLength(0); x++)
        {
            for (int y = 0; y < noiseMap.GetLength(1); y++)
            {
                for (int z = 0; z < noiseMap.GetLength(2); z++)
                {
                    noiseMap[x, y, z] = Mathf.InverseLerp(minNoise, maxNoise, noiseMap[x, y, z]);
                }
            }
        }
    }
}
