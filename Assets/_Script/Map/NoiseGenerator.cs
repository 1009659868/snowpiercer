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
    public float GetNoiseValue(Vector3 position, NoiseType type)
    {
        float[,,] noiseMap = _mapManager.GetNoiseMap(type);
        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        int z = Mathf.FloorToInt(position.z);

        return (x >= 0 && x < noiseMap.GetLength(0) &&
                y >= 0 && y < noiseMap.GetLength(1) &&
                z >= 0 && z < noiseMap.GetLength(2))
                ? noiseMap[x, y, z]
                : 0f;
    }
    //生成多级噪声,根据noiseType和noiseSettings生成噪声值,3D噪声
    public float[,,] GenerateNoise(NoiseType noiseType, NoiseSettings settings)
    {
        Vector3 mapSize = _mapManager.mapSize;
        int width = Mathf.FloorToInt(mapSize.x);
        int height = Mathf.FloorToInt(mapSize.y);
        int depth = Mathf.FloorToInt(mapSize.z);
        float[,,] noiseMap = new float[width, height, depth];

        Vector2 offset = settings.offset;
        //根据噪声类型调整生成策略
        switch (noiseType)
        {
            case NoiseType.Height_low:
                GenerateHeightNoise(noiseMap, settings, heightLowWeight);
                break;
            case NoiseType.Height_mid:
                GenerateHeightNoise(noiseMap, settings, heightMidWeight);
                break;
            case NoiseType.Height_high:
                GenerateHeightNoise(noiseMap, settings, heightHighWeight);
                break;
            case NoiseType.Moisture:
                GenerateEnvironmentalNoise(noiseMap, settings);
                break;
            case NoiseType.Temperature:
                GenerateEnvironmentalNoise(noiseMap, settings);
                break;
            case NoiseType.Resource:
                GenerateResourceNoise(noiseMap, settings);
                break;
        }
        _mapManager.SetNoiseMap(noiseType, noiseMap);
        return noiseMap;
    }
    //生成高度相关的噪声（多层叠加）
    private void GenerateHeightNoise(float[,,] noiseMap, NoiseSettings settings, float weight)
    {
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);
        int depth = noiseMap.GetLength(2);

        float minNoise = float.MaxValue;
        float maxNoise = float.MinValue;

        Vector2 offset = settings.offset;
        float scale = settings.scale > 0 ? settings.scale : 0.0001f; // 防止 scale 为 0
        int octaves = settings.octaves;
        float persistence = settings.persistance;
        float lacunarity = settings.lacunarity;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                float totalNoise = 0f;
                float amplitude = 1f;
                float frequency = 1f;
                float maxPossibleHeight = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x + offset.x) / scale * frequency;
                    float sampleZ = (z + offset.y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2 - 1; // 使其范围在 [-1,1]
                    totalNoise += perlinValue * amplitude;

                    maxPossibleHeight += amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                totalNoise /= maxPossibleHeight; // 归一化噪声值
                totalNoise = Mathf.Clamp(totalNoise, -1f, 1f);

                for (int y = 0; y < height; y++)
                {
                    // 使用 y 轴进行高度衰减，使其更符合地形
                    float heightFactor = Mathf.InverseLerp(0, height - 1, y);
                    float finalNoise = totalNoise * weight * Mathf.Pow(1f - heightFactor, verticalFalloffStrength);

                    noiseMap[x, y, z] = finalNoise;

                    // 记录最大最小噪声值
                    if (finalNoise > maxNoise) maxNoise = finalNoise;
                    if (finalNoise < minNoise) minNoise = finalNoise;
                }
            }
        }

        // 归一化噪声地图
        NormalizeNoiseMap(noiseMap, maxNoise, minNoise);
    }
    //生成环境参数噪声（湿度和温度）
    private void GenerateEnvironmentalNoise(float[,,] noiseMap, NoiseSettings settings)
    {

    }
    //生成资源分布的噪声（离散化）
    private void GenerateResourceNoise(float[,,] noiseMap, NoiseSettings settings)
    {

    }
    //混合所有噪声层生成最终地形
    public float[,,] GenerateFinalNoiseMap()
    {

        return null;
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
