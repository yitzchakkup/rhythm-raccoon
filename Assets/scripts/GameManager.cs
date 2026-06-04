using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // This class no longer needs to hold a direct reference to the UI.
    // It only needs to trigger the EndGame state.

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    public void EndGame()
    {
        Debug.Log("Game Over!");
        Time.timeScale = 0f;
        
        // Find the UI from the static reference and activate it.
        if (SceneUIRefs.Instance != null && SceneUIRefs.gameOverUI != null)
        {
            SceneUIRefs.gameOverUI.SetActive(true);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}