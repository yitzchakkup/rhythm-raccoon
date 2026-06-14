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

    [Header("Lobby UI Elements")]
    public GameObject lobbyPanel; 
    public Button createButton;
    public Button joinButton;
    public TMP_Text statusText;

    [Header("Waiting Room UI Elements")]
    public GameObject waitingRoomPanel; 
    public Button readyButton;
    public TMP_Text waitingRoomText;
    public TMP_Text countdownText;
    public TMP_Text readyCountText; 

    [Header("Juice Settings")]
    public float transitionDuration = 0.15f; 
    private Coroutine currentTransition;

    [Header("Scene Transition")]
    public CanvasGroup fadeBlock; 
    public float sceneFadeSpeed = 0.5f;

    private bool isReady = false;

    void Start()
    {
        startPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        waitingRoomPanel.SetActive(false);
        
        EnsureCanvasGroup(startPanel);
        EnsureCanvasGroup(lobbyPanel);
        EnsureCanvasGroup(waitingRoomPanel);
        
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
        // UPDATED: Start a coroutine to handle the safe disconnect!
        StartCoroutine(SinglePlayerStartRoutine());
    }

    private IEnumerator SinglePlayerStartRoutine()
    {
        // 1. If we are connected, gracefully disconnect first
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            
            // Wait right here until Photon confirms the internet is fully disconnected
            while (PhotonNetwork.IsConnected)
            {
                yield return null;
            }
        }

        // 2. Now it is 100% safe to turn on offline mode and create the local room
        PhotonNetwork.OfflineMode = true;
        PhotonNetwork.CreateRoom("OfflineRoom"); 
    }

    public void OnMultiplayerClicked()
    {
        SwitchPanel(startPanel, lobbyPanel);
        
        createButton.interactable = false;
        joinButton.interactable = false;
        statusText.text = "Connecting to Servers...";
        
        PhotonNetwork.ConnectUsingSettings();
    }

    public void OnBackToMainMenuClicked()
    {
        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();

        GameObject activePanel = lobbyPanel.activeSelf ? lobbyPanel : waitingRoomPanel.activeSelf ? waitingRoomPanel : null;
        SwitchPanel(activePanel, startPanel);
    }

    // --- LOBBY LOGIC ---

    public override void OnConnectedToMaster()
    {
        statusText.text = "Connected to Master! Joining Lobby...";
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        statusText.text = "In Lobby. Ready to play!";
        createButton.interactable = true;
        joinButton.interactable = true;
    }

    public void CreateGameRoom()
    {
        statusText.text = "Creating Room...";
        RoomOptions roomOptions = new RoomOptions() { MaxPlayers = 2 };
        PhotonNetwork.CreateRoom("TypingArena", roomOptions);
    }

    public void JoinGameRoom()
    {
        statusText.text = "Joining Room...";
        PhotonNetwork.JoinRoom("TypingArena");
    }

    // --- ROOM LOGIC ---

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.OfflineMode)
        {
            // UPDATED: Do not load instantly! Trigger the fade to black first.
            StartCoroutine(SinglePlayerFadeRoutine());
            return;
        }

        SwitchPanel(lobbyPanel, waitingRoomPanel);
        
        countdownText.text = "";
        SetPlayerReadyState(false);
        UpdateWaitingRoomText();
        UpdateReadyCountUI(); 
    }

    // --- NEW: Single Player Fade Routine ---
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