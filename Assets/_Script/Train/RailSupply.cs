using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


//运输车厢
public class RailSupply : Car 
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

    protected override void Start()
    {
        base.Start();
        
    }


}
