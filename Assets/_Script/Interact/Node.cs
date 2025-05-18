using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Node:MonoBehaviour {
    [SerializeField]private Material hoverMaterial;
    public bool isEnter{ get; set; }
    public bool isInteracting =false;
    [SerializeField] public Vector3 offset = new Vector3(0, 4f, 0);
    private Material initMaterial;
    private List<Renderer> _childRenderers=new List<Renderer>();
    private List<Material> _originalMaterials=new List<Material>();
    protected virtual void Start () {
        
        Renderer[] renderers= GetComponentsInChildren<Renderer>(true);
        foreach(Renderer renderer in renderers) {
            _childRenderers.Add(renderer);
            _originalMaterials.Add(renderer.material);
        }
    }
    protected virtual void OnMouseDown(){
        if(BuildManager._instance.Selected!=null){
            BuildManager._instance.Selected=null;
        }

    }
    protected virtual void OnMouseEnter()
    {
        hoverEnter();
    }
    protected virtual void OnMouseExit(){
        hoverExit();
    }
    public virtual void hoverEnter(){
        isEnter=true;
        foreach(Renderer renderer in _childRenderers) {
            if(renderer!=null){
                renderer.material = hoverMaterial;
            }
        }
    }
    public virtual void hoverExit(){
        isEnter=false;
        for(int i=0;i<_childRenderers.Count;i++){
            if(_childRenderers[i]!=null){
                _childRenderers[i].material=_originalMaterials[i];
            }
        }
    }
    

}