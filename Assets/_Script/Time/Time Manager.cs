using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeManager : MonoBehaviour
{
    public static TimeManager _instance;

    [Header("UI Elements")]
    [SerializeField] private Image outerProgressBar;
    [SerializeField] private Gradient dayNightGradient; // 渐变变量
    [SerializeField] private float colorTransitionSpeed = 3f; // 渐变速度

    [Header("Day/Night Cycle")]
    public Light sunLight;
    public float dayDuration = 600f; // 一天的总秒数
    [Range(0, 1)] public float currentTimeOfDay;
    public bool isNight;

    [Header("Day/Night Settings")]
    [Tooltip("Start of night phase (0-1)")]
    [Range(0, 1)] public float nightStart = 0.75f;
    [Tooltip("End of night phase (0-1)")]
    [Range(0, 1)] public float nightEnd = 0.25f;
    [Range(0, 1)] public float startTimeOfDay = 0f;

    [Header("Time Scale")]
    [SerializeField]
    [Range(0,1)] private float timeScale = 0.6f;
    private float orginTiemScale = 1;
    void Awake()
    {
        _instance= this;
        currentTimeOfDay =startTimeOfDay;
        InitializeUI();
        UpdateSunAndNight();
    }
    void Update(){
        UpdateDayNightCycle();
        UpdateUIElements();
    }
    private void InitializeUI(){
        if(outerProgressBar!=null){
            outerProgressBar.type = Image.Type.Filled;
            outerProgressBar.fillMethod = Image.FillMethod.Radial360;
            outerProgressBar.fillOrigin = (int)Image.Origin360.Top;
            outerProgressBar.fillClockwise = true;
        }
    }
    private void UpdateUIElements(){
        if(outerProgressBar!=null){
            outerProgressBar.fillAmount = currentTimeOfDay;
            Color targetColor = dayNightGradient.Evaluate(currentTimeOfDay);//颜色渐变
            outerProgressBar.color = Color.Lerp(
                outerProgressBar.color,
                targetColor,
                Time.deltaTime*colorTransitionSpeed
            );
        
        }
    }
    private void UpdateDayNightCycle()
    {
        currentTimeOfDay += Time.deltaTime / dayDuration;
        currentTimeOfDay %= 1;
        UpdateSunAndNight();
    }
    private void UpdateSunAndNight()
    {
        // 更新太阳位置
        sunLight.transform.rotation = Quaternion.Euler(new Vector3(
            (currentTimeOfDay * 360f) - 90f,
            90f,
            0f
        ));

        // 判断昼夜状态（处理跨天的情况）
        if (nightStart < nightEnd)
        {
            isNight = currentTimeOfDay >= nightStart && currentTimeOfDay <= nightEnd;
        }
        else
        {
            isNight = currentTimeOfDay >= nightStart || currentTimeOfDay <= nightEnd;
        }
    }
    // 设置当前时间（0-1）
    public void SetCurrentTime(float normalizedTime)
    {
        currentTimeOfDay = Mathf.Clamp01(normalizedTime);
        UpdateSunAndNight();
    }

    // 设置昼夜时间段（自动处理反向范围）
    public void SetDayNightPhase(float start, float end)
    {
        nightStart = Mathf.Clamp01(start);
        nightEnd = Mathf.Clamp01(end);
        UpdateSunAndNight();
    }

    public float GetDayDuration()
    {
        if (nightStart < nightEnd)
        {
            return 1 - (nightEnd - nightStart);
        }
        return (nightStart - nightEnd);
    }

    public float GetNightDuration()
    {
        if (nightStart < nightEnd)
        {
            return nightEnd - nightStart;
        }
        return (1 - nightStart) + nightEnd;
    }
    public void SetTimeScale()
    {
        Time.timeScale = timeScale;
    }

    public void ResetTimeScale()
    {
        Time.timeScale = orginTiemScale;
    }
}
