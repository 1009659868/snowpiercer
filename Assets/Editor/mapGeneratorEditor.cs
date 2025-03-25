// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEditor;

// [CustomEditor(typeof(MapGenerator_V3))]//指定MapGenerator_V3的自定义编辑器
// public class mapGeneratorEditor : Editor
// {
//     public override void OnInspectorGUI()
//     {
//         MapGenerator_V3 mapGenerator = (MapGenerator_V3)target;
//         if(DrawDefaultInspector()){
//             if(mapGenerator.autoUpdate){
//                 mapGenerator.GenerateMap();
//             }
//         }
//         if(GUILayout.Button("Generate Map")){
//             mapGenerator.GenerateMap();
//         }
//     }
// }
