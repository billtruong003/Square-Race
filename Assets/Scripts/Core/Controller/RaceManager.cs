using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RaceManager : Singleton<RaceManager>
{
    [Header("Configuration")]
    [SerializeField] private List<SquareController> _racers = new List<SquareController>();

    [Header("Debug / Fallback")]
    [SerializeField] private LevelData _debugLevelData;

    [Header("Game Rules")]
    [SerializeField] private int _maxRankSlots = 3;
    [SerializeField] private float _suddenDeathDelay = 0.2f;
    [SerializeField] private float _endPanelDelay = 2.0f;

    private bool _isRaceActive = false;
    private int _finishedCount = 0;
    private int _aliveCount = 0;
    private GameMode _currentMode = GameMode.Racing;

    private void Start()
    {
        if (_racers == null || _racers.Count == 0 || _racers.Any(r => r == null))
        {
            _racers = FindObjectsByType<SquareController>(FindObjectsSortMode.None).ToList();
        }

        InitializeRace();
    }

    public List<SquareController> GetAllRacers() => _racers;

    private void InitializeRace()
    {
        _finishedCount = 0;

        LevelData dataToLoad = null;

        if (GameNavigationManager.Instance != null && GameNavigationManager.Instance.SelectedLevel != null)
        {
            dataToLoad = GameNavigationManager.Instance.SelectedLevel;
        }
        else if (_debugLevelData != null)
        {
            dataToLoad = _debugLevelData;
        }

        if (dataToLoad != null)
        {
            _currentMode = dataToLoad.Mode;
            if (ColorManager.Instance != null)
            {
                ColorManager.Instance.SetRaceConfig(dataToLoad);
            }
        }

        foreach (var racer in _racers)
        {
            if (racer != null)
            {
                racer.gameObject.SetActive(true);
                racer.Initialize();
            }
        }

        _aliveCount = _racers.Count;

        if (ColorManager.Instance != null)
        {
            ColorManager.Instance.ApplyToRacers(_racers);
        }

        if (GameUIController.Instance != null)
        {
            GameUIController.Instance.SetupInitialState(OnReadyPressed);

            if (_currentMode == GameMode.LastManStanding)
            {
                GameUIController.Instance.InitializeLMSBoard(_racers);
                GameUIController.Instance.UpdateSurvivorCount(_aliveCount);
            }
        }

        GameManager.Instance.SetGameState(GameState.Staging);
    }

    public void OnReadyPressed()
    {
        if (GameUIController.Instance != null)
            GameUIController.Instance.OffReadyButton();

        StartCoroutine(StartSequence());
    }

    private IEnumerator StartSequence()
    {
        string[] counts = { "3", "2", "1", "GO!" };
        foreach (var c in counts)
        {
            if (GameUIController.Instance != null)
                GameUIController.Instance.UpdateCountdown(c, true);
            yield return new WaitForSeconds(1f);
        }

        if (GameUIController.Instance != null)
            GameUIController.Instance.UpdateCountdown("", false);

        StartRace();
    }

    private void StartRace()
    {
        _isRaceActive = true;
        GameManager.Instance.SetGameState(GameState.Playing);
        foreach (var racer in _racers) if (racer != null) racer.StartEngine();
    }

    public void NotifyRacerFinished(SquareController racer)
    {
        if (!_isRaceActive || _currentMode != GameMode.Racing) return;

        _finishedCount++;
        racer.gameObject.SetActive(false);

        if (GameUIController.Instance != null)
            GameUIController.Instance.AddRankEntry(_finishedCount, racer.name, racer.GetSprite(), racer.GetColor());

        if (_finishedCount >= _maxRankSlots)
        {
            StartCoroutine(EndGameSequence());
        }
    }

    public void NotifyRacerDied(SquareController racer)
    {
        if (!_isRaceActive) return;

        _aliveCount--;

        if (_currentMode == GameMode.LastManStanding)
        {
            if (GameUIController.Instance != null)
            {
                GameUIController.Instance.UpdateRacerStatus(racer.name, true);
                GameUIController.Instance.UpdateSurvivorCount(_aliveCount);
            }

            if (_aliveCount <= 1)
            {
                StartCoroutine(EndGameSequence());
            }
        }
    }

    private IEnumerator EndGameSequence()
    {
        _isRaceActive = false;
        var remainingRacers = _racers.Where(r => r.gameObject.activeSelf && r.GetHealth() > 0).ToList();

        foreach (var racer in remainingRacers) racer.StopEngine();

        if (_currentMode == GameMode.Racing)
        {
            foreach (var racer in remainingRacers)
            {
                racer.TriggerDeactiveState();
                yield return new WaitForSeconds(_suddenDeathDelay);
            }
        }

        yield return new WaitForSeconds(_endPanelDelay);

        GameManager.Instance.SetGameState(GameState.GameOver);

        if (GameUIController.Instance != null)
            GameUIController.Instance.ShowEndPanel();
    }

#if UNITY_EDITOR
    [SerializeField] private GameObject _racerPrefab;
    [SerializeField] private Transform _racersContainer;
    [SerializeField] private int _spawnCount = 50;

    [ContextMenu("Spawn Racers")]
    public void Editor_SpawnRacers()
    {
        if (_racersContainer == null || _racerPrefab == null) return;

        var children = new List<GameObject>();
        foreach (Transform child in _racersContainer) children.Add(child.gameObject);
        foreach (var child in children) DestroyImmediate(child);

        _racers.Clear();
        for (int i = 0; i < _spawnCount; i++)
        {
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(_racerPrefab, _racersContainer);
            obj.name = $"Racer_{i + 1:000}";
            _racers.Add(obj.GetComponent<SquareController>());
        }
        EditorUtility.SetDirty(this);
    }
#endif
}