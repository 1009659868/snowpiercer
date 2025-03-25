// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class MapGenerator_V3 : MonoBehaviour
// {
//     public enum DrawMode { NoiseMap, colorMap };
//     public DrawMode drawMode;
//     [Header("地图参数")]
//     public int mapHeight;
//     public int mapwidth;
//     public float noiseScale;
//     public int seed;
//     public Vector2 offset;
//     [Range(1,16)]public int octaves;
//     [Range(0, 1)]
//     public float persistance;
//     public float lacunarity;

//     public bool autoUpdate;
//     public TerrainType[] regions;
//     public void GenerateMap()
//     {
//         float[,] noiseMap = Noise.GenerateNoiseMap_v2(mapHeight, mapwidth, noiseScale, octaves, persistance, lacunarity, offset, seed);
//         Color[] colorMap = new Color[mapwidth * mapHeight];
//         for (int y = 0; y < mapHeight; y++)
//         {
//             for (int x = 0; x < mapwidth; x++)
//             {
//                 float currentHight = noiseMap[x, y];
//                 for (int i = 0; i < regions.Length; i++)
//                 {
//                     if (currentHight <= regions[i].height)
//                     {
//                         colorMap[y * mapwidth + x] = regions[i].color;
//                         break;
//                     }
//                 }
//             }
//         }
//         MapDisplay display = FindObjectOfType<MapDisplay>();
//         if (drawMode == DrawMode.NoiseMap)
//         {
//             display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
//         }
//         else if (drawMode == DrawMode.colorMap)
//         {
//             display.DrawTexture(TextureGenerator.TextureFromColourMap(colorMap, mapwidth, mapHeight));
//         }
//         Debug.LogError("Over");
//     }
//     // Start is called before the first frame update
//     void OnValidate()
//     {
//         if (mapHeight < 1)
//         {
//             mapHeight = 1;
//         }
//         if (mapwidth < 1)
//         {
//             mapwidth = 1;
//         }
//         if (lacunarity < 1)
//         {
//             lacunarity = 1;
//         }
//         if (octaves < 0)
//         {
//             octaves = 0;
//         }
//     }
// }
// [System.Serializable]
// public struct TerrainType
// {
//     public string name;
//     public float height;
//     public Color color;
// }