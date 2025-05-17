using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimplexNoise;
using Unity.AI.Navigation;
using UnityEngine.AI;
#region Chunk
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(MeshFilter))]
[System.Serializable]
public class Chunk : MonoBehaviour
{
    private MapManager _mapManager => MapManager._instance;
    private NoiseGenerator _noiseGenerator => NoiseGenerator._instance;
    private ChunkLoader _chunkLoader => ChunkLoader._instance;
    private MapGenerator _mapGenerator => MapGenerator._instance;
    public Vector3 size;
    public Mesh mesh;
    public GameObject ChunkObject;
    public Vector3 position;
    public static List<Chunk> chunks = new List<Chunk>();

    public static int chunkWidth = 16;
    public static int chunkHeight = 64;
    public int seed;
    public float baseHeight = 10;
    public float frequency = 0.025f;
    public float amplitude = 1;
    BlockType[,,] map;
    Mesh chunkMesh;
    MeshRenderer meshRenderer;
    MeshCollider meshCollider;
    MeshFilter meshFilter;
    Vector3 offset0;
    Vector3 offset1;
    Vector3 offset2;
    System.Random rand;
    public static Chunk GetChunk(Vector3 wPos)
    {
        for (int i = 0; i < chunks.Count; i++)
        {
            Vector3 tempPos = chunks[i].transform.position;

            //wPos是否超出了Chunk的XZ平面的范围
            if ((wPos.x < tempPos.x) || (wPos.z < tempPos.z) || (wPos.x >= tempPos.x +MyGrid._instance.largerCellSize.x) || (wPos.z >= tempPos.z +MyGrid._instance.largerCellSize.z))
                continue;
            return chunks[i];
        }
        return null;
    }
    
