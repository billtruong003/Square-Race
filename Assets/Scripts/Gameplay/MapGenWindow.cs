using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class MazeMapGenerator : MonoBehaviour
{
    [BoxGroup("Configurations")]
    [SerializeField, Required, AssetsOnly]
    private List<TextAsset> _mazeJsonFiles;

    [BoxGroup("Configurations")]
    [SerializeField]
    private float _distanceBetweenMaps = 50f;

    [BoxGroup("Assets References")]
    [SerializeField, Required, AssetsOnly]
    private GameObject _wallPrefab;

    [BoxGroup("Assets References")]
    [SerializeField, AssetsOnly]
    private GameObject _startPointPrefab;

    [BoxGroup("Assets References")]
    [SerializeField, AssetsOnly]
    private GameObject _endPointPrefab;

    [BoxGroup("Physics")]
    [SerializeField]
    private PhysicsMaterial2D _wallPhysicsMaterial;

    [BoxGroup("Hierarchy")]
    [SerializeField]
    private Transform _rootContainer;

    [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1)]
    public void GenerateMazes()
    {
        Cleanup();

        if (!ValidateInputs()) return;

        for (int i = 0; i < _mazeJsonFiles.Count; i++)
        {
            if (_mazeJsonFiles[i] == null) continue;

            MazeSchema schema = ParseSchema(_mazeJsonFiles[i]);
            if (schema != null)
            {
                BuildSingleMaze(schema, i);
            }
        }
    }

    [Button(ButtonSizes.Medium), GUIColor(1, 0.4f, 0.4f)]
    public void Cleanup()
    {
        if (_rootContainer == null) _rootContainer = transform;

        var children = new List<GameObject>();
        foreach (Transform child in _rootContainer) children.Add(child.gameObject);
        children.ForEach(DestroyImmediate);
    }

    private bool ValidateInputs()
    {
        return _mazeJsonFiles != null && _mazeJsonFiles.Count > 0 && _wallPrefab != null;
    }

    private MazeSchema ParseSchema(TextAsset json)
    {
        try
        {
            return JsonUtility.FromJson<MazeSchema>(json.text);
        }
        catch
        {
            return null;
        }
    }

    private void BuildSingleMaze(MazeSchema schema, int index)
    {
        GameObject mapParent = new GameObject($"Maze_Level_{index + 1}");
        mapParent.transform.SetParent(_rootContainer);
        mapParent.transform.position = new Vector3(index * _distanceBetweenMaps, 0, 0);

        Vector2 offset = new Vector2(-schema.width / 2f, schema.height / 2f);

        for (int y = 0; y < schema.height; y++)
        {
            for (int x = 0; x < schema.width; x++)
            {
                if (IsWall(x, y, schema))
                {
                    Vector3 pos = new Vector3(x + offset.x + 0.5f, -(y - offset.y) - 0.5f, 0);
                    CreateWall(pos, mapParent.transform);
                }
            }
        }

        SpawnSpecialPoints(schema, mapParent.transform, offset);
    }

    private bool IsWall(int x, int y, MazeSchema schema)
    {
        // 1. HARD BORDERS (Priority)
        // Top & Bottom: 2 layers
        if (y < 2 || y >= schema.height - 2) return true;

        // Left: 2 layers
        if (x < 2) return true;

        // Right: 3 layers
        if (x >= schema.width - 3) return true;

        // 2. INNER MAZE (From JSON)
        int dataIndex = y * schema.width + x;
        if (dataIndex >= 0 && dataIndex < schema.data.Length)
        {
            return schema.data[dataIndex] == 1;
        }

        return false;
    }

    private void CreateWall(Vector3 localPos, Transform parent)
    {
        GameObject wall = Instantiate(_wallPrefab, parent);
        wall.transform.localPosition = localPos;

        var collider = wall.GetComponent<BoxCollider2D>();
        if (collider == null) collider = wall.AddComponent<BoxCollider2D>();

        if (_wallPhysicsMaterial != null)
        {
            collider.sharedMaterial = _wallPhysicsMaterial;
        }
    }

    private void SpawnSpecialPoints(MazeSchema schema, Transform parent, Vector2 offset)
    {
        // Start Point: Top-Left of Inner Area (x=2, y=2)
        if (_startPointPrefab != null)
        {
            Vector3 startPos = new Vector3(2 + offset.x + 0.5f, -(2 - offset.y) - 0.5f, 0);
            GameObject startObj = Instantiate(_startPointPrefab, parent);
            startObj.transform.localPosition = startPos;
            startObj.name = "Start_Point";
        }

        // End Point: Bottom-Right of Inner Area (x=Width-4, y=Height-3)
        // Width-4 because Right border is 3 layers (Indices: Width-1, Width-2, Width-3 are walls)
        // Height-3 because Bottom border is 2 layers (Indices: Height-1, Height-2 are walls)
        if (_endPointPrefab != null)
        {
            int endX = schema.width - 4;
            int endY = schema.height - 3;
            Vector3 endPos = new Vector3(endX + offset.x + 0.5f, -(endY - offset.y) - 0.5f, 0);

            GameObject endObj = Instantiate(_endPointPrefab, parent);
            endObj.transform.localPosition = endPos;
            endObj.name = "End_Point";
        }
    }

    private void OnValidate()
    {
        if (_rootContainer == null) _rootContainer = transform;
    }
}

[Serializable]
public class MazeSchema
{
    public int width;
    public int height;
    public int[] data;
}

