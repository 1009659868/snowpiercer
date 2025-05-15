using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//控制地图生成
//控制出生点和终点
//控制时间变化

/// <summary>
///  游戏管理器
/// </summary>
public class GameManager : Singleton<GameManager>
{
    public static GameManager _instance { get;}
    /// <summary>
    /// 需要生成的预制体集合
    /// </summary>
    public GameObject[] SystemPrefabs;
    public List<GameObject> _instancedSystemPrefabs = new List<GameObject>();

    void Start(){
        //加载时不销毁指定对象,GameManager将持续存在
        DontDestroyOnLoad(gameObject);
        InstantiateSystemPrefabs();
    }
    /// <summary>
    /// 实例化系统预制体
    /// </summary>
    private void InstantiateSystemPrefabs(){
        GameObject prefabInstance;
        foreach(var systemPrefab in SystemPrefabs){
            prefabInstance=Instantiate(systemPrefab);
            _instancedSystemPrefabs.Add(prefabInstance);
        }
    }
    protected override void OnDestroy() {
        base.OnDestroy();
        foreach(var item in _instancedSystemPrefabs){
            Destroy(item);
        }
        _instancedSystemPrefabs.Clear();
    }
}
