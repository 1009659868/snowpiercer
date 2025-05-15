using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TrainManager : MonoBehaviour
{
    
    public static TrainManager _instance;
    public Transform _Holder;
    //保存所有车厢的prefab,并提供加载prefab和回收prefab的接口
    [Header("Cars")]
    [SerializeField]private GameObject[] cars;
    public List<Car> CarList;

    public Dictionary<string, GameObject> _activedCar = new Dictionary<string, GameObject>();

    void Awake()
    {
        _instance = this;
        foreach (var car in cars){
            GameObject carobj=Instantiate(car,Vector3.zero, Quaternion.identity);
            carobj.transform.parent=_Holder;
            _activedCar.Add(car.name,carobj);
            CarList.Add(carobj.GetComponent<Car>());
        }
    }
    public GameObject GetCar(string name){
        return _activedCar[name];
    }
    public List<Car> GetCars(){
        return CarList;
    }

}
