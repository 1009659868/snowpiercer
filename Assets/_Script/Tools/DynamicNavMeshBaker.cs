using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using Unity.AI.Navigation;

public class DynamicNavMeshBaker : MonoBehaviour
{
    public static DynamicNavMeshBaker _instance;
    [Header("Baking Settings")]
    [SerializeField] private float _bakeInterval = 0.1f;
    [SerializeField] private int _bakesPerFrame = 2;
    private Dictionary<NavMeshSurface, NavMeshDataInstance> _surfaceToInstanceMap = new Dictionary<NavMeshSurface, NavMeshDataInstance>();
    private Queue<NavMeshSurface> _surfacesToBake = new Queue<NavMeshSurface>();
    
    private bool _isProcessing;
    private void Awake()
    {
        if(_instance==null){
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else{
            // Destroy(gameObject);
        }
    }
    public void RequestBake(NavMeshSurface surface){
        if(surface == null) return;

        _surfacesToBake.Enqueue(surface);

        if(!_isProcessing){
            StartCoroutine(ProcessBaking());
        }
    }
    private IEnumerator ProcessBaking(){
        _isProcessing = true;
        
        while(_surfacesToBake.Count>0){
            int processed =0;
            lock(_surfacesToBake){
                while(processed < _bakesPerFrame && _surfacesToBake.Count > 0){
                    NavMeshSurface surface = _surfacesToBake.Dequeue();
                    if (surface != null )
                    {
                        surface.BuildNavMesh();
                        processed++;
                    }
                    yield return null;
                }
            }

            yield return new WaitForSeconds(_bakeInterval);
        }
        _isProcessing = false; 
    }
    
    public void UnregisterSurface(NavMeshSurface surface)
    {
        lock (_surfacesToBake)
        {
            var tempList = new List<NavMeshSurface>(_surfacesToBake);
            tempList.Remove(surface);
            _surfacesToBake = new Queue<NavMeshSurface>(tempList);
        }
    }
    public void ConfigureNavMeshSurface(NavMeshSurface surface)
    {
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = LayerMask.GetMask("Ground","Grid Object"); // 使用你的地面层级
        surface.agentTypeID = 0; // 默认代理类型
    }
}