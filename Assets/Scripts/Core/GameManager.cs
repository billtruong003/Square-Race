using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Menu,
    Staging,
    Playing,
    Paused,
    GameOver
}

public class GameManager : Singleton<GameManager>
{
    public GameState CurrentState { get; private set; } = GameState.Menu;
    private float _previousTimeScale = 1f;

    public void SetGameState(GameState newState)
    {
        CurrentState = newState;
        if (newState == GameState.Playing || newState == GameState.Menu)
        {
            Time.timeScale = 1f;
        }
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;

        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        CurrentState = GameState.Paused;
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;

        Time.timeScale = _previousTimeScale;
        CurrentState = GameState.Playing;
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        CurrentState = GameState.Menu;
        SceneManager.LoadScene(GameConstants.MenuScene);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        CurrentState = GameState.Playing;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}