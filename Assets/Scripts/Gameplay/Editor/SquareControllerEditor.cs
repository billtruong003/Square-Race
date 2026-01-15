using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SquareController))]
[CanEditMultipleObjects]
public class SquareControllerEditor : Editor
{
    private SerializedProperty _startAngleProp;
    private SerializedProperty _rayDistProp;

    private void OnEnable()
    {
        _startAngleProp = serializedObject.FindProperty("_startAngle");
        _rayDistProp = serializedObject.FindProperty("_rayDistance");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);

        // Slider chỉnh hướng nhanh
        EditorGUILayout.PropertyField(_startAngleProp, new GUIContent("Start Direction Angle"));

        serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI()
    {
        SquareController t = (SquareController)target;
        Transform tr = t.transform;

        // Vẽ vòng tròn điều hướng
        EditorGUI.BeginChangeCheck();

        float currentAngle = _startAngleProp.floatValue;
        float rad = currentAngle * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Sin(-rad), Mathf.Cos(rad), 0);

        Handles.color = new Color(1f, 0.5f, 0f, 0.8f);
        Handles.DrawWireDisc(tr.position, Vector3.forward, 1.5f);

        // Tay cầm xoay hướng
        Quaternion rot = Quaternion.Euler(0, 0, currentAngle);
        Quaternion newRot = Handles.Disc(rot, tr.position, Vector3.forward, 1.5f, false, 0.1f);

        if (EditorGUI.EndChangeCheck())
        {
            float newAngle = newRot.eulerAngles.z;
            _startAngleProp.floatValue = newAngle;
            serializedObject.ApplyModifiedProperties();
        }

        // Vẽ mũi tên hướng di chuyển
        Handles.color = Color.cyan;
        float rayDist = _rayDistProp.floatValue;
        Handles.ArrowHandleCap(0, tr.position, Quaternion.LookRotation(direction), rayDist + 1f, EventType.Repaint);
    }
}