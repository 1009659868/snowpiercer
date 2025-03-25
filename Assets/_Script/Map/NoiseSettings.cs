using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class NoiseSettings 
{
    public float scale ;
    public int octaves ;
    [Range(0, 1)]
    public float persistance ;
    public float lacunarity ;
    public int seed;
    public Vector2 offset;
    public float Weight;

    public void ValidateValues()
    {
        scale = Mathf.Max(0.0001f, scale);
        octaves = Mathf.Max(1, octaves);
        persistance = Mathf.Clamp01(persistance);
        lacunarity = Mathf.Max(1, lacunarity);
    }
    public static bool NoiseSettingsEqual(NoiseSettings a, NoiseSettings b){
        return a.scale == b.scale &&
           a.octaves == b.octaves &&
           a.persistance == b.persistance &&
           a.lacunarity == b.lacunarity &&
           a.seed == b.seed &&
           a.offset == b.offset;
    }
}
[System.Serializable]
public struct _NoiseNode
{
    public NoiseType type;
    public NoiseSettings settings;
}
[System.Serializable]
public enum NoiseType{
    //地形:高度(低频,中频,高频),湿度,温度,资源
    //高度(低频):主要用于生成地形的基础高度,如海拔
    //高度(中频):主要用于生成地形的起伏高度,如山脉
    //高度(高频):主要用于生成地形的细节高度,如山脚
    //湿度:主要用于生成地形的湿度,如沼泽,洼地
    //温度:主要用于生成地形的温度,如雪山,沙漠
    //资源:主要用于生成地形的资源,如矿物,植被
    Height_low,
    Height_mid,
    Height_high,
    Moisture,
    Temperature,
    Resource
}