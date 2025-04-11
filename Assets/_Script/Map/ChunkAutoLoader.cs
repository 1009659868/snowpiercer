using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Chunk))]
public class ChunkAutoLoader : MonoBehaviour
{
    private Transform _player;
    private float _unloadDistance;
    private float _loadDistance;
    private float _checkInterval = 1f; // 检测间隔(秒)
    private float _nextCheckTime;
    private Vector3 _chunkSize;
    void Start()
    {
        // 获取必要引用
        _player = ChunkLoader._instance.GetPlayerTransform();
        _unloadDistance = ChunkLoader._instance.GetUnloadRadius() * MyGrid._instance.largerCellSize.x;
        _loadDistance = ChunkLoader._instance.GetLoadRadius() * MyGrid._instance.largerCellSize.x;
        _chunkSize = MyGrid._instance.largerCellSize;
        
        // 初始检测时间随机化，避免所有区块在同一帧检测
        _nextCheckTime = Time.time + Random.Range(0f, _checkInterval);
    }
     void Update()
    {
        if (Time.time >= _nextCheckTime)
        {
            CheckDistance();
            _nextCheckTime = Time.time + _checkInterval;
        }
    }
    private void CheckDistance()
    {
        if (_player == null) return;
        
        // 计算区块中心到玩家的距离
        float distance = Vector3.Distance(transform.position, _player.position);
        
        // 如果超出卸载距离，自我卸载
        if (distance > _unloadDistance)
        {
            Dictionary<Vector3, Chunk> activeChunks = ChunkLoader._instance.GetActiveChunks();
            if (activeChunks.TryGetValue(transform.position, out Chunk chunk))
            {
                chunk.ChunkObject.SetActive(false);
            }
        }
    }
    
}
