using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable :ISelectable
{
    bool CanInteract(PlayerStack playerStack);
    void Interact(PlayerStack playerStack);

    Vector3 GetInteractionPosition();
    bool isInteractable { get; set; }
}