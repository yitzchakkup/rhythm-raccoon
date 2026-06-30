using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    // Local Instance - Removed DontDestroyOnLoad to fix Canvas UI breaking on scene reloads
    public static NetworkManager Instance { get; private set; }

    [Header("Start Menu UI")]
    public GameObject startPanel;

    [Header("Waiting Room UI Elements")]
    public GameObject waitingRoomPanel;
    public HangingSignButton readyButton;
    public HangingSignButton backButton;
    public TMP_Text waitingRoomText;
    public TMP_Text countdownText;
    public TMP_Text readyCountText;

    [Header("Background Screens")]
    public GameObject startBackground;
    public GameObject waitingRoomBackground;

    [Header("Juice Settings")]
    public float transitionDuration = 0.15f;
    private Coroutine currentTransition;
    private Coroutine dotsCoroutine;

    [Header("Scene Transition")]
    public CanvasGroup fadeBlock;
    public float sceneFadeSpeed = 0.5f;
    
    [Header("Transition Settings")]
    public float fadeDuration = 0.5f;

    [Header("Audio")]
    public AudioClip lobbyMusic;

    private bool isReady = false;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        startPanel.SetActive(true);
        waitingRoomPanel.SetActive(false);
        startBackground.SetActive(true);
        waitingRoomBackground.SetActive(false);

        EnsureCanvasGroup(startPanel);
        EnsureCanvasGroup(waitingRoomPanel);

        if (readyButton != null) readyButton.gameObject.SetActive(false);

        if (AudioManager.Instance != null && lobbyMusic != null)
            AudioManager.Instance.PlayMusic(lobbyMusic);

        if (fadeBlock != null)
        {
            fadeBlock.alpha = 0f;
            fadeBlock.blocksRaycasts = false;
        }
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void EnsureCanvasGroup(GameObject panel)
    {
        if (panel == null) return;
        if (!panel.TryGetComponent<CanvasGroup>(out CanvasGroup cg)) cg = panel.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        panel.transform.localScale = Vector3.one;
    }

    private void SwitchPanel(GameObject fromPanel, GameObject toPanel, GameObject fromBg, GameObject toBg)
    {
        if (currentTransition != null) StopCoroutine(currentTransition);
        currentTransition = StartCoroutine(TransitionRoutine(fromPanel, toPanel, fromBg, toBg));
    }

    private IEnumerator TransitionRoutine(GameObject fromPanel, GameObject toPanel, GameObject fromBg, GameObject toBg)
    {
        float timer = 0f;
        if (fromPanel != null && fromPanel.activeSelf)
        {
            CanvasGroup fromGroup = fromPanel.GetComponent<CanvasGroup>();
            Vector3 startScale = fromPanel.transform.localScale;
            Vector3 endScale = Vector3.one * 0.9f;
            while (timer < transitionDuration)
            {
                float t = timer / transitionDuration;
                float ease = Mathf.Sin(t * Mathf.PI * 0.5f);
                if (fromGroup != null) fromGroup.alpha = 1f - ease;
                fromPanel.transform.localScale = Vector3.Lerp(startScale, endScale, ease);
                timer += Time.deltaTime;
                yield return null;
            }
            fromPanel.SetActive(false);
        }

        if (fromBg != null) fromBg.SetActive(false);
        if (toBg != null) toBg.SetActive(true);

        timer = 0f;
        if (toPanel != null)
        {
            toPanel.SetActive(true);
            CanvasGroup toGroup = toPanel.GetComponent<CanvasGroup>();
            Vector3 startScale = Vector3.one * 0.9f;
            Vector3 endScale = Vector3.one;
            while (timer < transitionDuration)
            {
                float t = timer / transitionDuration;
                float ease = Mathf.Sin(t * Mathf.PI * 0.5f);
                if (toGroup != null) toGroup.alpha = ease;
                toPanel.transform.localScale = Vector3.Lerp(startScale, endScale, ease);
                timer += Time.deltaTime;
                yield return null;
            }
            if (toGroup != null) toGroup.alpha = 1f;
            toPanel.transform.localScale = Vector3.one;
        }
    }

    private IEnumerator WaitingDotsRoutine()
    {
        string baseText = "Looking for a room";
        int dots = 0;
        while (true)
        {
            waitingRoomText.text = baseText + new string('.', dots % 4);
            dots++;
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator ShowButtonJuicy()
    {
        readyButton.gameObject.SetActive(true);
        readyButton.transform.localScale = Vector3.zero;
        float timer = 0f;
        float duration = 0.3f;
        while (timer < duration)
        {
            float t = timer / duration;
            float scale = Mathf.Min(1.2f, 1f + (Mathf.Sin(t * Mathf.PI) * 0.3f)); 
            readyButton.transform.localScale = Vector3.one * scale;
            timer += Time.deltaTime;
            yield return null;
        }
        readyButton.transform.localScale = Vector3.one;
    }

    public void OnSinglePlayerClicked() => StartCoroutine(SinglePlayerStartRoutine());

    private IEnumerator SinglePlayerStartRoutine()
    {
        if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            while (PhotonNetwork.NetworkClientState != ClientState.Disconnected) yield return null;
        }
        PhotonNetwork.OfflineMode = true;
        PhotonNetwork.JoinOrCreateRoom("OfflineRoom", new RoomOptions() { MaxPlayers = 1 }, null);
    }

    public void OnMultiplayerClicked()
    {
        // 1. Force the App Version so both players are in the exact same matchmaking pool
        PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion = "1.0.0";
        
        dotsCoroutine = StartCoroutine(WaitingDotsRoutine());
        SwitchPanel(startPanel, waitingRoomPanel, startBackground, waitingRoomBackground);
        PhotonNetwork.ConnectUsingSettings();
    }

    public void OnBackToMainMenuClicked() => StartCoroutine(DisconnectAndReturnRoutine());

    private IEnumerator DisconnectAndReturnRoutine()
    {
        if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            while (PhotonNetwork.NetworkClientState != ClientState.Disconnected) yield return null;
        }
        SwitchPanel(waitingRoomPanel, startPanel, waitingRoomBackground, startBackground);
    }

    // --- FIXED MATCHMAKING FLOW ---
    public override void OnConnectedToMaster()
    {
        if (PhotonNetwork.OfflineMode)
        {
            Debug.Log("<color=green>[Network]</color> Offline mode connected. Skipping Lobby.");
            return; 
        }

        Debug.Log("<color=green>[Network]</color> Connected to Master. Joining Lobby...");
        PhotonNetwork.JoinLobby(); 
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("<color=green>[Network]</color> Joined Lobby. Attempting to join or create a room atomically...");

        RoomOptions roomOptions = new RoomOptions() 
        { 
            MaxPlayers = 2, 
            IsVisible = true, 
            IsOpen = true 
        };

        // Stripped down to the absolute essentials to avoid version conflicts!
        PhotonNetwork.JoinRandomOrCreateRoom(
            expectedCustomRoomProperties: null,
            expectedMaxPlayers: 2,
            roomName: "Room_" + Random.Range(10000, 99999),
            roomOptions: roomOptions
        );
    }

    public override void OnJoinedRoom()
    {
        // --- THE FIX: Intercept Offline Mode and load the scene ---
        if (PhotonNetwork.OfflineMode)
        {
            Debug.Log("<color=green>[Network]</color> Offline Room Joined! Loading GameScene...");
            StartCoroutine(SinglePlayerFadeRoutine());
            return;
        }

        // --- Standard Multiplayer UI Logic ---
        if (dotsCoroutine != null) StopCoroutine(dotsCoroutine);
        countdownText.text = "";
        SetPlayerReadyState(false);
        if (backButton != null) backButton.interactable = true;
        if (readyButton != null) StartCoroutine(ShowButtonJuicy());
        UpdateWaitingRoomText();
        UpdateReadyCountUI();
    }
    
    private IEnumerator SinglePlayerFadeRoutine()
    {
        // 1. Fade the screen to black
        if (fadeBlock != null)
        {
            fadeBlock.blocksRaycasts = true;
            float timer = 0f;
            while (timer < sceneFadeSpeed)
            {
                fadeBlock.alpha = Mathf.Lerp(0f, 1f, timer / sceneFadeSpeed);
                timer += Time.deltaTime;
                yield return null;
            }
            fadeBlock.alpha = 1f;
        }

        // 2. Actually load the scene! 
        // (Make sure "GameScene" matches the exact spelling in your Build Settings)
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }

    public override void OnLeftRoom() { if (readyButton != null) readyButton.gameObject.SetActive(false); }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateWaitingRoomText();
        UpdateReadyCountUI();
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        SetPlayerReadyState(false);
        UpdateWaitingRoomText();
        UpdateReadyCountUI();
        countdownText.text = "Player left. Waiting...";
        if (PhotonNetwork.IsMasterClient) { PhotonNetwork.CurrentRoom.IsOpen = true; PhotonNetwork.CurrentRoom.IsVisible = true; }
    }

    public void ToggleReady() => SetPlayerReadyState(!isReady);

    private void SetPlayerReadyState(bool ready)
    {
        isReady = ready;
        TMP_Text buttonText = readyButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null) buttonText.text = isReady ? "UNREADY" : "READY";
        Hashtable props = new Hashtable() { { "IsReady", isReady } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    private void UpdateWaitingRoomText() => waitingRoomText.text = $"Players in Room: {PhotonNetwork.CurrentRoom.PlayerCount} / 2";

    private void UpdateReadyCountUI()
    {
        if (readyCountText == null) return;
        int readyPlayers = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue("IsReady", out object readyState) && (bool)readyState) readyPlayers++;
        }
        readyCountText.text = $"Players Ready: {readyPlayers} / 2";
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("IsReady")) { UpdateReadyCountUI(); CheckIfAllPlayersReady(); }
    }

    private void CheckIfAllPlayersReady()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom.PlayerCount != 2) return;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!p.CustomProperties.TryGetValue("IsReady", out object readyState) || !(bool)readyState) return;
        }
        photonView.RPC("StartCountdown_RPC", RpcTarget.All);
    }

    [PunRPC]
    private void StartCountdown_RPC()
    {
        readyButton.interactable = false;
        if (backButton != null) backButton.interactable = false;
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        for (int i = 3; i > 0; i--)
        {
            countdownText.text = $"Game Starting In: {i}";
            yield return new WaitForSeconds(1f);
        }
        countdownText.text = "GO!";
        yield return new WaitForSeconds(0.5f);
        if (fadeBlock != null)
        {
            fadeBlock.blocksRaycasts = true;
            float timer = 0f;
            while (timer < sceneFadeSpeed)
            {
                fadeBlock.alpha = Mathf.Lerp(0f, 1f, timer / sceneFadeSpeed);
                timer += Time.deltaTime;
                yield return null;
            }
            fadeBlock.alpha = 1f;
        }
        if (PhotonNetwork.IsMasterClient) PhotonNetwork.LoadLevel("GameScene");
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"<color=red>[Network]</color> Disconnected from Photon: {cause}");
        waitingRoomText.text = "Connection Failed! Click Back and try again.";
        if (dotsCoroutine != null) StopCoroutine(dotsCoroutine);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        // We leave this here just to catch standard errors, but we DO NOT create a room here anymore!
        Debug.Log($"<color=orange>[Network]</color> Random join failed: {message}");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"<color=red>[Network]</color> CreateRoom failed: {message}");
        waitingRoomText.text = "Error creating room! Retrying...";
        string roomName = "Room_" + Random.Range(1000, 9999);
        PhotonNetwork.CreateRoom(roomName); 
    }
    
    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log($"<color=yellow>[Network State]</color> Current State: {PhotonNetwork.NetworkClientState}");
        }
    }
}