using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Train : MonoBehaviour
{
    private static Train _instance;
    public static Train Instance => _instance;
    public const float SPEED_MODE_MULTIPLIER = 3f;

    public TrainEngine engine;
    public float speed;
    [SerializeField] public List<Car> cars;

    public float progress { get; private set; }

    private void Awake()
    {
        _instance = this;

    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            StartEngine();
        }
    }

    private void StartEngine()
    {
        StartCoroutine(Progress_Co());
        EventManager.TrainStarted();
    }

    private IEnumerator Progress_Co()
    {
        // 初始化车厢轨道分配（第一个车厢在第一个轨道，后续车厢依次分配）
        for (int i = 0; i < cars.Count; i++)
        {
            cars[i].AttachToRail(Railway.Instance.rails[cars.Count-i-1]);
        }
        progress = 0f;
        while (true)
        {
            progress += Time.deltaTime * speed * (engine == null ? SPEED_MODE_MULTIPLIER : 1f);
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
        for (int i = 0; i < cars.Count; i++)
        {
            if (cars[i].attachedRail?.next == null)
            {
                cars[i].Explode();
            }
            else
            {
                // 每个车厢根据前车位置更新轨道
                var prevCar = i > 0 ? cars[i-1] : engine;
                if (prevCar != null && !prevCar.isExploded)
                {
                    cars[i].AttachToRail((Rail)prevCar.attachedRail?.previous);
                }
            }
        }
    }
}
