using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private bool isDisconnecting = false;
    
    [Header("Single Player Audio")]
    public AudioClip singlePlayerEndSting;
    public AudioClip singlePlayerEndMusic;

    [Header("Juice Prefabs")]
    public GameObject confettiPrefab;
    
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
        Debug.Log("<color=red>[GameManager]</color> EndGame() called!");
    
        // 1. Kill Switch
        WordGenerator wg = FindAnyObjectByType<WordGenerator>();
        if (wg != null) wg.enabled = false;
        PowerupGenerator pg = FindAnyObjectByType<PowerupGenerator>();
        if (pg != null) pg.enabled = false;

        // 2. Play Single Player Audio
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(singlePlayerEndSting);
            // Play the loop after the sting (assuming sting is ~2 seconds)
            StartCoroutine(PlayLoopAfterSting(singlePlayerEndMusic, 2.0f));
        }

        // 3. UI Logic
        if (SceneUIRefs.singlePlayerEndLayout != null) 
        {
            SceneUIRefs.singlePlayerEndLayout.SetActive(true);
            SinglePlayerEndScreen endScreen = SceneUIRefs.singlePlayerEndLayout.GetComponent<SinglePlayerEndScreen>();
            if (endScreen != null) endScreen.SetupScreen(finalScore);
        }
    
        if (SceneUIRefs.offlineLoseBackground != null) SceneUIRefs.offlineLoseBackground.SetActive(true);
    }

    private IEnumerator PlayLoopAfterSting(AudioClip loop, float delay)
    {
        yield return new WaitForSeconds(delay);
        AudioManager.Instance.PlayMusic(loop);
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

            // 1. Audio Logic (Self-Centered)
            // If my score is higher (or equal), I won. Period. 
            bool iWonTheMatch = myScore >= opponentScore;
            MultiplayerMatchManager.Instance.PlayEndGameAudio(iWonTheMatch);

            // 2. Visual Background Logic (Character-Centered)
            // Host is ALWAYS Possum. Guest is ALWAYS Raccoon.
            bool iAmHost = PhotonNetwork.IsMasterClient;
            
            // The Possum wins if: I am Host and I won, OR I am Guest and I lost.
            bool possumIsTheWinner = (iAmHost && iWonTheMatch) || (!iAmHost && !iWonTheMatch);

            if (possumIsTheWinner)
            {
                if (SceneUIRefs.possumWinBackground != null) SceneUIRefs.possumWinBackground.SetActive(true);
            }
            else
            {
                if (SceneUIRefs.raccoonWinBackground != null) SceneUIRefs.raccoonWinBackground.SetActive(true);
            }

            // Turn on the final UI overlay
            if (SceneUIRefs.multiplayerEndLayout != null)
            {
                SceneUIRefs.multiplayerEndLayout.SetActive(true);
            }
            
            
            if (iWonTheMatch && confettiPrefab != null)
            {
                // Left Fountain: Bottom-left corner, rotated 45 degrees to shoot toward the center
                Vector3 leftPos = new Vector3(-8.5f, -4.5f, 0f);
                Quaternion leftRot = Quaternion.Euler(-45f, 90f, -90f); // Adjust angles to point top-right
                GameObject leftFountain = Instantiate(confettiPrefab, leftPos, leftRot);

                // Right Fountain: Bottom-right corner, rotated to shoot toward the center
                Vector3 rightPos = new Vector3(8.5f, -4.5f, 0f);
                Quaternion rightRot = Quaternion.Euler(-135f, 90f, -90f); // Adjust angles to point top-left
                GameObject rightFountain = Instantiate(confettiPrefab, rightPos, rightRot);
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

        Debug.Log("<color=yellow>[GameManager]</color> Cleaning up network before returning to menu...");

        // 1. Leave the room if we are in one
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }

        // 2. Disconnect completely and wait for confirmation
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            while (PhotonNetwork.IsConnected) 
            { 
                yield return null; 
            }
        }

        // 3. Reset offline mode and load the menu
        PhotonNetwork.OfflineMode = false;
        isDisconnecting = false;
        
        Debug.Log("<color=green>[GameManager]</color> Disconnected cleanly. Loading Main Menu!");
        SceneManager.LoadScene("LobbyScene");
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}