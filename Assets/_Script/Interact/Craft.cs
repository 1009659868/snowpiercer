using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//制作每件物品还需要配料表

public class Craft : MonoBehaviour
{
    public GameObject Rail;
    public GameObject RailSupply;
    public GameObject MachineGun;
    public GameObject DockTrain;
    public void OnCraftRail(){
        BuildManager._instance.Selected=Rail;
        BuildManager._instance.type=RecipeType.Rail;
    }
    public void OnCraftRailSupply(){
        BuildManager._instance.Selected = RailSupply;
        BuildManager._instance.type=RecipeType.RailSupply;
    }
    public void OnCraftMachineGun(){
        BuildManager._instance.Selected = MachineGun;
        BuildManager._instance.type=RecipeType.MachineGun;
    }
    public void OnCraftDockTrain(){
        BuildManager._instance.Selected = DockTrain;
        BuildManager._instance.type=RecipeType.DockTrain;
    }
}
