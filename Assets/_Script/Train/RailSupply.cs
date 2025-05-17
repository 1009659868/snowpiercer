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
    [Header("Prefabs")]
    [SerializeField] private GameObject woodPrefab;
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private GameObject railPrefab;
    //存储类型:如wood和rock为小物体类,rail为大物体类
    //一个RailSupply只能存储一种类型的物体,由首次存储的类型决定,清空后重置
    private StorageType currentStorageType = StorageType.None;
    private StackableType storedType = StackableType.NONE;
    private Stack<IStackable> storedItems = new Stack<IStackable>();
    private int currentUnits = 0;
    private PlayerStack playerStack =null;
    private Coroutine interactionRoutine;
    public int GetStoredCount(StackableType type)
    {
        // 未存储或类型不匹配时返回0
        if (currentStorageType == StorageType.None || storedType != type) 
            return 0;
        
        return currentUnits;
    }
    public int ConsumeItems(StackableType type, int requiredAmount)
    {
        // 有效性检查
        if (currentStorageType == StorageType.None || 
            storedType != type || 
            requiredAmount <= 0)
        {
            return 0;
        }
    
        int actualConsumed = Mathf.Min(requiredAmount, currentUnits);
        int remaining = actualConsumed;
        
        // 消耗物品堆栈
        while (remaining > 0 && storedItems.Count > 0)
        {
            var item = storedItems.Pop();
            DestroyStackableItem(item);
            remaining--;
            currentUnits--;
        }
    
        // 更新存储状态
        if (storedItems.Count == 0)
        {
            currentStorageType = StorageType.None;
            storedType = StackableType.NONE;
        }
    
        return actualConsumed;
    }

    private void DestroyStackableItem(IStackable item)
    {
        // 处理连接关系
        if (item.upper != null)
        {
            item.upper.lower = item.lower;
        }
        if (item.lower != null)
        {
            item.lower.upper = item.upper;
        }

        // 销毁游戏对象
        MonoBehaviour itemMono = item as MonoBehaviour;
        if (itemMono != null)
        {
            // 解除父物体关系
            itemMono.transform.parent = null;
            Destroy(itemMono.gameObject);
        }
    }
    protected override void Start()
    {
        base.Start();
        playerStack = FindObjectOfType<PlayerStack>();
    }
    //当鼠标进入物体并
    //按下左键时执行存储
    //按下右键时执行拿取
    protected override void OnMouseEnter()
    {
        base.OnMouseEnter();
        interactionRoutine = StartCoroutine(HandleInteraction());
    }
    protected override void OnMouseExit()
    {
        base.OnMouseExit();
        if(interactionRoutine != null)
        {
            StopCoroutine(interactionRoutine);
            interactionRoutine = null;
        }
        playerStack.isInteracting=isInteracting = false;
    }
    private IEnumerator HandleInteraction(){
        while (true)
        {
            // 等待有效交互按键
            yield return new WaitUntil(() => 
                playerStack.isStore || 
                playerStack.isRetrieve
            );

            // 确定操作类型
            bool isStore = playerStack.isStore;
            bool isRetrieve = playerStack.isRetrieve;

            // 防止同时触发
            if (isStore && isRetrieve)
            {
                Debug.Log("Cannot perform both actions simultaneously");
                yield return new WaitForEndOfFrame();
                continue;
            }

            // 执行操作
            if (isStore) yield return StartCoroutine(StoreRoutine());
            if (isRetrieve) yield return StartCoroutine(RetrieveRoutine());

            // 等待操作完成
            yield return new WaitWhile(() => isInteracting);
        }
    }
    private IEnumerator StoreRoutine()
    {
        playerStack.isInteracting=isInteracting = true;
        StoreItems();

        // 等待按键释放或操作完成
        yield return new WaitWhile(() => 
            PlayerKeyBinding.isPressed(playerStack.binding.storeKeys) && 
            !playerStack.isStackEmpty &&
            currentUnits < GetAvailableSlots(currentStorageType)
        );

        playerStack.isInteracting=isInteracting = false;
    }
    private IEnumerator RetrieveRoutine()
    {
        playerStack.isInteracting=isInteracting = true;
        RetrieveItems();

        // 支持长按连续取出
        while (PlayerKeyBinding.isPressed(playerStack.binding.retrieveKeys) )
        {
            if (storedItems.Count == 0 || playerStack.isStackFull) break;

            RetrieveItems();
            yield return new WaitForSeconds(0.1f); // 连续操作间隔
        }

        playerStack.isInteracting=isInteracting = false;
    }

    private void StoreItems(){
        
        if (playerStack == null || playerStack.isStackEmpty) return;

        StackableType playerType = playerStack.stackedType;
        StorageType requiredType = GetStorageType(playerType);
        // 类型检查
        if (currentStorageType != StorageType.None && currentStorageType != requiredType)
        {
            Debug.Log("Cannot store different item types");
            return;
        }
        // 计算可用空间
        int availableSlots = GetAvailableSlots(requiredType);
        int itemsToStore = playerStack.stack.Count;

        if (availableSlots < itemsToStore)
        {
            Debug.Log("Not enough storage space");
            return;
        }
        // 初始化存储类型
        if (currentStorageType == StorageType.None){
            currentStorageType = requiredType;
            storedType = playerType;
        }
            
        
        // 转移物品
        while (!playerStack.isStackEmpty && currentUnits < availableSlots)
        {
            IStackable item = playerStack.Pop();
            item.isGrabbed = false;
            item.Clear();

            // 设置存储位置
            Transform slot = GetNextStoragePosition();
            if (slot == null) break;

            MonoBehaviour itemMono = item as MonoBehaviour;
            if (itemMono != null)
            {
                itemMono.transform.SetPositionAndRotation(slot.position, slot.rotation);
                itemMono.transform.parent = slot;
            }

            storedItems.Push(item);
            currentUnits++;
        }
    }

    private void RetrieveItems(){
        Debug.Log("Retrieve");
        if (playerStack == null || playerStack.isStackFull) return;

        if (storedItems.Count == 0) return;

        // 获取实际可取出数量
        int availableTake = Mathf.Min(
            PlayerStack.MAX_STACK_SIZE - playerStack.stack.Count,
            storedItems.Count
        );
        IStackable lowerItem = null;
        if (!playerStack.isStackEmpty)
        {
            lowerItem = playerStack.stack.Peek();
        }
        Debug.Log("Retrieve");
        for(int i = 0; i < availableTake; i++){
            if (!storedItems.TryPop(out IStackable currentItem)) break;

            // 断开存储连接
            MonoBehaviour itemMono = currentItem as MonoBehaviour;
            if (itemMono != null)
            {
                itemMono.transform.parent = null;
                var rb = itemMono.GetComponent<Rigidbody>();
                if (rb != null) Destroy(rb);
            }

            // 处理链接关系（类似PlayerStack的Grab逻辑）
            if (currentItem is ILinkable linkable)
            {
                linkable.previous = null;
                linkable.next = null;
            }

            // 建立堆叠关系
            currentItem.lower = lowerItem;
            currentItem.upper = null;
            if (lowerItem != null)
            {
                lowerItem.upper = currentItem;
            }

            // 重置物品状态
            currentItem.isGrabbed = true;
            currentItem.isFlying = false;
            currentItem.Reset();

            // 加入玩家堆栈
            playerStack.stack.Push(currentItem);
            lowerItem = currentItem;
            currentUnits--;

            currentItem.SnapToStack(
                lowerItem == null ? playerStack.stackOrigin.position : lowerItem.anchor,
                playerStack.transform.eulerAngles
            );
        }
        // 更新玩家堆栈顶部状态
        if (!playerStack.isStackEmpty)
        {
            playerStack.stack.Peek().upper = null;
        }
        // 重置存储状态
        if (storedItems.Count == 0)
        {
            currentStorageType = StorageType.None;
            currentUnits = 0;
        }
    }

    private StorageType GetStorageType(StackableType type)
    {
        switch (type)
        {
            case StackableType.WOOD:
            case StackableType.ROCK:
                return StorageType.Small;
            case StackableType.RAIL:
                return StorageType.Large;
            default:
                return StorageType.None;
        }
    }
    private int GetAvailableSlots(StorageType type)
    {
        return type == StorageType.Small ? 
            smallItemPositions.Length - currentUnits : 
            largeItemPositions.Length - currentUnits;
    }
    private Transform GetNextStoragePosition()
    {
        if (currentStorageType == StorageType.Small)
        {
            if (currentUnits < smallItemPositions.Length)
                return smallItemPositions[currentUnits];
        }
        else if (currentStorageType == StorageType.Large)
        {
            if (currentUnits < largeItemPositions.Length)
                return largeItemPositions[currentUnits];
        }
        return null;
    }
}
