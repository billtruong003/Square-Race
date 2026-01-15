using UnityEngine;
using System.Collections.Generic;

public enum GridAnchor
{
    Center,
    Top,
    Bottom,
    Left,
    Right,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public class GridLayoutManager : MonoBehaviour
{
    [Header("Layout Settings")]
    [SerializeField] private int _columns = 10;
    [SerializeField] private Vector2 _spacing = new Vector2(1.5f, 1.5f);
    [SerializeField] private GridAnchor _anchor = GridAnchor.Center;

    [ContextMenu("Execute Layout")]
    public void LayoutChildren()
    {
        List<Transform> children = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf) children.Add(child);
        }

        int count = children.Count;
        if (count == 0) return;

        int safeColumns = Mathf.Max(1, _columns);
        int rows = Mathf.CeilToInt((float)count / safeColumns);
        int actualColumns = Mathf.Min(count, safeColumns);

        float totalWidth = (actualColumns - 1) * _spacing.x;
        float totalHeight = (rows - 1) * _spacing.y;

        Vector2 startPos = GetStartPosition(totalWidth, totalHeight);

        for (int i = 0; i < count; i++)
        {
            int row = i / safeColumns;
            int col = i % safeColumns;

            Vector2 offset = new Vector2(col * _spacing.x, -row * _spacing.y);
            children[i].localPosition = startPos + offset;
        }
    }

    private Vector2 GetStartPosition(float width, float height)
    {
        switch (_anchor)
        {
            case GridAnchor.TopLeft:
                return new Vector2(0, 0);

            case GridAnchor.Top:
                return new Vector2(-width * 0.5f, 0);

            case GridAnchor.TopRight:
                return new Vector2(-width, 0);

            case GridAnchor.Left:
                return new Vector2(0, height * 0.5f);

            case GridAnchor.Center:
                return new Vector2(-width * 0.5f, height * 0.5f);

            case GridAnchor.Right:
                return new Vector2(-width, height * 0.5f);

            case GridAnchor.BottomLeft:
                return new Vector2(0, height);

            case GridAnchor.Bottom:
                return new Vector2(-width * 0.5f, height);

            case GridAnchor.BottomRight:
                return new Vector2(-width, height);

            default:
                return Vector2.zero;
        }
    }
}