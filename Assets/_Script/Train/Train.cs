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
        List<Car> cars=TrainManager._instance.GetCars();
        for (int i = 0; i < cars.Count(); i++)
        {   
            int targetRailIndex = cars.Count()-i-1;
            Rail targetRail = Railway.Instance.rails[targetRailIndex];
            // targetRailIndex = Mathf.Clamp(targetRailIndex, 0, Railway.Instance.rails.Count );
            cars[i].AttachToRail(targetRail);
            // cars[i].AttachToRail(Railway.Instance.rails[cars.Count-i-1]);
        }

    }
    private IEnumerator Progress_Co()
    {
        
        List<Car> cars=TrainManager._instance.GetCars();
        progress = 0f;
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
