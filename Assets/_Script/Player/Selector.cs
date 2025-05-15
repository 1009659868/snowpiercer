using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Selector : MonoBehaviour
{
    [SerializeField] private PlayerStack playerStack;
    [SerializeField] private MouseFocusChecker mouseFocusChecker;
    // [SerializeField] private FocusChecker focusChecker;
    [SerializeField] private StackablePreview previews;
    [SerializeField] private Material selectedMaterial;
    private IStackable selectedStackable;
    private GameObject focus { get; set; }
    
    public bool isPreviewing { get; private set; }
    public bool _isPreviewing;

    // 当前通过鼠标射线检测到的目标物体
    
    private void Update()
    {
        // 每帧更新鼠标检测目标
        focus = mouseFocusChecker.mouseFocus;
        // focus = focusChecker.focus;

        _isPreviewing=isPreviewing = HandlePreview();

        if (isPreviewing)
        {
            ClearSelections();
            return;
        }

        HandleSelection();
    }
    private void ClearSelections(){
        if (selectedStackable != null)
        {
            selectedStackable.Deselect();
            selectedStackable = null;
        }
    }
    private bool HandlePreview()
    {
        if (playerStack.isStackEmpty)
        {
            previews.Disable();
            return false;
        }
        if (focus != null)
        {
            previews.Disable();
            return false;
        }
        // 当玩家手上有物体时，在鼠标位置（转换为世界坐标，可以通过射线检测获得碰撞点或依据网格算法计算）展示预览
        if (playerStack.stackedType != StackableType.RAIL)
        {
            previews.SetPosition(mouseFocusChecker.worldPosition, playerStack.stackedType);
            previews.SetPreview(playerStack.stackedType);
            return true;
        }
        previews.SetPosition(mouseFocusChecker.worldPosition, StackableType.RAIL);
        previews.rail.Reset();
        
        if (previews.rail.upperNeighbor != null)
        {
            if (previews.rail.upperNeighbor.previous != null && previews.rail.upperNeighbor.next == null)
            {    
                previews.SetPreview(StackableType.RAIL);
                return true;
            }
        }
        if (previews.rail.lowerNeighbor != null)
        {
            if (previews.rail.lowerNeighbor.previous != null && previews.rail.lowerNeighbor.next == null)
            {    
                previews.SetPreview(StackableType.RAIL);
                return true;
            }
        }
        if (previews.rail.rightNeighbor != null)
        {
            if (previews.rail.rightNeighbor.previous != null && previews.rail.rightNeighbor.next == null)
            {    
                previews.SetPreview(StackableType.RAIL);
                return true;
            }
        }
        if (previews.rail.leftNeighbor != null)
        {
            if (previews.rail.leftNeighbor.previous != null && previews.rail.leftNeighbor.next == null)
            {    
                previews.SetPreview(StackableType.RAIL);
                return true;
            }
        }

        previews.Disable();
        return false;
    }

    private void HandleSelection()
    {
        if (focus != null)
        {
            
            // if(focus.TryGetComponent(out IInteractable interactable)){
            //     if(interactable == selectedInteractable) return;

            //     ClearSelections();

            //     interactable.Select(selectedMaterial);
            //     selectedInteractable = interactable;
            //     return;
            // }

            if (focus.TryGetComponent(out IStackable stackable))
            {
                stackable = stackable.Peek();
                
                if (stackable == selectedStackable) return;

                ClearSelections();

                try
                {
                    if (((ILinkable)stackable).next != null) return;
                }
                catch (System.Exception){}

                stackable.Select(selectedMaterial);
                selectedStackable = stackable;
            }
        }
        else
        {
            ClearSelections();
        }
    }
}

[System.Serializable]
public struct StackablePreview
{
    public Rail rail;
    public GameObject wood;
    public GameObject rock;

    public void SetPreview(StackableType type)
    {
        switch (type)
        {
            case StackableType.RAIL:
                rail.gameObject.SetActive(true);
                wood.SetActive(false);
                break;
            case StackableType.WOOD:
                rail.gameObject.SetActive(false);
                wood.SetActive(true);
                break;
            case StackableType.ROCK:
                rail.gameObject.SetActive(false);
                rock.SetActive(true);
                break;
        }
    }

    public void Disable()
    {
        rail.gameObject.SetActive(false);
        wood.SetActive(false);
        rock.SetActive(false);
    }

    public void SetPosition(Vector3 position, StackableType type)
    {
        switch (type)
        {
            case StackableType.RAIL:
                rail.GetComponent<IGrid>().SnapToGrid(position);
                break;
            case StackableType.WOOD:
                wood.GetComponent<IGrid>().SnapToGrid(position);
                break;
            case StackableType.ROCK:
                rock.GetComponent<IGrid>().SnapToGrid(position);
                break;
        }
    }
}
