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
        Debug.Log("out");
        for (int i = 0; i < noiseLevels.Length; i++)
        {
            if (noiseLevels[i] == null) continue;
            double noise = noiseLevels[i].Noise(x * frequency, y * frequency, z * frequency);
            value += noise * amplitude;
            maxValue += amplitude;

            amplitude *= settings.persistance;
            frequency *= settings.lacunarity;
        }
        Debug.Log("out");

        // 归一化噪声值
        value /= maxValue;

        // 垂直衰减（根据高度 y 调整，比如海拔较高噪声值降低）
        double verticalFactor = 1.0 - Mathf.Abs((float)y) / verticalFalloffStrength;
        verticalFactor = System.Math.Max(verticalFactor, 0.0);

        return (float)(value * verticalFactor);

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
        Debug.Log("in");
        if (_heightNoiseLevels == null)
            InitializeNoiseLayers(_mapManager.GetNoiseSettings(NoiseType.Height_low), out _heightNoiseLevels);
        return GenerateNoiseValue(_heightNoiseLevels,_mapManager.GetNoiseSettings(NoiseType.Height_low),position);
    }
    //获取温度噪声
    public float GetTemperatureNoise(Vector3 position){
        if (_temperatureNoiseLevels == null)
            InitializeNoiseLayers(_mapManager.GetNoiseSettings(NoiseType.Temperature), out _temperatureNoiseLevels);
        return GenerateNoiseValue(_temperatureNoiseLevels,_mapManager.GetNoiseSettings(NoiseType.Temperature),position);
    }
    //获取湿度噪声
    public float GetMoistureNoise(Vector3 position){
        if (_moistureNoiseLevels == null)
            InitializeNoiseLayers(_mapManager.GetNoiseSettings(NoiseType.Moisture), out _moistureNoiseLevels);
        return GenerateNoiseValue(_moistureNoiseLevels,_mapManager.GetNoiseSettings(NoiseType.Moisture),position);
    }
    //获取资源噪声
    public float GetResourceNoise(Vector3 position){
        if (_resourceNoiseLevels == null)
            InitializeNoiseLayers(_mapManager.GetNoiseSettings(NoiseType.Resource), out _resourceNoiseLevels);
        return GenerateNoiseValue(_resourceNoiseLevels,_mapManager.GetNoiseSettings(NoiseType.Resource),position);
    }
    
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
