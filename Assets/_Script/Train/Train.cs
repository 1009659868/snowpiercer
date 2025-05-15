using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class Train : MonoBehaviour
{
    public static Train _instance;
    
    public const float SPEED_MODE_MULTIPLIER = 3f;
    public float speed;
    public float progress { get; private set; }
    public bool Runing=false;
    private void Awake()
    {
        _instance=this;
    }
    private void Start()
    {
        CarPosInit();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)&&!Runing)
        {
            Runing=!Runing;
            StartEngine();
        }else{
            //StopEngine();
        }
    }

    private void StartEngine()
    {
        StartCoroutine(Progress_Co());
        EventManager.TrainStarted();
    }
    private void CarPosInit(){
        // 确保轨道数量足够
        // 初始化车厢轨道分配（第一个车厢在第一个轨道，后续车厢依次分配）
        // List<Car> cars=TrainManager._instance.GetCars();
        // for (int i = 0; i < cars.Count(); i++)
        // {   
        //     int targetRailIndex = cars.Count()-i-1;
        //     Rail targetRail = Railway.Instance.rails[targetRailIndex];
        //     // targetRailIndex = Mathf.Clamp(targetRailIndex, 0, Railway.Instance.rails.Count );
        //     cars[i].AttachToRail(targetRail);
        //     // cars[i].AttachToRail(Railway.Instance.rails[cars.Count-i-1]);
        // }
        // 获取所有车厢
        List<Car> cars = TrainManager._instance.GetCars();
        if (cars.Count == 0) return;

        // 获取轨道列表
        List<Rail> rails = Railway.Instance.rails;
        if (rails.Count == 0) return;

        // 计算所有车厢的总长度
        float totalCarLength = 0f;
        foreach (var car in cars)
        {
            // 假设车厢长度通过某种方式获取，例如通过碰撞体或模型
            float carLength = GetCarLength(car);
            totalCarLength += carLength;
        }

        // 计算轨道的总长度
        float totalRailLength = GetTotalRailLength(rails);

        // 如果轨道总长度小于车厢总长度，无法初始化
        if (totalRailLength < totalCarLength)
        {
            Debug.LogError("轨道总长度不足以容纳所有车厢！");
            return;
        }

        // 初始化车厢位置
        float currentPosition = 0f;
        for (int i=cars.Count-1 ;i>=0; i--)
        {
            // 获取车厢长度
            float carLength = GetCarLength(cars[i]);

            // 计算车厢在轨道上的位置比例
            float progress = currentPosition / totalRailLength;

            // 将车厢附着到轨道上，并设置初始位置
            AttachCarToRail(cars[i], progress);

            // 更新当前位置
            currentPosition += carLength;
        }
        Vector3 fixDis = cars[0].transform.position - cars[0].attachedRail.transform.position;
        foreach (var car in cars){
            car.transform.position -= fixDis;
            car.transform.position+=new Vector3(GetRailLength(rails[0])/2,0,0);
        }
    }
    public float GetCarLength(Car car)
    {
        // 根据车厢的碰撞体或模型计算长度
        // 假设车厢有一个碰撞体，长度为 collider.bounds.size.z
        return car.GetComponent<Collider>().bounds.size.x;
    }
    private float GetTotalRailLength(List<Rail> rails)
    {
        float totalLength = 0f;
        foreach (var rail in rails)
        {
            // 获取轨道的长度
            float railLength = GetRailLength(rail);
            totalLength += railLength;
        }
        return totalLength;
    }
    private float GetRailLength(Rail rail)
    {
        // 根据轨道的起点和终点计算长度
        Vector3 startPos = rail.normalPath.start.position;
        Vector3 endPos = rail.normalPath.end.position;
        return Vector3.Distance(startPos, endPos);
    }
    private void AttachCarToRail(Car car, float progress)
    {
        // 根据进度找到轨道上的位置
        Tuple<Vector3,Rail> position = GetPositionOnRail(progress);
        car.transform.position = position.Item1;
        car.attachedRail = position.Item2;
        car.PathCalculated();
        this.progress = progress;
    }
    private Tuple<Vector3, Rail> GetPositionOnRail(float progress)
    {
        // 获取所有轨道
        List<Rail> rails = Railway.Instance.rails;
        if (rails.Count == 0)
        {
            Debug.LogError("没有可用的轨道！");
            return new Tuple<Vector3, Rail>(Vector3.zero, null);
        }

        // 计算总轨道长度
        float totalRailLength = GetTotalRailLength(rails);
        if (totalRailLength == 0)
        {
            Debug.LogError("总轨道长度为零！");
            return new Tuple<Vector3, Rail>(Vector3.zero, null);
        }

        // 计算目标位置的绝对距离
        float targetDistance = progress * totalRailLength;

        // 遍历轨道段，找到目标位置所在的轨道段
        float accumulatedLength = 0f;
        foreach (var rail in rails)
        {
            float railLength = GetRailLength(rail);
            if (accumulatedLength + railLength >= targetDistance)
            {
                // 计算在当前轨道段内的相对进度
                float relativeProgress = (targetDistance - accumulatedLength) / railLength;

                // 计算轨道段内的位置
                Vector3 startPos = rail.normalPath.start.position;
                Vector3 endPos = rail.normalPath.end.position;

                if (rail.isCorner)
                {
                    // 对于曲线轨道，使用曲线插值
                    Vector3 curveAnchor = rail.cornerPath.curveAnchor.position;
                    return new Tuple<Vector3, Rail>(Helper.Vector3QLerp(startPos, curveAnchor, endPos, relativeProgress),rail);
                }
                else
                {
                    // 对于直线轨道，使用线性插值
                    return new Tuple<Vector3, Rail>(Vector3.Lerp(startPos, endPos, relativeProgress),rail);
                }
            }

            accumulatedLength += railLength;
        }

        // 如果没有找到合适的轨道段，返回最后一个轨道段的终点
        Rail lastRail = rails.Last();
        Debug.Log("lastRail:"+lastRail);
        return new Tuple<Vector3, Rail>(lastRail.normalPath.end.position,lastRail);
    }
    private IEnumerator Progress_Co()
    {
        
        List<Car> cars=TrainManager._instance.GetCars();
        progress=0f;
        while (true)
        {
            progress += Time.deltaTime * speed * (cars[0] == null ? SPEED_MODE_MULTIPLIER : 1f);
            if (progress >= 1f)
            {
                progress = 0f;
                EventManager.TrainPassedNextRail();
                UpdateAllCarsAttachment(); // 更新所有车厢的轨道
            }
            yield return 0;
        }
    }
    private void UpdateAllCarsAttachment()
    {
        List<Car> cars=TrainManager._instance.GetCars();
        for (int i = 0; i < cars.Count; i++)
        {
            if (cars[i].attachedRail?.next == null)
            {
                cars[i].Explode();
            }
            else
            {
                // 每个车厢根据前车位置更新轨道
                var prevCar = i > 0 ? cars[i-1] : null;
                if (prevCar != null && !prevCar.isExploded)
                {
                    cars[i].AttachToRail((Rail)prevCar.attachedRail?.previous);
                }
            }
        }
    }
}
