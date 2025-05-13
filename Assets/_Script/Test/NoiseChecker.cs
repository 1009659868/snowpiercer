using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class NoiseChecker : MonoBehaviour
{
    public RenderTexture curveRT;      // 曲线图渲染纹理
    public RenderTexture noiseRT;      // 噪声图渲染纹理
    public RenderTexture heightRT;     // 高度图渲染纹理
    public Material curveMaterial;     // 曲线绘制材质
    public Material noiseMaterial;     // 噪声图材质
    public Gradient heightGradient;    // 高度颜色渐变

    [Header("UI References")]
    public RawImage curveImage;
    public RawImage noiseImage;
    public RawImage heightImage;
    public Dropdown noiseTypeDropdown;
    public InputField scaleInput;
    public Slider amplitudeSlider;

    private NoiseType currentNoiseType = NoiseType.Height_low;
    private NoiseSettings currentSettings;

    void Start()
    {
        // 初始化渲染纹理
        InitializeRenderTextures();
        
        // 初始化UI控件
        noiseTypeDropdown.onValueChanged.AddListener(OnNoiseTypeChanged);
        scaleInput.onEndEdit.AddListener(OnScaleChanged);
        amplitudeSlider.onValueChanged.AddListener(OnAmplitudeChanged);

        // 获取初始设置
        UpdateCurrentSettings();
        RefreshVisualizations();
    }

    void InitializeRenderTextures()
    {
        int rtSize = 512;
        CreateRenderTexture(ref curveRT, rtSize);
        CreateRenderTexture(ref noiseRT, rtSize);
        CreateRenderTexture(ref heightRT, rtSize);

        curveImage.texture = curveRT;
        noiseImage.texture = noiseRT;
        heightImage.texture = heightRT;
    }

    void CreateRenderTexture(ref RenderTexture rt, int size)
    {
        rt = new RenderTexture(size, size, 0);
        rt.enableRandomWrite = true;
        rt.Create();
    }

    void UpdateCurrentSettings()
    {
        currentSettings = MapManager._instance.GetNoiseSettings(currentNoiseType);
    }

    void RefreshVisualizations()
    {
        UpdateCurveGraph();
        UpdateNoiseMap();
        UpdateHeightMap();
    }

    void UpdateCurveGraph()
    {
        // 在XZ平面沿X轴采样
        Texture2D tex = new Texture2D(curveRT.width, curveRT.height);
        Vector3 samplePos = Vector3.zero;

        for (int x = 0; x < curveRT.width; x++)
        {
            samplePos.x = x;
            float value = GetNoiseValue(samplePos);
            int graphY = Mathf.FloorToInt(value * curveRT.height);

            for (int y = 0; y < curveRT.height; y++)
            {
                Color color = (y == graphY) ? Color.green : Color.black;
                tex.SetPixel(x, y, color);
            }
        }

        Graphics.Blit(tex, curveRT);
        Destroy(tex);
    }

    void UpdateNoiseMap()
    {
        // 生成黑白噪声图
        Texture2D tex = new Texture2D(noiseRT.width, noiseRT.height);
        Vector3 samplePos = Vector3.zero;

        for (int x = 0; x < noiseRT.width; x++)
        {
            for (int z = 0; z < noiseRT.height; z++)
            {
                samplePos.x = x;
                samplePos.z = z;
                float value = GetNoiseValue(samplePos);
                tex.SetPixel(x, z, Color.Lerp(Color.black, Color.white, value));
            }
        }

        tex.Apply();
        Graphics.Blit(tex, noiseRT);
        Destroy(tex);
    }

    void UpdateHeightMap()
    {
        // 生成彩色高度图
        Texture2D tex = new Texture2D(heightRT.width, heightRT.height);
        Vector3 samplePos = Vector3.zero;

        for (int x = 0; x < heightRT.width; x++)
        {
            for (int z = 0; z < heightRT.height; z++)
            {
                samplePos.x = x;
                samplePos.z = z;
                float value = GetNoiseValue(samplePos);
                tex.SetPixel(x, z, heightGradient.Evaluate(value));
            }
        }

        tex.Apply();
        Graphics.Blit(tex, heightRT);
        Destroy(tex);
    }

    float GetNoiseValue(Vector3 pos)
    {
        // float combinNoise= NoiseGenerator._instance.GetNoiseValue(pos);
        // 根据当前噪声类型获取值
        switch(currentNoiseType)
        {
            case NoiseType.Height_low:
                return NoiseGenerator._instance.GetHeightNoise(pos);
            case NoiseType.Moisture:
                return NoiseGenerator._instance.GetMoistureNoise(pos);
            case NoiseType.Temperature:
                return NoiseGenerator._instance.GetTemperatureNoise(pos);
            case NoiseType.Resource:
                return NoiseGenerator._instance.GetResourceNoise(pos);
            default:
                return 0;
        }
    }

    // UI事件处理
    void OnNoiseTypeChanged(int index)
    {
        currentNoiseType = (NoiseType)index;
        UpdateCurrentSettings();
        RefreshVisualizations();
    }

    void OnScaleChanged(string value)
    {
        float newScale;
        if(float.TryParse(value, out newScale))
        {
            currentSettings.scale = newScale;
            RefreshVisualizations();
        }
    }

    void OnAmplitudeChanged(float value)
    {
        currentSettings.amplitudes = value;
        RefreshVisualizations();
    }

    void OnGUI()
    {
        // 四窗口布局
        GUILayout.BeginArea(new Rect(0, 0, Screen.width/2, Screen.height/2));
        GUILayout.Label("曲线视图");
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(Screen.width/2, 0, Screen.width/2, Screen.height/2));
        GUILayout.Label("噪声图");
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(0, Screen.height/2, Screen.width/2, Screen.height/2));
        GUILayout.Label("高度图");
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(Screen.width/2, Screen.height/2, Screen.width/2, Screen.height/2));
        DrawControlPanel();
        GUILayout.EndArea();
    }

    void DrawControlPanel()
    {
        GUILayout.Label("噪声控制面板");
        GUILayout.Space(10);

        // 噪声类型选择
        GUILayout.Label("噪声类型:");
        currentNoiseType = (NoiseType)GUILayout.SelectionGrid(
            (int)currentNoiseType, 
            System.Enum.GetNames(typeof(NoiseType)), 
            3);

        // 参数控制
        GUILayout.Label("缩放:");
        currentSettings.scale = GUILayout.HorizontalSlider(currentSettings.scale, 1, 500);

        GUILayout.Label("振幅:");
        currentSettings.amplitudes = GUILayout.HorizontalSlider(currentSettings.amplitudes, 0, 2);

        if(GUILayout.Button("刷新视图"))
        {
            RefreshVisualizations();
        }
    }
}
