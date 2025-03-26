using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkUnloadDetector : MonoBehaviour
{
    public float unloadRadius = 15f;
    private MapGenerator _mapGenerator;
    private ChunkLoader _chunkLoader;
    private Collider _collider;

    private void Start()
    {
        _mapGenerator = MapGenerator._instance;
        _chunkLoader=ChunkLoader._instance;
        _collider = gameObject.AddComponent<SphereCollider>();
        ((SphereCollider)_collider).radius = _chunkLoader.unloadDistance;
        unloadRadius=_chunkLoader.unloadDistance;
        _collider.isTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        
    }
    // 在编辑器中绘制加载范围
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        //Gizmos.DrawWireSphere(transform.position, _chunkLoader.unloadDistance*MyGrid._instance.largerCellSize.x);
    }
}
