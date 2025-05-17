using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Craft : MonoBehaviour
{
    public GameObject Rail;
    public GameObject RailSupply;
    public GameObject MachineGun;
    public GameObject DockTrain;
    public void OnCraftRail(){
        BuildManager._instance.Selected=Rail;
    }
    public void OnCraftRailSupply(){
        BuildManager._instance.Selected = RailSupply;
    }
    public void OnCraftMachineGun(){
        Debug.Log("C MachineGun");
        BuildManager._instance.Selected = MachineGun;
    }
    public void OnCraftDockTrain(){
        BuildManager._instance.Selected = DockTrain;
    }
}
