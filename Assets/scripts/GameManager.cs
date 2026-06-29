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

    // --- NEW: Listen for scene reloads to forcefully wipe the UI ---
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"<color=cyan>[GameManager]</color> New Scene Loaded: {scene.name}. Attempting to force all End Screens OFF.");
        
        // 1. Force time back to normal just in case!
        Time.timeScale = 1f;

        // 2. Hide all the screens so we start fresh
        if (SceneUIRefs.multiplayerEndLayout != null) SceneUIRefs.multiplayerEndLayout.SetActive(false);
        if (SceneUIRefs.singlePlayerEndLayout != null) SceneUIRefs.singlePlayerEndLayout.SetActive(false);
        if (SceneUIRefs.sharedEndGameLayout != null) SceneUIRefs.sharedEndGameLayout.SetActive(false);
        if (SceneUIRefs.possumWinBackground != null) SceneUIRefs.possumWinBackground.SetActive(false);
        if (SceneUIRefs.raccoonWinBackground != null) SceneUIRefs.raccoonWinBackground.SetActive(false);
        if (SceneUIRefs.offlineLoseBackground != null) SceneUIRefs.offlineLoseBackground.SetActive(false);
    }

    public void EndGame(int finalScore = 0)
    {
        Debug.Log("<color=red>[GameManager]</color> EndGame() was called! Freezing logic.");
        
        WordGenerator wg = FindAnyObjectByType<WordGenerator>();
        if (wg != null) wg.enabled = false;

        PowerupGenerator pg = FindAnyObjectByType<PowerupGenerator>();
        if (pg != null) pg.enabled = false;

        if (MultiplayerMatchManager.Instance != null && MultiplayerMatchManager.Instance.IsMultiplayerGame())
        {
            Debug.Log("<color=red>[GameManager]</color> Diverting to Multiplayer End Screen...");
            if (SceneUIRefs.multiplayerEndLayout != null) SceneUIRefs.multiplayerEndLayout.SetActive(true);
            if (SceneUIRefs.singlePlayerEndLayout != null) SceneUIRefs.singlePlayerEndLayout.SetActive(false);
        }
        else
        {
            Debug.Log("<color=red>[GameManager]</color> Opening Single Player End Screen...");
            if (SceneUIRefs.singlePlayerEndLayout != null) 
            {
                SceneUIRefs.singlePlayerEndLayout.SetActive(true);
                SinglePlayerEndScreen endScreen = SceneUIRefs.singlePlayerEndLayout.GetComponent<SinglePlayerEndScreen>();
                if (endScreen != null) endScreen.SetupScreen(finalScore);
            }

            if (SceneUIRefs.offlineLoseBackground != null) SceneUIRefs.offlineLoseBackground.SetActive(true);
            if (SceneUIRefs.multiplayerEndLayout != null) SceneUIRefs.multiplayerEndLayout.SetActive(false);
        }
    }

    public void EndGameMultiplayer()
    {
        Debug.Log("<color=red>[GameManager]</color> EndGameMultiplayer() was called! Freezing logic and calculating winner.");
        
        WordGenerator wg = FindAnyObjectByType<WordGenerator>();
        if (wg != null) wg.enabled = false;

        PowerupGenerator pg = FindAnyObjectByType<PowerupGenerator>();
        if (pg != null) pg.enabled = false;

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
                Debug.Log("<color=magenta>[GameManager]</color> Turning ON the Multiplayer End Layout!");
                SceneUIRefs.multiplayerEndLayout.SetActive(true);
            }
        }
        else
        {
            Debug.LogError("Cannot determine multiplayer winner: MultiplayerMatchManager not found.");
            EndGame();
        }
    }

    public void RequestMultiplayerReplay()
    {
        Debug.Log("<color=blue>[GameManager]</color> RequestMultiplayerReplay called! Initiating Soft Reset...");
        Time.timeScale = 1f; 

        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            // INSTEAD of reloading the scene, we trigger a Soft Reset across the network!
            if (MultiplayerMatchManager.Instance != null)
            {
                MultiplayerMatchManager.Instance.TriggerSoftReset();
            }
        }
    }

    public void RestartCurrentLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
        isDisconnecting = false;
        SceneManager.LoadScene(0); 
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}