using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


//运输车厢
public class RailSupply : Car , IInteractable
{
    public enum StorageType { None, Small, Large }
    [Header("Storage Settings")]
    [SerializeField] private Transform[] smallItemPositions; // 小物体存储位置
    [SerializeField] private Transform[] largeItemPositions; // 轨道存储位置
    [SerializeField] private StackablePreview previews;
    [SerializeField] private Material selectedMaterial;
    [Header("Prefabs")]
    [SerializeField] private GameObject woodPrefab;
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private GameObject railPrefab;

    private StorageType currentStorageType = StorageType.None;
    private Stack<IStackable> storedItems = new Stack<IStackable>();
    private int currentUnits = 0;
    private bool _isInteractable = true;
    public bool isInteractable { get => _isInteractable; set => _isInteractable = value; }
    

    public bool CanInteract(PlayerStack playerStack){
        if(!isInteractable) return false;
        if(playerStack.isStackEmpty){
            //玩家手中无物品,可以取出物品
            return storedItems.Count>0;
        }else{
            //玩家手中有物品,检测是否可以存入
            if(currentStorageType == StorageType.None){
                return true;
            }else if(currentStorageType == StorageType.Small){
                return playerStack.stackedType!=StackableType.RAIL&&currentUnits<smallItemPositions.Count();
            }else{
                return playerStack.stackedType == StackableType.RAIL&&currentUnits<largeItemPositions.Count();
            }
        }
    }

    public void Interact(PlayerStack playerStack){
        if(playerStack.isStackEmpty){
            //取出物品
            TakeItem(playerStack);
        }else{
            //存入物品
            StoreItem(playerStack);
        }
    }
    private void StoreItem(PlayerStack playerStack){
        IStackable item = null;
        // playerStack.Peek();

        if(item==null) return;

        if(item.type == StackableType.RAIL){
            // 存储轨道
            if(currentStorageType == StorageType.None){
                currentStorageType = StorageType.Large;
            }
            if(currentStorageType != StorageType.Large || currentUnits>largeItemPositions.Count()){
                return;
            }
            
            item = playerStack.Pop();
            ((Resource)item).SnapToStorage(largeItemPositions[currentUnits],transform,railPrefab.transform.localScale);
            storedItems.Push(item);
            currentUnits+=1;
        }else{
            // 存储木头或石头等
            if (currentStorageType == StorageType.None)
            {
                currentStorageType = StorageType.Small;
            }

            if (currentStorageType != StorageType.Small || currentUnits >= smallItemPositions.Count())
            {
                return;
            }
            item = playerStack.Pop();
            ((Resource)item).SnapToStorage(smallItemPositions[currentUnits],transform,woodPrefab.transform.localScale);
            storedItems.Push(item);
            currentUnits+=1;
        }
    }

    private void TakeItem(PlayerStack playerStack){
        if(storedItems.Count==0) return;
        IStackable item = storedItems.Pop();
        //重置父物体
        
    }
    public Vector3 GetInteractionPosition()
    {
        return transform.position + transform.forward * 2f;
    }
    
}
