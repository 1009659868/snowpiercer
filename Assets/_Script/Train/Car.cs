using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class Car : MonoBehaviour
{
    private static Train main;
    public string _name;
    private TrainManager _TrainManager=>TrainManager._instance;
    private Train _Train=>Train._instance;
    private Coroutine moveCoroutine;
    protected bool isPathCalculated;
    public Rail attachedRail;
    public float progressOffset;
    public int attachedRailIndex => Railway.Instance.rails.IndexOf(attachedRail);
    public GameObject[] destroyBeforeExplode;
    public ParticleSystem[] particles;

    public virtual float progress => main.progress;
    public bool isExploded { get; private set; }
    
    protected virtual void Start()
    {
        // // 延迟初始化确保轨道系统已建立
        StartCoroutine(DelayedInit());
    }
    protected virtual void Update(){
        
    }
    private IEnumerator DelayedInit()
    {
        yield return new WaitUntil(() => 
            Railway.Instance != null && 
            Railway.Instance.rails.Count > 0 &&
            Train._instance != null&&
            _TrainManager._activedCar.Count>0);
        // if (attachedRail == null)
        // {
        //     // // 根据车厢顺序自动分配初始轨道
        //     // int index = Train.Instance.cars.IndexOf(this);
        //     // int RIndex=Train.Instance.cars.Count-index-1<0?Train.Instance.cars.Count:Train.Instance.cars.Count-index-1;
        //     // AttachToRail(Railway.Instance.rails[RIndex]);
        //     Debug.Log(_name);
        //     //_Train.cars.Add(_TrainManager.GetCar(_name).GetComponent<Car>());
        //     int index = _TrainManager.CarList.IndexOf(this);
        //     Debug.Log(index);
        //     int railIndex = _TrainManager._activedCar.Count - index - 1;
        //     railIndex = Mathf.Clamp(railIndex, 0, Railway.Instance.rails.Count - 1);
        //     AttachToRail(Railway.Instance.rails[railIndex]);
        // }
        if(main==null)
        main = Train._instance; // 直接引用单例实例
    }
    // private void Awake()
    // {
    //     main = Train._instance; // 直接引用单例实例
    //     // main = this.GetComponentInParent<Train>();
    //     // 添加保护性检查
    //     if (main == null)
    //     {
    //         Debug.LogError("Train instance not found! Ensure Train script exists in the scene.");
    //     }
    // }

    private void OnEnable()
    {
        EventManager.OnTrainStarted += StartMovement;
        EventManager.OnTrainPassedNextRail += AttachToNextRail;
    }

    private void OnDisable()
    {
        EventManager.OnTrainStarted -= StartMovement;
        EventManager.OnTrainPassedNextRail -= AttachToNextRail;
    }

    public virtual void StartMovement()
    {
        moveCoroutine = StartCoroutine(Move_Co());
    }

    public virtual void StopMovement()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
    }

    public virtual void Explode()
    {
        if (isExploded) return;

        StopMovement();
        foreach (var obj in destroyBeforeExplode)
        {
            Destroy(obj, 0.1f);
        }
        for (int i = 0; i < particles.Length; i++)
        {   
            particles[i].Play();
        }
        Destroy(gameObject, particles[0].main.duration);

        isExploded = true;
    }

    public virtual void AttachToRail(Rail rail)
    {
        attachedRail = rail;
        ResetPositionToRailStart();
        isPathCalculated = false;
        
    }

    public virtual void AttachToNextRail()
    {
        if (attachedRail.next == null)
        {
            Explode();
            return;
        }

        attachedRail = (Rail) attachedRail.next;
        isPathCalculated = false;
    }
    private void ResetPositionToRailStart()
    {
        Vector3 startPos = Vector3.zero;
        float startAngleY = 0f;
        CalculatePath(ref startPos, ref startPos, ref startAngleY, ref startAngleY);
        transform.position = startPos;
        transform.eulerAngles = Vector3.up * startAngleY;
    }
    protected void CalculatePath(ref Vector3 startPos, ref Vector3 endPos, ref float startAngleY, ref float endAngleY)
    {
        if (attachedRail.isReversed)
        {
            startPos = attachedRail.isCorner ? attachedRail.cornerPath.end.position : attachedRail.normalPath.end.position;
            endPos = attachedRail.isCorner ? attachedRail.cornerPath.start.position : attachedRail.normalPath.start.position;

            startAngleY = 180f + (attachedRail.isCorner ? attachedRail.cornerPath.end.eulerAngles.y : attachedRail.normalPath.end.eulerAngles.y);
            endAngleY = 180f + (attachedRail.isCorner ? attachedRail.cornerPath.start.eulerAngles.y : attachedRail.normalPath.start.eulerAngles.y);
        }
        else
        {
            startPos = attachedRail.isCorner ? attachedRail.cornerPath.start.position : attachedRail.normalPath.start.position;
            endPos = attachedRail.isCorner ? attachedRail.cornerPath.end.position : attachedRail.normalPath.end.position;

            startAngleY = attachedRail.isCorner ? attachedRail.cornerPath.start.eulerAngles.y : attachedRail.normalPath.start.eulerAngles.y;
            endAngleY = attachedRail.isCorner ? attachedRail.cornerPath.end.eulerAngles.y : attachedRail.normalPath.end.eulerAngles.y;
        }

        isPathCalculated = true;
    }


    public virtual IEnumerator Move_Co()
    {
        Vector3 startPosition = Vector3.zero;
        Vector3 targetPosition = Vector3.zero;

        float startAngleY = 0f;
        float targetAngleY = 0f;

        while (true)
        {
            if (!isPathCalculated)
            {
                CalculatePath(ref startPosition, ref targetPosition, ref startAngleY, ref targetAngleY);
            }

            if (attachedRail.isCorner)
            {
                transform.position = Helper.Vector3QLerp(startPosition, attachedRail.cornerPath.curveAnchor.position, targetPosition, progress);
            }
            else
            {
                transform.position = Vector3.Lerp(startPosition, targetPosition, progress); 
            }
            transform.eulerAngles = Vector3.up * Mathf.LerpAngle(startAngleY, targetAngleY, progress);

            yield return 0;
        }
    }
}