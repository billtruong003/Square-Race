using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameUIController : Singleton<GameUIController>
{
    [SerializeField] private GameObject _startPanel;
    [SerializeField] private GameObject _gamePanel;
    [SerializeField] private GameObject _endPanel;
    [SerializeField] private DynamicRankLayout _rankingLayout;
    [SerializeField] private TextMeshProUGUI _countdownText;
    [SerializeField] private TextMeshProUGUI _aliveCountText;
    [SerializeField] private Button _readyButton;
    [SerializeField] private Button _replayButton;
    [SerializeField] private Button _menuButton;

    protected override void Awake()
    {
        base.Awake();
        _replayButton.onClick.AddListener(OnReplayPressed);
        _menuButton.onClick.AddListener(OnMenuPressed);
    }

    public void SetupInitialState(UnityEngine.Events.UnityAction onReadyAction)
    {
        _startPanel.SetActive(true);
        _gamePanel.SetActive(true);
        _endPanel.SetActive(false);
        _countdownText.gameObject.SetActive(false);
        if (_aliveCountText != null) _aliveCountText.gameObject.SetActive(false);

        if (_rankingLayout != null) _rankingLayout.Clear();

        _readyButton.onClick.RemoveAllListeners();
        _readyButton.onClick.AddListener(onReadyAction);
        _readyButton.gameObject.SetActive(true);
    }

    public void InitializeLMSBoard(List<SquareController> racers)
    {
        if (_rankingLayout == null) return;
        _rankingLayout.Clear();

        foreach (var racer in racers)
        {
            _rankingLayout.AddPermanentEntry(racer.name, racer.GetSprite(), racer.GetColor());
        }

        if (_aliveCountText != null) _aliveCountText.gameObject.SetActive(true);
    }

    public void UpdateRacerStatus(string racerName, bool isDead)
    {
        if (_rankingLayout != null)
        {
            _rankingLayout.SetEntryStatus(racerName, isDead);
        }
    }

    public void UpdateSurvivorCount(int count)
    {
        if (_aliveCountText != null)
        {
            _aliveCountText.text = $"Alive: {count}";
        }
    }

    public void UpdateCountdown(string text, bool active)
    {
        _countdownText.gameObject.SetActive(active);
        _countdownText.text = text;
        if (!active) _startPanel.SetActive(false);
    }

    public void AddRankEntry(int rank, string name, Sprite sprite, Color color)
    {
        if (_rankingLayout != null)
            _rankingLayout.AddNewEntry($"#{rank} {name}", sprite, color);
    }

    public void ShowEndPanel() => _endPanel.SetActive(true);
    public void OffReadyButton() => _readyButton.gameObject.SetActive(false);

    private void OnReplayPressed() => GameManager.Instance.RestartLevel();
    private void OnMenuPressed() => GameManager.Instance.ReturnToMenu();
}