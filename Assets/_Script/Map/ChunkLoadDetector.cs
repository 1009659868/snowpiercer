using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkLoadDetector : MonoBehaviour
{
    public float loadRadius = 10f;
    private MapGenerator _mapGenerator;
    private ChunkLoader _chunkLoader;
    private Collider _collider;
    
    private void Start()
    {
        _mapGenerator = MapGenerator._instance;
        _chunkLoader=ChunkLoader._instance;
        _collider = gameObject.AddComponent<SphereCollider>();
        ((SphereCollider)_collider).radius = _chunkLoader.loadDistance;
        loadRadius=_chunkLoader.loadDistance;
        _collider.isTrigger = true;
    }
    private void OnTriggerEnter(Collider other) {
        
        if (other.CompareTag("Boundary")&&_mapGenerator.initedMap)
        {
            Debug.Log("---Generate---");
            Vector3 chunkPosition = other.transform.position;
            if (_mapGenerator.IsInLoadArea(chunkPosition))
            {
                _mapGenerator.UpdateActiveChunks();
            }
        }
    }
    // 在编辑器中绘制加载范围
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        //Gizmos.DrawWireSphere(transform.position, _chunkLoader.loadDistance*MyGrid._instance.largerCellSize.x);
    }
}
