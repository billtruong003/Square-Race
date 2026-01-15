using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class StagingManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private GameObject _visualPrefab;
    [SerializeField] private Transform _container;

    // Runtime Preview Data
    private int _previewCount = 20;
    private LevelData _currentLevelData;

    private void Start()
    {
        LoadLevelConfig();
        GeneratePreview();
    }

    private void LoadLevelConfig()
    {
        if (GameNavigationManager.Instance != null && GameNavigationManager.Instance.SelectedLevel != null)
        {
            _currentLevelData = GameNavigationManager.Instance.SelectedLevel;
            _previewCount = _currentLevelData.StagingRacerCount > 0 ? _currentLevelData.StagingRacerCount : 20;
        }
    }

    public void GeneratePreview()
    {
        ClearContainer();

        if (_visualPrefab == null || _container == null) return;

        // Dummy list giả lập để truyền vào ColorManager
        List<SquareController> dummyRacers = new List<SquareController>();
        List<SquareVisual> createdVisuals = new List<SquareVisual>();

        // 1. Spawn Objects
        for (int i = 0; i < _previewCount; i++)
        {
            GameObject obj = Instantiate(_visualPrefab, _container);
            if (obj.TryGetComponent<SquareVisual>(out var visual))
            {
                createdVisuals.Add(visual);
            }
        }

        if (_container.TryGetComponent<GridLayoutManager>(out var layout))
        {
            layout.LayoutChildren();
        }

        // 2. Apply Colors using ColorManager Logic
        // Trick: Chúng ta không có SquareController thật ở đây, nên ta sẽ mượn logic tính toán
        // Nhưng vì ColorManager được thiết kế nhận List<SquareController>, 
        // ở Staging ta nên tự mô phỏng logic đó để nhẹ nhàng hơn, hoặc refactor ColorManager tách logic tính màu ra.
        // Tuy nhiên để giữ đúng yêu cầu "tập trung ColorManager", ta sẽ set data cho ColorManager trước.

        if (ColorManager.Instance != null && _currentLevelData != null)
        {
            ColorManager.Instance.SetRaceConfig(_currentLevelData); // Load data vào manager

            // Manual Apply cho Visuals (Vì Staging dùng Visual không dùng Controller đầy đủ)
            ApplyPreviewVisuals(createdVisuals);
        }
    }

    private void ApplyPreviewVisuals(List<SquareVisual> visuals)
    {
        if (_currentLevelData == null) return;

        for (int i = 0; i < visuals.Count; i++)
        {
            Color color = Color.white;
            Sprite shape = null;

            // Logic màu y hệt ColorManager (DRY - Don't Repeat Yourself: Nếu dự án lớn nên tách hàm GetColor(index) ra public)
            if (_currentLevelData.ColorMode == ColorAssignmentMode.FixedSequence &&
                _currentLevelData.FixedColorSequence != null &&
                _currentLevelData.FixedColorSequence.Count > 0)
            {
                color = _currentLevelData.FixedColorSequence[i % _currentLevelData.FixedColorSequence.Count];
            }
            else if (_currentLevelData.RacerColorGradient != null)
            {
                float t = i * (1f / Mathf.Max(1, visuals.Count - 1));
                color = _currentLevelData.RacerColorGradient.Evaluate(t);
            }

            if (_currentLevelData.RacerShapes != null && _currentLevelData.RacerShapes.Count > 0)
            {
                shape = _currentLevelData.RacerShapes[i % _currentLevelData.RacerShapes.Count];
            }

            visuals[i].Setup(i + 1, shape, color, 3.5f);
        }
    }

    private void ClearContainer()
    {
        if (_container == null) return;
        foreach (Transform child in _container) Destroy(child.gameObject);
    }

    public void OnStartRacePressed()
    {
        // Data đã được set vào ColorManager ở Start -> LoadLevelConfig rồi
        // Nhưng set lại lần nữa cho chắc chắn trước khi chuyển Scene
        if (ColorManager.Instance != null && _currentLevelData != null)
        {
            ColorManager.Instance.SetRaceConfig(_currentLevelData);
        }

        string targetScene = (_currentLevelData != null) ? _currentLevelData.SceneName : "GameMap1";
        SceneManager.LoadScene(targetScene);
    }
}