    void Start()
    {
        _chunkLoader.RegisterChunk(this);
        rand=new System.Random(100);
        //初始化地图
        InitMap();
        // var surface = gameObject.AddComponent<NavMeshSurface>();
        // DynamicNavMeshBaker._instance.ConfigureNavMeshSurface(surface);
        // DynamicNavMeshBaker._instance.RequestBake(surface);
    }
    void InitMap()
    {   
        // Debug.Log("InitMap-------");
        Vector3 chunkWorldPosition=transform.position;
        //生成地形包括平原,水洼,高山
        //if (_chunkLoader.HasChunk(chunkWorldPosition)) return;
        // Debug.Log("Has");
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

                // float combinNoise= _noiseGenerator.GetNoiseValue(new Vector3(worldX, 0, worldZ));
                float heightNoise=_noiseGenerator.GetHeightNoise(new Vector3(worldX, 0, worldZ));
                float moistureNoise = _noiseGenerator.GetMoistureNoise(new Vector3(worldX, 0, worldZ));
                float temperatureNoise = _noiseGenerator.GetTemperatureNoise(new Vector3(worldX, 0, worldZ));
                float resourceNoise = _noiseGenerator.GetResourceNoise(new Vector3(worldX, 0,worldZ));
                // Debug.Log("combinNoise:"+combinNoise);
                // Debug.Log("heightNoise:"+heightNoise);
                // 获取地表高度（使用多种噪声混合）
                int surfaceHeight = Mathf.FloorToInt(_mapGenerator._baseHeight + heightNoise * _mapGenerator._heightScale);

                // Debug.Log("surfaceHeight:"+surfaceHeight);
                // 从最低点到最高点遍历垂直方向
                for (int worldY = _mapGenerator._minWorldY; worldY <= surfaceHeight; worldY++)
                {
                    Vector3 blockWorldPos = new Vector3(worldX, worldY-4.5f, worldZ);

                    // 跳过已有方块
                    if (_chunkLoader.HasBlock(blockWorldPos)) continue;

                    //  跳过初始层
                    if(worldY==_chunkLoader.GetPlayerPosition().y) continue;

                    //随机资源
                    

                    // 确定方块类型
                    BlockType type = _mapGenerator.DetermineBlockType(
                        worldY,
                        surfaceHeight,
                        temperatureNoise,
                        moistureNoise
                    );

                    // 注册非空气方块
                    if (type != BlockType.Air)
                    {
                        _chunkLoader.RegisterBlock(
                            this,
                            blockWorldPos,
                            type,
                            GridType.DetailGrid
                        );
                        
                        if(worldY-_chunkLoader.GetPlayerPosition().y>=1){
                            // Debug.Log("ground");
                            Block block = _chunkLoader.GetBlock(MyGrid._instance.DetailGridToWorld(MyGrid._instance.WorldToDetailGrid(blockWorldPos)));
                            block.blockObject.layer =LayerMask.NameToLayer("Ground");
                        }
                        if(worldY==surfaceHeight){
                            Block block = _chunkLoader.GetBlock(MyGrid._instance.DetailGridToWorld(MyGrid._instance.WorldToDetailGrid(blockWorldPos)));
                            RandomResoure(block,block.type,resourceNoise);
                        }
                    }
                }
            }
        }
        
    }
    void RandomResoure(Block block ,BlockType blockType ,float resourceNoise){
        Vector3 pos =block.position;
        
        if(resourceNoise*10<2) return;
        float[,] dir=new float[8,2]{
            {-4,0}, //上
            {4,0},  //下
            {0,-4}, //左
            {0,4},  //右
            {-4,-4},//左上
            {-4,4}, //右下
            {4,-4}, //左下
            {4,4}   //右下
        };
        for(int i=0;i<8;i++){
            var temp=new Vector3(pos.x+dir[i,0],pos.y,pos.z+dir[i,1]);
            Block tempblock = _chunkLoader.GetBlock(temp);
            if(tempblock==null) return;
            if(tempblock.isHaveHarvest) return;
        }
        pos+=new Vector3(0,2,0);
        // Debug.Log(resourceNoise);
        GameObject tree=_mapGenerator.treePrefab;
        GameObject obj=null;
        switch(blockType){
            case BlockType.Dirt:
                obj=Instantiate(tree,pos,Quaternion.identity);
                break;
            case BlockType.Grass:
                obj=Instantiate(tree,pos,Quaternion.identity);
                break;
            default: 
                return;
        }
        block.isHaveHarvest=true;
        obj.transform.parent=_mapManager._resourceHolder;
    }
    int GenerateHeight(Vector3 wPos)
    {

        //让随机种子，振幅，频率，应用于我们的噪音采样结果
        float x0 = (wPos.x + offset0.x) * frequency;
        // float y0 = (wPos.y + offset0.y) * frequency;
        float z0 = (wPos.z + offset0.z) * frequency;

        float x1 = (wPos.x + offset1.x) * frequency * 2;
        // float y1 = (wPos.y + offset1.y) * frequency * 2;
        float z1 = (wPos.z + offset1.z) * frequency * 2;

        float x2 = (wPos.x + offset2.x) * frequency / 4;
        // float y2 = (wPos.y + offset2.y) * frequency / 4;
        float z2 = (wPos.z + offset2.z) * frequency / 4;

        float noise0 = Noise.Generate(x0, z0) * amplitude;
        // float noise1 = Noise.Generate(x1, y1, z1) * amplitude / 2;
        float noise2 = Noise.Generate(x2, z2) * amplitude / 4;

        //在采样结果上，叠加上baseHeight，限制随机生成的高度下限
        return Mathf.FloorToInt(noise0  + noise2 + baseHeight);
    }
    BlockType GenerateBlockType(Vector3 wPos)
    {
        //y坐标是否在Chunk内
        if (wPos.y >= chunkHeight)
        {
            return BlockType.Air;
        }

        //获取当前位置方块随机生成的高度值
        float genHeight = GenerateHeight(wPos);

        //当前方块位置高于随机生成的高度值时，当前方块类型为空
        if (wPos.y > genHeight)
        {
            return BlockType.Air;
        }
        //当前方块位置等于随机生成的高度值时，当前方块类型为草地
        else if (wPos.y == genHeight)
        {
            return BlockType.Grass;
        }
        //当前方块位置小于随机生成的高度值 且 大于 genHeight - 5时，当前方块类型为泥土
        else if (wPos.y < genHeight && wPos.y > genHeight - 5)
        {
            return BlockType.Dirt;
        }
        //其他情况，当前方块类型为碎石
        return BlockType.Stone;
    }
    public void BuildChunk()
    {
        chunkMesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        //遍历chunk, 生成其中的每一个Block
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int z = 0; z < chunkWidth; z++)
                {
                    BuildBlock(x, y, z, verts, uvs, tris);
                }
            }
        }

        chunkMesh.vertices = verts.ToArray();
        chunkMesh.uv = uvs.ToArray();
        chunkMesh.triangles = tris.ToArray();
        chunkMesh.RecalculateBounds();
        chunkMesh.RecalculateNormals();

        meshFilter.mesh = chunkMesh;
        meshCollider.sharedMesh = chunkMesh;
    }
    void BuildBlock(int x, int y, int z, List<Vector3> verts, List<Vector2> uvs, List<int> tris)
    {
        if (map[x, y, z] == 0) return;

        BlockType typeid = map[x, y, z];

        //Left
        if (CheckNeedBuildFace(x - 1, y, z))
            BuildFace(typeid, new Vector3(x, y, z), Vector3.up, Vector3.forward, false, verts, uvs, tris);
        //Right
        if (CheckNeedBuildFace(x + 1, y, z))
            BuildFace(typeid, new Vector3(x + 1, y, z), Vector3.up, Vector3.forward, true, verts, uvs, tris);

        //Bottom
        if (CheckNeedBuildFace(x, y - 1, z))
            BuildFace(typeid, new Vector3(x, y, z), Vector3.forward, Vector3.right, false, verts, uvs, tris);
        //Top
        if (CheckNeedBuildFace(x, y + 1, z))
            BuildFace(typeid, new Vector3(x, y + 1, z), Vector3.forward, Vector3.right, true, verts, uvs, tris);

        //Back
        if (CheckNeedBuildFace(x, y, z - 1))
            BuildFace(typeid, new Vector3(x, y, z), Vector3.up, Vector3.right, true, verts, uvs, tris);
        //Front
        if (CheckNeedBuildFace(x, y, z + 1))
            BuildFace(typeid, new Vector3(x, y, z + 1), Vector3.up, Vector3.right, false, verts, uvs, tris);
    }
    bool CheckNeedBuildFace(int x, int y, int z)
    {
        if (y < 0) return false;
        var type = GetBlockType(x, y, z);
        switch (type)
        {
            case BlockType.Air:
                return true;
            default:
                return false;
        }
    }
    public BlockType GetBlockType(int x, int y, int z)
    {
        if (y < 0 || y > chunkHeight - 1)
        {
            return 0;
        }

        //当前位置是否在Chunk内
        if ((x < 0) || (z < 0) || (x >= chunkWidth) || (z >= chunkWidth))
        {
            var id = GenerateBlockType(new Vector3(x, y, z) + transform.position);
            return id;
        }
        return map[x, y, z];
    }
    void BuildFace(BlockType typeid, Vector3 corner, Vector3 up, Vector3 right, bool reversed, List<Vector3> verts, List<Vector2> uvs, List<int> tris)
    {
        int index = verts.Count;

        verts.Add(corner);
        verts.Add(corner + up);
        verts.Add(corner + up + right);
        verts.Add(corner + right);

        Vector2 uvWidth = new Vector2(0.25f, 0.25f);
        Vector2 uvCorner = new Vector2(0.00f, 0.75f);

        uvCorner.x += (float)(typeid - 1) / 4;
        uvs.Add(uvCorner);
        uvs.Add(new Vector2(uvCorner.x, uvCorner.y + uvWidth.y));
        uvs.Add(new Vector2(uvCorner.x + uvWidth.x, uvCorner.y + uvWidth.y));
        uvs.Add(new Vector2(uvCorner.x + uvWidth.x, uvCorner.y));

        if (reversed)
        {
            tris.Add(index + 0);
            tris.Add(index + 1);
            tris.Add(index + 2);
            tris.Add(index + 2);
            tris.Add(index + 3);
            tris.Add(index + 0);
        }
        else
        {
            tris.Add(index + 1);
            tris.Add(index + 0);
            tris.Add(index + 2);
            tris.Add(index + 3);
            tris.Add(index + 2);
            tris.Add(index + 0);
        }
    }
    public Chunk(Vector3 size, Vector3 position)
    {
        this.size = size;
        this.mesh = new Mesh();
        this.position = position;

        this.ChunkObject = new GameObject("Chunk" + position);
        this.ChunkObject.transform.position = position;
        this.ChunkObject.transform.localScale = size;
        //设置tag=Chunk
        this.ChunkObject.tag = "Chunk";
    }


}
#endregion

#region BlockType
public enum BlockType
{
    Air,
    Dirt ,
    Grass,
    Water,
    Sand,
    Stone ,
    Wood,
    Iron
}
#endregion