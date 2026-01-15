using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteInEditMode]
[RequireComponent(typeof(CompositeCollider2D))]
public class TilemapOutlineRenderer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _thickness = 0.05f;
    [SerializeField] private Color _color = Color.white;
    [SerializeField] private Material _material;
    [SerializeField] private bool _autoUpdate = true;

    [Header("Sorting")]
    [SerializeField] private string _sortingLayerName = "Default";
    [SerializeField] private int _sortingOrder = 1;

    private CompositeCollider2D _compositeCollider;
    private GameObject _outlineObject;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;

    private readonly List<Vector2> _pathPoints = new List<Vector2>();
    private readonly List<Vector3> _vertices = new List<Vector3>();
    private readonly List<int> _triangles = new List<int>();
    private readonly List<Color> _colors = new List<Color>();

    private void OnEnable()
    {
        _compositeCollider = GetComponent<CompositeCollider2D>();
        EnsureOutlineObject();
        Refresh();
    }

    private void OnValidate()
    {
        _compositeCollider = GetComponent<CompositeCollider2D>();
        EnsureOutlineObject();
        Refresh();
    }

    private void LateUpdate()
    {
        if (_autoUpdate && transform.hasChanged)
        {
            Refresh();
            transform.hasChanged = false;
        }
    }

    private void OnDisable()
    {
        if (_outlineObject != null)
        {
            if (Application.isPlaying) Destroy(_outlineObject);
            else DestroyImmediate(_outlineObject);
        }
    }

    public void Refresh()
    {
        if (_compositeCollider == null || _meshFilter == null) return;

        UpdateRendererSettings();
        GenerateMesh();
    }

    private void EnsureOutlineObject()
    {
        if (_outlineObject != null) return;

        Transform child = transform.Find("TilemapOutline");
        if (child != null)
        {
            _outlineObject = child.gameObject;
        }
        else
        {
            _outlineObject = new GameObject("TilemapOutline");
            _outlineObject.transform.SetParent(transform, false);
            _outlineObject.transform.localPosition = Vector3.zero;
            _outlineObject.transform.localScale = Vector3.one;
        }

        _meshFilter = _outlineObject.GetComponent<MeshFilter>();
        if (_meshFilter == null) _meshFilter = _outlineObject.AddComponent<MeshFilter>();

        _meshRenderer = _outlineObject.GetComponent<MeshRenderer>();
        if (_meshRenderer == null) _meshRenderer = _outlineObject.AddComponent<MeshRenderer>();

        if (_mesh == null)
        {
            _mesh = new Mesh { name = "OutlineMesh" };
            _meshFilter.sharedMesh = _mesh;
        }
    }

    private void UpdateRendererSettings()
    {
        if (_meshRenderer == null) return;

        if (_material != null)
        {
            _meshRenderer.sharedMaterial = _material;
        }
        else if (_meshRenderer.sharedMaterial == null)
        {
            _meshRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        _meshRenderer.sortingLayerName = _sortingLayerName;
        _meshRenderer.sortingOrder = _sortingOrder;
    }

    private void GenerateMesh()
    {
        _mesh.Clear();
        _vertices.Clear();
        _triangles.Clear();
        _colors.Clear();

        int pathCount = _compositeCollider.pathCount;

        for (int i = 0; i < pathCount; i++)
        {
            _pathPoints.Clear();
            _compositeCollider.GetPath(i, _pathPoints);
            AddPathToMesh(_pathPoints);
        }

        _mesh.SetVertices(_vertices);
        _mesh.SetTriangles(_triangles, 0);
        _mesh.SetColors(_colors);
        _mesh.RecalculateBounds();
    }

    private void AddPathToMesh(List<Vector2> path)
    {
        if (path.Count < 2) return;

        int startIndex = _vertices.Count;
        int pointCount = path.Count;
        float halfThickness = _thickness * 0.5f;

        for (int i = 0; i < pointCount; i++)
        {
            Vector2 current = path[i];
            Vector2 prev = path[(i - 1 + pointCount) % pointCount];
            Vector2 next = path[(i + 1) % pointCount];

            Vector2 tangent1 = (current - prev).normalized;
            Vector2 tangent2 = (next - current).normalized;
            Vector2 tangentSum = (tangent1 + tangent2).normalized;

            Vector2 miter = new Vector2(-tangentSum.y, tangentSum.x);
            float dot = Vector2.Dot(miter, new Vector2(-tangent1.y, tangent1.x));

            float miterLength = halfThickness / Mathf.Max(0.1f, dot);
            Vector2 offset = miter * miterLength;

            _vertices.Add(new Vector3(current.x + offset.x, current.y + offset.y, 0));
            _vertices.Add(new Vector3(current.x - offset.x, current.y - offset.y, 0));

            _colors.Add(_color);
            _colors.Add(_color);

            if (i < pointCount - 1)
            {
                int currentBase = startIndex + (i * 2);
                int nextBase = startIndex + ((i + 1) * 2);

                _triangles.Add(currentBase);
                _triangles.Add(nextBase);
                _triangles.Add(currentBase + 1);

                _triangles.Add(currentBase + 1);
                _triangles.Add(nextBase);
                _triangles.Add(nextBase + 1);
            }
        }

        int lastBase = startIndex + ((pointCount - 1) * 2);
        int firstBase = startIndex;

        _triangles.Add(lastBase);
        _triangles.Add(firstBase);
        _triangles.Add(lastBase + 1);

        _triangles.Add(lastBase + 1);
        _triangles.Add(firstBase);
        _triangles.Add(firstBase + 1);
    }
}