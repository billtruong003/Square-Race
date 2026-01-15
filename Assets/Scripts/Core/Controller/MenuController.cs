using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public void OnPlayPressed()
    {
        SceneManager.LoadScene(GameConstants.MapSelectScene);
    }

    public void OnQuitPressed()
    {
        GameManager.Instance.QuitGame();
    }
}