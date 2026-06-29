using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class NetworkManager : MonoBehaviourPunCallbacks
{
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

    [Header("Scene Transition")]
    public CanvasGroup fadeBlock;
    public float sceneFadeSpeed = 0.5f;

    [Header("Audio")]
    public AudioClip lobbyMusic;

    private bool isReady = false;

    void Start()
    {
        startPanel.SetActive(true);
        waitingRoomPanel.SetActive(false);
        startBackground.SetActive(true);
        waitingRoomBackground.SetActive(false);

        EnsureCanvasGroup(startPanel);
        EnsureCanvasGroup(waitingRoomPanel);

        if (AudioManager.Instance != null && lobbyMusic != null)
        {
            AudioManager.Instance.PlayMusic(lobbyMusic);
        }

        if (fadeBlock != null)
        {
            fadeBlock.alpha = 0f;
            fadeBlock.blocksRaycasts = false;
        }

        PhotonNetwork.AutomaticallySyncScene = true;
    }

    // --- FIX 1: Automatically add CanvasGroup if it's missing! ---
    private void EnsureCanvasGroup(GameObject panel)
    {
        if (panel == null) return;
        
        if (!panel.TryGetComponent<CanvasGroup>(out CanvasGroup cg))
        {
            cg = panel.AddComponent<CanvasGroup>();
        }
        
        cg.alpha = 1f;
        panel.transform.localScale = Vector3.one;
    }

    // --- PANEL TRANSITION LOGIC ---

    // --- FIX 2: Pass the backgrounds into the SwitchPanel method ---
    private void SwitchPanel(GameObject fromPanel, GameObject toPanel, GameObject fromBg, GameObject toBg)
    {
        if (currentTransition != null) StopCoroutine(currentTransition);
        currentTransition = StartCoroutine(TransitionRoutine(fromPanel, toPanel, fromBg, toBg));
    }

    private IEnumerator TransitionRoutine(GameObject fromPanel, GameObject toPanel, GameObject fromBg, GameObject toBg)
    {
        float timer = 0f;

        // 1. Fade OUT the old panel
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

        // --- FIX 3: Swap the backgrounds cleanly in the middle of the transition! ---
        if (fromBg != null) fromBg.SetActive(false);
        if (toBg != null) toBg.SetActive(true);

        timer = 0f;

        // 2. Fade IN the new panel
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

    // --- UI BUTTON METHODS ---

    public void OnSinglePlayerClicked()
    {
        StartCoroutine(SinglePlayerStartRoutine());
    }

    private IEnumerator SinglePlayerStartRoutine()
    {
        if (PhotonNetwork.InRoom || PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Joining)
        {
            PhotonNetwork.OfflineMode = false;
            yield return null;
        }

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            while (PhotonNetwork.NetworkClientState != Photon.Realtime.ClientState.Disconnected)
                yield return null;
        }

        yield return null;

        PhotonNetwork.OfflineMode = true;

        while (PhotonNetwork.NetworkClientState != Photon.Realtime.ClientState.ConnectedToMasterServer)
            yield return null;

        PhotonNetwork.JoinOrCreateRoom("OfflineRoom", new RoomOptions() { MaxPlayers = 1 }, null);
    }

    public void OnMultiplayerClicked()
    {
        // --- FIX 4: Call the updated SwitchPanel and let it handle the backgrounds ---
        waitingRoomText.text = "Connecting to Server...";
        SwitchPanel(startPanel, waitingRoomPanel, startBackground, waitingRoomBackground);
        PhotonNetwork.ConnectUsingSettings();
    }

    public void OnBackToMainMenuClicked()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }

        // --- FIX 5: Use the updated SwitchPanel for the back button too ---
        SwitchPanel(waitingRoomPanel, startPanel, waitingRoomBackground, startBackground);
    }

    // --- PHOTON CALLBACKS ---

    public override void OnConnectedToMaster()
    {
        waitingRoomText.text = "Connected! Looking for a room...";
        PhotonNetwork.JoinRandomRoom(null, 2, MatchmakingMode.FillRoom, null, null);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        waitingRoomText.text = "No open rooms found. Creating one...";

        string roomName = "Room_" + Random.Range(1000, 9999);
        RoomOptions roomOptions = new RoomOptions()
        {
            MaxPlayers = 2,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"CreateRoom failed ({returnCode}): {message}. Retrying...");
        string roomName = "Room_" + Random.Range(1000, 9999);
        RoomOptions roomOptions = new RoomOptions() { MaxPlayers = 2, IsVisible = true, IsOpen = true };
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.OfflineMode)
        {
            StartCoroutine(SinglePlayerFadeRoutine());
            return;
        }

        countdownText.text = "";
        SetPlayerReadyState(false);
        if (backButton != null) backButton.interactable = true;
        UpdateWaitingRoomText();
        UpdateReadyCountUI();
    }

    private IEnumerator SinglePlayerFadeRoutine()
    {
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
        PhotonNetwork.LoadLevel("GameScene");
    }

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

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = true;
            PhotonNetwork.CurrentRoom.IsVisible = true;
        }
    }

    public void ToggleReady()
    {
        SetPlayerReadyState(!isReady);
    }

    private void SetPlayerReadyState(bool ready)
    {
        isReady = ready;
        TMP_Text buttonText = readyButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null) 
        {
            buttonText.text = isReady ? "UNREADY" : "READY";
        }

        Hashtable props = new Hashtable() { { "IsReady", isReady } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    private void UpdateWaitingRoomText()
    {
        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        waitingRoomText.text = $"Players in Room: {playerCount} / 2";
    }

    private void UpdateReadyCountUI()
    {
        if (readyCountText == null) return;

        int readyPlayers = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue("IsReady", out object readyState) && (bool)readyState)
            {
                readyPlayers++;
            }
        }
        readyCountText.text = $"Players Ready: {readyPlayers} / 2";
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("IsReady"))
        {
            UpdateReadyCountUI();
            CheckIfAllPlayersReady();
        }
    }

    private void CheckIfAllPlayersReady()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (PhotonNetwork.CurrentRoom.PlayerCount != 2) return;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!p.CustomProperties.TryGetValue("IsReady", out object readyState) || !(bool)readyState)
                return;
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

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }
}