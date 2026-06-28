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

    public void EndGame()
    {
        Debug.Log("Game Over!");
        Time.timeScale = 0f;

        if (MultiplayerMatchManager.Instance != null && MultiplayerMatchManager.Instance.IsMultiplayerGame())
        {
            if (SceneUIRefs.multiplayerEndLayout != null) SceneUIRefs.multiplayerEndLayout.SetActive(true);
            if (SceneUIRefs.singlePlayerEndLayout != null) SceneUIRefs.singlePlayerEndLayout.SetActive(false);
        }
        else
        {
            if (SceneUIRefs.singlePlayerEndLayout != null) SceneUIRefs.singlePlayerEndLayout.SetActive(true);
            if (SceneUIRefs.multiplayerEndLayout != null) SceneUIRefs.multiplayerEndLayout.SetActive(false);
        }
    }

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
            float myScore = MultiplayerMatchManager.Instance.GetMyScore();
            float opponentScore = MultiplayerMatchManager.Instance.GetOpponentScore();

            bool iWon = myScore >= opponentScore;
            bool iAmPossum = PhotonNetwork.IsMasterClient;
            bool possumWon = (iAmPossum && iWon) || (!iAmPossum && !iWon);

            if (possumWon)
            {
                if (SceneUIRefs.possumWinBackground != null) SceneUIRefs.possumWinBackground.SetActive(true);
            }
            else
            {
                if (SceneUIRefs.raccoonWinBackground != null) SceneUIRefs.raccoonWinBackground.SetActive(true);
            }

            if (SceneUIRefs.multiplayerEndLayout != null)
            {
                SceneUIRefs.multiplayerEndLayout.SetActive(true);
            }
        }
        else
        {
            Debug.LogError("Cannot determine multiplayer winner: MultiplayerMatchManager not found.");
            EndGame();
        }
    }

    // --- UPDATED: Communicates with MatchSyncManager to register the replay vote ---
    public void RequestMultiplayerReplay()
    {
        // Unpause the local engine immediately so network syncing and scene loading can process smoothly
        Time.timeScale = 1f;

        if (MatchSyncManager.Instance != null)
        {
            // Set local custom property to alert the Master Client we are ready
            MatchSyncManager.Instance.LocalPlayerWantsToPlayAgain();
            Debug.Log("Replay requested. Waiting for opponent...");
        }
        else
        {
            Debug.LogError("MatchSyncManager Instance not found! Cannot sync replay request.");
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

        if (SceneUIRefs.possumWinBackground != null) SceneUIRefs.possumWinBackground.SetActive(false);
        if (SceneUIRefs.raccoonWinBackground != null) SceneUIRefs.raccoonWinBackground.SetActive(false);
        if (SceneUIRefs.offlineLoseBackground != null) SceneUIRefs.offlineLoseBackground.SetActive(false);
        if (SceneUIRefs.sharedEndGameLayout != null) SceneUIRefs.sharedEndGameLayout.SetActive(false);
        if (SceneUIRefs.singlePlayerEndLayout != null) SceneUIRefs.singlePlayerEndLayout.SetActive(false);
        if (SceneUIRefs.multiplayerEndLayout != null) SceneUIRefs.multiplayerEndLayout.SetActive(false);



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