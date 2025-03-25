using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
#region 需完成
//完成两个功能,存储Prefab和加载地图块
//额外性能优化:
//      1.合并物体网格,减少碰撞检测;
//      2.内部面不渲染,只渲染可见的面!!!
#endregion
public class ChunkLoader : MonoBehaviour
{
    public static ChunkLoader _instance;
    public Transform _mapHolder;
    public Transform _boundaryHolder;
    public Transform _chunkHolder;
    [Header("Blocks")]
    [SerializeField]private Block[] blocks;
    private GameObjectPool blockPool;
    private Dictionary<blockType,Block> _prefabMap = new Dictionary<blockType, Block>();
    private Dictionary<Vector3,Block>_activeBlocks = new Dictionary<Vector3, Block>();
    private Dictionary<Vector3, Chunk> _activeChunks = new Dictionary<Vector3, Chunk>();
    private Dictionary<Vector3, List<Vector3>> _chunkBlockMap = new Dictionary<Vector3, List<Vector3>>();
    private Dictionary<Vector3,GameObject> _activeBoundary = new Dictionary<Vector3, GameObject>();
    [Header("Load area")]
    public Transform player;
    public int loadDistance = 10;//加载距离(半径,大网格系统,区块)
    public int unloadDistance = 15;//卸载距离(半径,大网格系统,区块)

