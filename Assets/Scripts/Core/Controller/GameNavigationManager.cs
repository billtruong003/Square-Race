using UnityEngine;

public class GameNavigationManager : Singleton<GameNavigationManager>
{
    public LevelData SelectedLevel { get; private set; }
    [SerializeField] private LevelData _defaultLevel;

    protected override void Awake()
    {
        base.Awake();
        if (SelectedLevel == null && _defaultLevel != null)
            SelectedLevel = _defaultLevel;
    }

    public void SetCurrentLevel(LevelData level)
    {
        SelectedLevel = level;
    }
}