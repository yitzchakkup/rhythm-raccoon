using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private bool isDisconnecting = false;

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

    // This is the original method for Single Player, left untouched.
    public void EndGame()
    {
        Debug.Log("Game Over!");
        Time.timeScale = 0f;
        
        if (SceneUIRefs.gameOverUI != null)
        {
            SceneUIRefs.gameOverUI.SetActive(true);
        }
    }
    
    /// <summary>
    /// Ends the multiplayer match, determines win/loss, and displays the appropriate screen.
    /// </summary>
    public void EndGameMultiplayer()
    {
        Time.timeScale = 0f;

        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
        }

        if (MultiplayerMatchManager.Instance != null)
        {
            int myScore = MultiplayerMatchManager.Instance.GetMyScore();
            int opponentScore = MultiplayerMatchManager.Instance.GetOpponentScore();

            // Determine the winner and show the correct screen using the SceneUIRefs bridge.
            if (myScore >= opponentScore)
            {
                if (SceneUIRefs.winUI != null)
                {
                    SceneUIRefs.winUI.SetActive(true);
                }
            }
            else
            {
                if (SceneUIRefs.loseUI != null)
                {
                    SceneUIRefs.loseUI.SetActive(true);
                }
            }
        }
        else
        {
            Debug.LogError("Cannot determine multiplayer winner: MultiplayerMatchManager not found.");
            // As a fallback, you could show a generic game over screen
            EndGame();
        }
    }

    public void ReturnToMainMenu()
    {
        if (isDisconnecting) return;
        StartCoroutine(ReturnToMainMenuRoutine());
    }

    private IEnumerator ReturnToMainMenuRoutine()
    {
        isDisconnecting = true;
        Time.timeScale = 1f;

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            while (PhotonNetwork.IsConnected) { yield return null; }
        }

        PhotonNetwork.OfflineMode = false;

        if (SceneUIRefs.winUI != null) SceneUIRefs.winUI.SetActive(false);
        if (SceneUIRefs.loseUI != null) SceneUIRefs.loseUI.SetActive(false);
        if (SceneUIRefs.gameOverUI != null) SceneUIRefs.gameOverUI.SetActive(false);

        isDisconnecting = false;
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
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