    void Awake()
    {
        _instance = this;
        blockPool=new GameObjectPool(transform);

        //预热对象池
        foreach (var block in blocks)
        {
            //预先生成400个备用
            
            _prefabMap.Add(block.type,block);
            if(block.HasVisual())
                blockPool.Prewarm(block.type,block.blockPrefab,400);
        }
        Debug.Log("prewarm over");
    }
    //清空地图
    public void ClearAll(){
        // 清空所有地图块
        foreach (var block in _activeBlocks.Values)
        {
            if (block.blockObject != null)
            {
                Destroy(block.blockObject); // 删除游戏对象
            }
        }
        _activeBlocks.Clear(); // 清空字典

        // 清空所有区块
        _activeChunks.Clear();
        _chunkBlockMap.Clear();

        Debug.Log("地图已清空！");
    }
    #region 区块管理
    //检测当前Chunk周围一chunk的范围,
    //如果其他chunk存在则什么都不做
    //如果不存在其他Chunk,则判断Chunk位置+一chunk位置的位置为地图边界
    //如果在Chunk生成时的位置与某一Boundary重合,那么销毁这个Boundary,再重新生成边界
    //当ChunkLoadDetector检测到这个地图边界时,则生成地图
    //所以此时需要生成一个边界GameObject,并为其添加tag=boundary
    private void RegisterBoundary(Vector3 chunkPosition){
        // 检查当前区块周围的四个方向（前后左右）
        Vector3[] neighborOffsets = new Vector3[] {
            new Vector3(MyGrid._instance.largerCellSize.x, 0, 0), new Vector3(-MyGrid._instance.largerCellSize.x, 0, 0),  // 左右
            new Vector3(0, 0, MyGrid._instance.largerCellSize.z), new Vector3(0, 0, -MyGrid._instance.largerCellSize.z),  // 前后
        };
        if (HasBoundary(chunkPosition)) 
        {
            DestroyBoundary(chunkPosition);
        }
        foreach (var offset in neighborOffsets)
        {
            Vector3 neighborPosition = chunkPosition + offset;
            if(HasChunk(neighborPosition))
                if(HasBoundary(neighborPosition))
                    DestroyBoundary(neighborPosition);

            if (!HasChunk(neighborPosition)) // 如果周围某个位置有其他区块
                if(!HasBoundary(neighborPosition))
                    CreateBoundary(neighborPosition);  // 创建新的边界                  
        }
        
    }
    // 创建边界
    private void CreateBoundary(Vector3 position)
    {
        GameObject boundary = new GameObject("Boundary_" + position);
        boundary.transform.position = position;
        boundary.transform.localScale= new Vector3(MyGrid._instance.largerCellSize.x,MyGrid._instance.largerCellSize.y,MyGrid._instance.largerCellSize.z);
        boundary.tag = "Boundary";  // 设置标签为 Boundary
        boundary.transform.SetParent(_boundaryHolder);
        BoxCollider collider = boundary.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        // 将边界添加到字典
        _activeBoundary.Add(position, boundary);

        // 可根据需求添加其他边界表现（例如加上模型、颜色等）
        Debug.Log("Boundary created at position: " + position);
    }
    private void DestroyBoundary(Vector3 position){
        if(_activeBoundary.ContainsKey(position)){
            Destroy(_activeBoundary[position]);
            _activeBoundary.Remove(position);
            Debug.Log("DestroyBoundary:"+position);
        }
    }
    // 注册区块
    public void RegisterChunk(Vector3 chunkPosition)
    {
        if (_activeChunks.ContainsKey(chunkPosition))
        {
            Debug.LogError("Chunk already exists at position: " + chunkPosition);
            return;
        }
        if(chunkPosition.y==player.transform.position.y)
        RegisterBoundary(chunkPosition);

        Chunk newChunk = new Chunk(new Vector3(MyGrid._instance.largerCellSize.x,MyGrid._instance.largerCellSize.y,MyGrid._instance.largerCellSize.z),chunkPosition);
        newChunk.ChunkObject.transform.SetParent(_chunkHolder);
        _activeChunks.Add(chunkPosition, newChunk);
        _chunkBlockMap[chunkPosition] = new List<Vector3>();
    }
    // 卸载区块
    public void UnregisterChunk(Vector3 chunkPosition)
    {
        if (!_activeChunks.TryGetValue(chunkPosition, out Chunk chunk))
        {
            Debug.LogError("No chunk found at position: " + chunkPosition);
            return;
        }

        if (_chunkBlockMap.TryGetValue(chunkPosition, out List<Vector3> blockPositions))
        {
            //批量移除_activeBlocks记录
            foreach (var blockPos in blockPositions.ToList())
            {
                UnregisterBlock(blockPos);
            }
            _chunkBlockMap.Remove(chunkPosition);
        }
        _activeChunks.Remove(chunkPosition);

        if(chunk.ChunkObject!=null){
            GameObject.Destroy(chunk.ChunkObject);
        }
        
    }
    // 获取区块
    public Chunk GetChunk(Vector3 chunkPosition)
    {
        if (!_activeChunks.TryGetValue(chunkPosition, out Chunk chunk))
        {
            Debug.LogError("No chunk found at position: " + chunkPosition);
            return new Chunk();
        }
        return chunk;
    }
    //获取已注册区块
    public Dictionary<Vector3, Chunk> GetActiveChunks(){
        if(_activeChunks==null || _activeChunks.Count==0){ 
            Debug.LogError("No active Chunk found");
            return new Dictionary<Vector3, Chunk>();
        }
            
        return _activeChunks;
    }
    // 获取区块位置（根据地块位置计算所属区块）
    private Vector3 GetChunkPosition(Vector3 blockPosition)
    {
        int chunkX = Mathf.FloorToInt(blockPosition.x / MyGrid._instance.largerCellSize.x) * (int)MyGrid._instance.largerCellSize.x;
        int chunkY = Mathf.FloorToInt(blockPosition.y / MyGrid._instance.largerCellSize.y) * (int)MyGrid._instance.largerCellSize.y;
        int chunkZ = Mathf.FloorToInt(blockPosition.z / MyGrid._instance.largerCellSize.z) * (int)MyGrid._instance.largerCellSize.z;
        return new Vector3(chunkX, chunkY, chunkZ);
    }
    //判断某位置是否存在区块
    public bool HasChunk(Vector3 chunkPosition){
        if(_activeChunks==null || _activeChunks.Count==0){
            Debug.LogError("No active Chunk found");
            return false;
        }
        return _activeChunks.ContainsKey(chunkPosition);
    }
    //判断Chunk生成位置是否存在边界,如存在则销毁
    private bool HasBoundary(Vector3 chunkPosition){
        if(_activeBoundary==null || _activeBoundary.Count==0){
            Debug.LogError("No active Boundary");
            return false;
        }
        return _activeBoundary.ContainsKey(chunkPosition);
    }
    #endregion
    #region 方块管理
    //获取activeBlocks
    public Dictionary<Vector3,Block> GetActiveBlocks(){
        if(_activeBlocks==null || _activeBlocks.Count==0){ 
            Debug.LogError("No active blocks found");
            return new Dictionary<Vector3, Block>();
        }
            
        return _activeBlocks;
    }
    //动态注册加载地图块
    public void RegisterBlock(Vector3 position,blockType type,GridType gridType){
        Vector3 worldPosition = MyGrid._instance.DetailGridToWorld(MyGrid._instance.WorldToDetailGrid(position));
        if(_activeBlocks.ContainsKey(worldPosition)){
            Debug.LogError("block already exists at position: "+position);
            return;
        }
        if(!_prefabMap.TryGetValue(type,out Block blockPrefab)){
            Debug.LogError("No prefab found for block type: "+type);
            return;
        }
        Vector3 chunkPos = GetChunkPosition(worldPosition);
        //如果chunk为空则,注册区块
        RegisterChunk(chunkPos);
        if (_chunkBlockMap.ContainsKey(chunkPos))
        {
            _chunkBlockMap[chunkPos].Add(worldPosition);
        }
        Block prefab =_prefabMap[type];
        
        var newBlock = new Block(worldPosition,prefab.size,prefab.blockPrefab,null,type,prefab.isDestroyable,prefab.isWalkable,prefab.isBuildable,prefab.isHarvestable);
        if(newBlock.HasVisual()){
            GameObject blockObject= LoadBlock(newBlock);
            newBlock.blockObject = blockObject;
            AdaptGrid(blockObject,gridType);
            blockObject.transform.SetParent(GetChunk(chunkPos).ChunkObject.transform);
        }
        _activeBlocks.Add(worldPosition,newBlock);
    }
    //适应网格大小
    private void AdaptGrid(GameObject blockObject,GridType gridType){
        switch(gridType){
            case GridType.DetailGrid:
                blockObject.transform.localScale = new Vector3(MyGrid._instance.detailCellSize.x,MyGrid._instance.detailCellSize.y,MyGrid._instance.detailCellSize.z);
                break;
            case GridType.LargeGrid:
                blockObject.transform.localScale = new Vector3(MyGrid._instance.largerCellSize.x,MyGrid._instance.largerCellSize.y,MyGrid._instance.largerCellSize.z);
                break;
        }
        
    }
    //加载地图块
    private GameObject LoadBlock(Block block){

        // return Instantiate(block.blockPrefab,block.position,Quaternion.identity,transform);
        return blockPool.Get(block.type,block.blockPrefab,block.position,Quaternion.identity,transform);
    }
    //卸载地图块
    public void UnregisterBlock(Vector3 position){
        if(!_activeBlocks.TryGetValue(position, out Block block)){
            Debug.LogError("No block found at position: "+position);
            return;
        }
        if(block.blockObject != null)
        {
            //将对象返还到对象池
            blockPool.Return(block.type, block.blockObject);
        }
        
        _activeBlocks.Remove(position);
        Vector3 chunkPos = GetChunkPosition(position);
        if (_chunkBlockMap.ContainsKey(chunkPos))
        {
            _chunkBlockMap[chunkPos].Remove(position);
        }
        Debug.Log("UnregisterBlock success! :"+position);
    }
    //获取地图块
    public Block GetBlock(Vector3 position){
        if(!_activeBlocks.TryGetValue(position,out Block block)){
            Debug.LogError("No block found at position: "+position);
            return new Block();
        }
        return block;
    }
    //获取所有地图块
    public Dictionary<Vector3,Block> GetAllBlocks(){
        return _activeBlocks;
    }
    //获取地图块类型
    public blockType GetBlockType(Vector3 position){
        if(!_activeBlocks.TryGetValue(position,out Block block)){
            Debug.LogError("No block found at position: "+position);
            return blockType.Grass;
        }
        return block.type;
    }
    //获取地图块是否可破坏
    public bool IsBlockDestroyable(Vector3 position){
        if(!_activeBlocks.TryGetValue(position,out Block block)){
            Debug.LogError("No block found at position: "+position);
            return false;
        }
        return block.isDestroyable;
    }
    //获取地图块是否可行走
    public bool IsBlockWalkable(Vector3 position){
        if(!_activeBlocks.TryGetValue(position,out Block block)){
            Debug.LogError("No block found at position: "+position);
            return false;
        }
        return block.isWalkable;
    }
    //获取地图块是否可建造
    public bool IsBlockBuildable(Vector3 position){
        if(!_activeBlocks.TryGetValue(position,out Block block)){
            Debug.LogError("No block found at position: "+position);
            return false;
        }
        return block.isBuildable;
    }
    //获取地图块是否可采集
    public bool IsBlockHarvestable(Vector3 position){
        if(!_activeBlocks.TryGetValue(position,out Block block)){
            Debug.LogError("No block found at position: "+position);
            return false;
        }
        return block.isHarvestable;
    }
    //获取地图块大小
    public Vector3 GetBlockSize(Vector3 position){
        if(!_activeBlocks.TryGetValue(position,out Block block)){
            Debug.LogError("No block found at position: "+position);
            return Vector3.zero;
        }
        return block.size;
    }
    //获取地图块位置
    public Vector3 GetBlockPosition(Vector3 position){
        if(!_activeBlocks.TryGetValue(position,out Block block)){
            Debug.LogError("No block found at position: "+position);
            return Vector3.zero;
        }
        return block.position;
    }
    //获取地图块Prefab
    public GameObject GetBlockPrefab(Vector3 position){
        if(!_activeBlocks.TryGetValue(position,out Block block)){
            Debug.LogError("No block found at position: "+position);
            return null;
        }
        return block.type == blockType.Air ? null : block.blockPrefab;
    }
    #endregion
    public int GetLoadRadius(){
        return loadDistance;
    }
    public int GetUnloadRadius(){
        return unloadDistance;
    }
    public Vector3 GetPlayerPosition(){
        return player.position;
    }
    
}
[System.Serializable]
public class Chunk{
    public Vector3 size;
    public Mesh mesh;
    public GameObject ChunkObject;
    public Vector3 position;
    public Chunk(){}
    public Chunk(Vector3 size,Vector3 position)
    {
        this.size = size;
        this.mesh = new Mesh();
        this.position = position;

        this.ChunkObject=new GameObject("Chunk"+position);
        this.ChunkObject.transform.position=position;
        this.ChunkObject.transform.localScale=size;
        //设置tag=Chunk
        this.ChunkObject.tag="Chunk";

        
    }
}
[System.Serializable]
public class Block{
    public Vector3 position{get;set;}
    //地块大小,占地范围
    public Vector3 size;
    public GameObject blockPrefab;
    public GameObject blockObject;
    public blockType type;
    //是否允许破坏
    public bool isDestroyable ;
    public bool isWalkable ;
    public bool isBuildable ;
    public bool isHarvestable ;
    public Block(){}
    public Block(Vector3 position,Vector3 blockSize,GameObject blockPrefab,GameObject blockObject,blockType type,bool isDestroyable,bool isWalkable,bool isBuildable,bool isHarvestable){
        this.position =  MyGrid._instance.DetailGridToWorld(MyGrid._instance.WorldToDetailGrid(position));
        this.size = blockSize;
        this.blockPrefab = blockPrefab;
        this.blockObject = blockObject;
        this.type = type;
        this.isDestroyable = isDestroyable;
        this.isWalkable = isWalkable;
        this.isBuildable = isBuildable;
        this.isHarvestable = isHarvestable;
    }
     public bool HasVisual()
    {
        return type != blockType.Air && blockPrefab != null;
    }

    
}
public enum blockType{
    Air,
    Grass,
    Water,
    Sand,
    Stone,
    Wood,
    Iron
}