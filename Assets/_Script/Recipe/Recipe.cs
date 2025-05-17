using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Recipe 
{
    public string recipeName;
    public GameObject resultPrefab;
    public RecipeRequirement[] requirements;
    
    [System.Serializable]
    public class RecipeRequirement
    {
        public StackableType type;
        public int amount;
    }
}
