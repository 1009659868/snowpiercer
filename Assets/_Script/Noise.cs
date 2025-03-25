// using UnityEngine;


// public struct NoiseMap
// {
//     public float nosieHeight;
//     public float noiseWidth;
// }

// public static class Noise 
// {
//     public static float[,] GenerateNoiseMap_v2(int mapHeight,int mapwidth ,float scale, int octaves,float persistance,float lacunarity,Vector2 offset,int seed){
//         float[,] noiseMap=new float[mapwidth,mapHeight];
//         System.Random prng = new System.Random(seed);
//         Vector2[] octaveOffsets = new Vector2[octaves];
//         for(int i = 0;i < octaves;i++){
//             float offsetX = prng.Next(-100000,100000)+offset.x;
//             float offsetY = prng.Next(-100000,100000)-offset.y;
//             octaveOffsets[i] = new Vector2(offsetX,offsetY);
//         }
//         if(scale <= 0){
//             scale = 0.0001f;
//         }
//         float maxNoiseHeight = float.MinValue;
//         float minNoiseHeight = float.MaxValue;

//         float halfWidth = mapwidth / 2f;
//         float halfHeight = mapHeight / 2f;

//         for(int y = 0;y < mapHeight;y++){
//             for(int x = 0;x < mapwidth;x++){
//                 float amplitude = 1;
//                 float frequency = 1;
//                 float noiseHeight = 0;
//                 for(int i = 0;i < octaves;i++){
//                     float xCoord = (x-halfWidth) / scale * frequency + octaveOffsets[i].x;
//                     float yCoord = (y-halfHeight) / scale * frequency + octaveOffsets[i].y;
//                     float perlinValue = Mathf.PerlinNoise(xCoord,yCoord)*2-1;
//                     noiseHeight+=perlinValue*amplitude;
//                     amplitude *= persistance;
//                     frequency *= lacunarity;
//                 }
                
                
//                 if(noiseHeight > maxNoiseHeight){
//                     maxNoiseHeight = noiseHeight;
//                 }else if(noiseHeight < minNoiseHeight){
//                     minNoiseHeight = noiseHeight;
//                 }
//                 noiseMap[x,y] = noiseHeight;
                
//             }
//         }
//         //归一化
//         for(int y = 0;y < mapHeight;y++){
//             for(int x = 0;x < mapwidth;x++){
//                 noiseMap[x,y] = Mathf.InverseLerp(minNoiseHeight,maxNoiseHeight,noiseMap[x,y]);
//             }
//         }


//         return noiseMap;
//     }

//     //octaves:度
//     //persistance:持续性,控制振幅
//     //lacunarity:空隙度,控制频率
//     //scale:缩放
//     public static NoiseMap[,,] GenerateNoiseMap(Vector3 mapSize,float scale ,float amplitude ,float frequency, int octaves,float persistance,float lacunarity){
//         NoiseMap[,,] noiseMap = new NoiseMap[(int)mapSize.x,(int)mapSize.y,(int)mapSize.z];
//         //防止scale为0
//         if(scale <= 0){
//             scale = 0.0001f;
//         }
//         float maxNoiseHeight = float.MinValue;
//         float minNoiseHeight = float.MaxValue;
//         float xRandom = Random.Range(0f,100f);
//         float zRandom = Random.Range(0f,100f);
//         float yRandom = Random.Range(0f,100f);
//         float wRandom = Random.Range(0f,100f);
//         for(int x = 0;x < mapSize.x;x++){
//             for(int z = 0;z < mapSize.z;z++){
//                 for(int y=0; y< mapSize.y;y++){
//                     float xFloat =x ;
//                     float zFloat =z ;
//                     float yFloat =y ;
//                     float wFloat = Mathf.Sqrt(Mathf.Pow(xFloat,2)+Mathf.Pow(zFloat,2));
//                     float xSizeFloat = mapSize.x;
//                     float zSizeFloat = mapSize.z;
//                     float ySizeFloat = mapSize.y;
//                     float wSizeFloat = Mathf.Sqrt(Mathf.Pow(xSizeFloat,2)+Mathf.Pow(zSizeFloat,2));
//                     float noiseHeight = Mathf.PerlinNoise(xFloat/xSizeFloat*frequency+xRandom,zFloat/zSizeFloat*frequency+zRandom) * scale;
//                     float noiseWidth = Mathf.PerlinNoise(yFloat/ySizeFloat*frequency+yRandom,wFloat/ wSizeFloat*frequency+wRandom) * scale;
//                     noiseMap[x,y,z].nosieHeight = noiseHeight;
//                     noiseMap[x,y,z].noiseWidth = noiseWidth;
//                     Debug.Log("x:"+x+",y:"+y+",z:"+z+"--"+noiseHeight+" And "+noiseWidth);
//                 }
                
//             }
//         }
//         //归一化
//         // for(int z = 0;z < mapSize.y;z++){
//         //     for(int x = 0;x < mapSize.x;x++){
//         //         noiseMap[x,z] = Mathf.InverseLerp(minNoiseHeight,maxNoiseHeight,noiseMap[x,z]);
//         //     }
//         // }

//         return noiseMap;
//     }
// }
