#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TilemapOutlineRenderer))]
public class TilemapOutlineRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TilemapOutlineRenderer script = (TilemapOutlineRenderer)target;

        GUILayout.Space(10);
        if (GUILayout.Button("Force Rebuild Mesh"))
        {
            script.Refresh();
        }
    }
}
#endif