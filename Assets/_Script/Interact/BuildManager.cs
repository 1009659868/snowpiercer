using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    public static BuildManager _instance;
    private GameObject _selected;
    public RecipeType type;
    public GameObject Selected{
        get { return _selected;}
        set { _selected = value;}
    }
    void Awake()
    {
        _instance=this;
    }
}
public enum RecipeType{
    Rail,
    RailSupply,
    MachineGun,
    DockTrain
}
