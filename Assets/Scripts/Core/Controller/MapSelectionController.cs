using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class MapSelectionController : MonoBehaviour
{
    [SerializeField] private List<LevelData> _allLevels;
    [SerializeField] private GameObject _itemPrefab;
    [SerializeField] private Transform _gridContainer;
    [SerializeField] private int _itemsPerPage = 100;

    [SerializeField] private Button _playButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _prevButton;
    [SerializeField] private TextMeshProUGUI _pageInfoText;

    private readonly List<LevelItemUI> _itemPool = new List<LevelItemUI>();
    private int _currentPage = 0;
    private int _totalPages;
    private LevelData _currentSelectedLevel;

    private void Start()
    {
        InitializePool();
        CalculatePagination();
        SetupUI();
        RefreshDisplay();
        GameNavigationManager.Instance.SetCurrentLevel(null);
    }

    private void SetupUI()
    {
        if (_playButton != null)
        {
            _playButton.onClick.AddListener(OnPlayButtonPressed);
            _playButton.interactable = false;
        }

        _nextButton.onClick.AddListener(OnNextPressed);
        _prevButton.onClick.AddListener(OnPrevPressed);
    }

    private void InitializePool()
    {
        for (int i = 0; i < _itemsPerPage; i++)
        {
            GameObject obj = Instantiate(_itemPrefab, _gridContainer);
            LevelItemUI item = obj.GetComponent<LevelItemUI>();
            item.Initialize(this);
            item.Deactivate();
            _itemPool.Add(item);
        }
    }

    private void CalculatePagination()
    {
        _totalPages = Mathf.CeilToInt((float)_allLevels.Count / _itemsPerPage);
        if (_totalPages == 0) _totalPages = 1;
    }

    private void RefreshDisplay()
    {
        int startIndex = _currentPage * _itemsPerPage;

        for (int i = 0; i < _itemsPerPage; i++)
        {
            int dataIndex = startIndex + i;
            LevelItemUI itemUI = _itemPool[i];

            if (dataIndex < _allLevels.Count)
            {
                LevelData data = _allLevels[dataIndex];
                itemUI.Setup(data, _currentSelectedLevel == data);
            }
            else
            {
                itemUI.Deactivate();
            }
        }

        UpdateNavigationUI();
    }

    private void UpdateNavigationUI()
    {
        _pageInfoText.text = $"Page {_currentPage + 1}/{_totalPages}";
        _prevButton.interactable = _currentPage > 0;
        _nextButton.interactable = _currentPage < _totalPages - 1;
    }

    public void OnLevelSelected(LevelData clickedData)
    {
        if (_currentSelectedLevel == clickedData)
        {
            DeselectCurrent();
        }
        else
        {
            SelectMap(clickedData);
        }
        RefreshVisuals();
    }

    private void SelectMap(LevelData data)
    {
        _currentSelectedLevel = data;
        GameNavigationManager.Instance.SetCurrentLevel(data);
        if (_playButton != null) _playButton.interactable = true;
    }

    private void DeselectCurrent()
    {
        _currentSelectedLevel = null;
        GameNavigationManager.Instance.SetCurrentLevel(null);
        if (_playButton != null) _playButton.interactable = false;
    }

    private void RefreshVisuals()
    {
        foreach (var item in _itemPool)
        {
            if (item.gameObject.activeSelf)
                item.SetSelectionState(item.GetData() == _currentSelectedLevel);
        }
    }

    public void OnPlayButtonPressed()
    {
        if (_currentSelectedLevel != null)
        {
            string sceneToLoad = !string.IsNullOrEmpty(_currentSelectedLevel.StagingSceneName)
                ? _currentSelectedLevel.StagingSceneName
                : GameConstants.StagingScene;

            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
        }
    }

    public void OnNextPressed()
    {
        if (_currentPage < _totalPages - 1)
        {
            _currentPage++;
            RefreshDisplay();
        }
    }

    public void OnPrevPressed()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            RefreshDisplay();
        }
    }
}