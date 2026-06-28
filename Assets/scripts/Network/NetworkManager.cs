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
    public Button readyButton;
    public Button backButton;
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

    private void EnsureCanvasGroup(GameObject panel)
    {
        if (panel.TryGetComponent<CanvasGroup>(out CanvasGroup cg)) cg.alpha = 1f;
        panel.transform.localScale = Vector3.one;
    }

    // --- PANEL TRANSITION LOGIC ---

    private void SwitchPanel(GameObject fromPanel, GameObject toPanel)
    {
        if (currentTransition != null) StopCoroutine(currentTransition);
        currentTransition = StartCoroutine(TransitionRoutine(fromPanel, toPanel));
    }

    private IEnumerator TransitionRoutine(GameObject fromPanel, GameObject toPanel)
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

    // --- UI BUTTON METHODS ---

    public void OnSinglePlayerClicked()
    {
        StartCoroutine(SinglePlayerStartRoutine());
    }

    private IEnumerator SinglePlayerStartRoutine()
    {
        // Step 1: Leave room if in one (covers offline ghost rooms too)
        if (PhotonNetwork.InRoom || PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Joining)
        {
            PhotonNetwork.OfflineMode = false; // Kills the offline session immediately
            yield return null; // Give Photon one frame to process
        }

        // Step 2: Disconnect if still connected in any way
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            while (PhotonNetwork.NetworkClientState != Photon.Realtime.ClientState.Disconnected)
                yield return null;
        }

        // Step 3: Extra safety — wait one more frame before re-enabling offline mode
        yield return null;

        // Step 4: Start fresh offline session
        PhotonNetwork.OfflineMode = true;

        // Step 5: Wait until Photon is actually ready to accept a room call
        while (PhotonNetwork.NetworkClientState != Photon.Realtime.ClientState.ConnectedToMasterServer)
            yield return null;

        // Step 6: Safe to create room now
        PhotonNetwork.JoinOrCreateRoom("OfflineRoom", new RoomOptions() { MaxPlayers = 1 }, null);
    }

    public void OnMultiplayerClicked()
    {
        SwitchPanel(startPanel, waitingRoomPanel);
        startBackground.SetActive(false);
        waitingRoomBackground.SetActive(true);
        waitingRoomText.text = "Connecting to Server...";
        PhotonNetwork.ConnectUsingSettings();
    }

    public void OnBackToMainMenuClicked()
    {
        // PROPER CLEANUP: Always leave the room before disconnecting
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }

        SwitchPanel(waitingRoomPanel, startPanel);
        waitingRoomBackground.SetActive(false);
        startBackground.SetActive(true);
    }

    // --- PHOTON CALLBACKS ---

    public override void OnConnectedToMaster()
    {
        waitingRoomText.text = "Connected! Finding a room...";
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        waitingRoomText.text = "Creating Room...";
        RoomOptions roomOptions = new RoomOptions() { MaxPlayers = 2 };
        PhotonNetwork.CreateRoom("TypingArena", roomOptions);
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
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        SetPlayerReadyState(false);
        UpdateWaitingRoomText();
        UpdateReadyCountUI();
        countdownText.text = "Player left. Waiting...";
    }

    public void ToggleReady()
    {
        SetPlayerReadyState(!isReady);
    }

    private void SetPlayerReadyState(bool ready)
    {
        isReady = ready;
        readyButton.GetComponentInChildren<TMP_Text>().text = isReady ? "UNREADY" : "READY";

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
            {
                return;
            }
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