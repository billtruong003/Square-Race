using UnityEngine;
using System.Collections.Generic;

public class DynamicRankLayout : MonoBehaviour
{
    [SerializeField] private RankingEntryUI _prefab;
    [SerializeField] private int _maxVisibleItems = 5;
    [SerializeField] private float _itemHeight = 60f;
    [SerializeField] private float _spacing = 10f;
    [SerializeField] private float _animationSpeed = 10f;
    [SerializeField] private Sprite _deathIcon;

    private readonly List<RankingEntryUI> _activeItems = new List<RankingEntryUI>();
    private readonly Dictionary<string, RankingEntryUI> _entryMap = new Dictionary<string, RankingEntryUI>();
    private readonly Stack<RankingEntryUI> _pool = new Stack<RankingEntryUI>();

    private void Awake()
    {
        InitializePool();
    }

    public void Clear()
    {
        _entryMap.Clear();
        while (_activeItems.Count > 0)
        {
            RecycleItem(_activeItems[0]);
            _activeItems.RemoveAt(0);
        }
    }

    // Used for Racing Mode
    public void AddNewEntry(string name, Sprite sprite, Color color)
    {
        RankingEntryUI item = GetFromPool();
        item.UpdateInfo(name, sprite, color);
        item.gameObject.SetActive(true);
        item.transform.SetAsLastSibling();

        RectTransform rt = item.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, -(_itemHeight + _spacing));

        _activeItems.Insert(0, item);

        if (_activeItems.Count > _maxVisibleItems + 1)
        {
            var lastItem = _activeItems[_activeItems.Count - 1];
            RecycleItem(lastItem);
            _activeItems.RemoveAt(_activeItems.Count - 1);
        }
    }

    // Used for LMS Mode
    public void AddPermanentEntry(string id, Sprite sprite, Color color)
    {
        RankingEntryUI item = GetFromPool();
        item.UpdateInfo(id, sprite, color);
        item.gameObject.SetActive(true);

        RectTransform rt = item.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, -(_activeItems.Count * (_itemHeight + _spacing)));

        _activeItems.Add(item);
        _entryMap[id] = item;
    }

    public void SetEntryStatus(string id, bool isDead)
    {
        if (_entryMap.TryGetValue(id, out var item))
        {
            if (isDead)
            {
                item.UpdateInfo(id, _deathIcon, Color.gray);
            }
        }
    }

    private void Update()
    {
        ProcessLayoutAnimation();
    }

    private void ProcessLayoutAnimation()
    {
        float dt = Time.deltaTime;

        for (int i = 0; i < _activeItems.Count; i++)
        {
            RankingEntryUI item = _activeItems[i];
            RectTransform rt = item.GetComponent<RectTransform>();
            float targetY = -i * (_itemHeight + _spacing);

            // Only recycle if strictly in dynamic mode (Racing) and exceeding limit
            // For LMS, we keep the list or implement scrolling (simplified here to keep logic clean)

            Vector2 currentPos = rt.anchoredPosition;
            rt.anchoredPosition = Vector2.Lerp(currentPos, new Vector2(0, targetY), dt * _animationSpeed);
        }
    }

    private void InitializePool()
    {
        for (int i = 0; i < 20; i++)
        {
            CreatePoolItem();
        }
    }

    private RankingEntryUI CreatePoolItem()
    {
        RankingEntryUI item = Instantiate(_prefab, transform);
        item.gameObject.SetActive(false);
        RectTransform rt = item.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        _pool.Push(item);
        return item;
    }

    private RankingEntryUI GetFromPool()
    {
        return _pool.Count > 0 ? _pool.Pop() : CreatePoolItem();
    }

    private void RecycleItem(RankingEntryUI item)
    {
        item.gameObject.SetActive(false);
        _pool.Push(item);
    }
}