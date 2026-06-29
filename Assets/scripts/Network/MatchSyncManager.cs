using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class MatchSyncManager : MonoBehaviourPunCallbacks
{
    // --- SINGLETON SETUP ---
    public static MatchSyncManager Instance { get; private set; }

    [Header("Components")]
    public WordGenerator wordGenerator;
    
    [Header("Audio Settings")]
    [SerializeField] private AudioClip backgroundMusic;

    private bool matchStarted = false;
    private double exactStartTime = 0;
    
    private const string START_TIME_KEY = "MatchStartTime";
    private const string LOADED_KEY = "SceneLoaded"; 
    private const string TUT_DONE_KEY = "TutorialDone"; // Tracks when the slideshow finishes
    private const string PLAY_AGAIN_KEY = "PlayAgain";   // Tracks play again votes

    private void Awake()
    {
        // Enforce the Singleton pattern
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        if (AudioManager.Instance != null && backgroundMusic != null)
        {
            AudioManager.Instance.PlayMusic(backgroundMusic);
        }
        
        // Tell the network: "My scene has finished loading, and I am starting the tutorial!"
        // Also explicitly reset the PlayAgain property to false for a clean loop state.
        Hashtable props = new Hashtable 
        { 
            { LOADED_KEY, true }, 
            { TUT_DONE_KEY, false },
            { PLAY_AGAIN_KEY, false } 
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    // --- Public function called by your UI / GameManager button ---
    public void LocalPlayerWantsToPlayAgain()
    {
        Hashtable props = new Hashtable { { PLAY_AGAIN_KEY, true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    // --- Called by the InstructorManager when the slideshow finishes ---
    public void LocalPlayerFinishedTutorial()
    {
        Hashtable props = new Hashtable { { TUT_DONE_KEY, true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
    {
        // Listen for the Play Again vote from either client
        if (changedProps.ContainsKey(PLAY_AGAIN_KEY))
        {
            CheckIfAllPlayersWantToPlayAgain();
        }

        // The Master Client listens for everyone's loading AND tutorial status
        if (!PhotonNetwork.IsMasterClient || matchStarted || exactStartTime != 0) return;

        if (changedProps.ContainsKey(LOADED_KEY))
        {
            CheckIfAllPlayersLoaded();
        }

        if (changedProps.ContainsKey(TUT_DONE_KEY))
        {
            CheckIfAllPlayersFinishedTutorial();
        }
    }

    private void CheckIfAllPlayersLoaded()
    {
        foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
        {
            if (!p.CustomProperties.TryGetValue(LOADED_KEY, out object state) || !(bool)state) return;
        }

        // Both loaded! Tell all clients to start the visual slideshow.
        photonView.RPC("TriggerSlideshow_RPC", RpcTarget.All);
    }

    [PunRPC]
    private void TriggerSlideshow_RPC()
    {
        if (InstructorManager.Instance != null) 
        {
            InstructorManager.Instance.StartTutorialSequence();
        }
    }

    private void CheckIfAllPlayersFinishedTutorial()
    {
        foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
        {
            if (!p.CustomProperties.TryGetValue(TUT_DONE_KEY, out object state) || !(bool)state) return;
        }

        // Both players finished reading the slides! Set the synced clock exactly 0.5 seconds into the future.
        double futureTime = PhotonNetwork.Time + 0.5f;
        Hashtable roomProps = new Hashtable { { START_TIME_KEY, futureTime } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
    }

    // --- Validates if everyone has voted to replay ---
    private void CheckIfAllPlayersWantToPlayAgain()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
        {
            if (!p.CustomProperties.TryGetValue(PLAY_AGAIN_KEY, out object state) || !(bool)state) return;
        }

        // Synchronously reload the active scene across the network for a fresh start.
        PhotonNetwork.LoadLevel(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // Both computers receive the exact start time from the Master Client
        if (propertiesThatChanged.ContainsKey(START_TIME_KEY))
        {
            exactStartTime = (double)propertiesThatChanged[START_TIME_KEY];
        }
    }

    private void Update()
    {
        // Wait until we have a start time, and stop checking once the match starts
        if (matchStarted || exactStartTime == 0) return;

        double timeLeft = exactStartTime - PhotonNetwork.Time;

        if (timeLeft <= 0)
        {
            // BOOM! Start the game on the exact same frame for both players.
            matchStarted = true;
            
            if (wordGenerator != null) 
            {
                wordGenerator.StartGameLoop();
            }
        }
    }
}