using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable; 

public static class GameConstants
{
    public const string PLAY_AGAIN_KEY = "PlayAgain";
}

public class EndGameUIController : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    [SerializeField] private HangingSignButton readyButton;
    [SerializeField] private HangingSignButton mainMenuButton;
    [SerializeField] private TMP_Text readyCountText;
    [SerializeField] private TMP_Text countdownText;

    private bool isReady = false;
    private Coroutine dotsCoroutine;
    private Coroutine countdownCoroutine;

    // --- NEW: Catching accidental awakes! ---
    private void Awake()
    {
        Debug.Log("<color=yellow>[UI Awake]</color> EndGameUIController woke up! If you see this right as a scene loads, your UI is turned ON by default in the Inspector!");
    }

    public override void OnEnable()
    {
        base.OnEnable();
        Debug.Log("<color=cyan>[UI Enable]</color> End screen opened. Checking current network state...");

        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(GameConstants.PLAY_AGAIN_KEY, out object readyState))
        {
            isReady = (bool)readyState;
        }
        else
        {
            isReady = false;
        }

        if (readyButton != null)
        {
            readyButton.onClick.RemoveAllListeners();
            readyButton.onClick.AddListener(OnReadyClicked);
            readyButton.interactable = true;
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            mainMenuButton.interactable = true;
        }

        EvaluateState();
    }

    public override void OnDisable()
    {
        base.OnDisable(); 
        Debug.Log("<color=cyan>[UI Disable]</color> End screen was successfully hidden/closed.");
        StopAllAnimations();
    }

    private void SetButtonLabel(string text)
    {
        if (readyButton == null) return;
        TMP_Text label = readyButton.GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = text;
    }

    private void OnReadyClicked()
    {
        isReady = !isReady;
        Hashtable props = new Hashtable() { { GameConstants.PLAY_AGAIN_KEY, isReady } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        SetButtonLabel(isReady ? "UNREADY" : "PLAY AGAIN");
    }

    private void OnMainMenuClicked()
    {
        if (mainMenuButton != null) mainMenuButton.interactable = false;
        if (GameManager.Instance != null) GameManager.Instance.ReturnToMainMenu();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey(GameConstants.PLAY_AGAIN_KEY))
        {
            EvaluateState();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        EvaluateState();
    }

    private void EvaluateState()
    {
        if (!PhotonNetwork.InRoom) return;

        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            StopAllAnimations();
            if (countdownText != null) countdownText.text = "Opponent left.";
            if (readyCountText != null) readyCountText.text = "";
            if (readyButton != null) readyButton.interactable = false;
            SetButtonLabel("PLAY AGAIN");
            return;
        }

        int readyCount = 0;
        bool amIReady = false;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue(GameConstants.PLAY_AGAIN_KEY, out object val) && (bool)val)
            {
                readyCount++;
                if (p.IsLocal) amIReady = true;
            }
        }

        if (readyCountText != null) readyCountText.text = $"Players Ready: {readyCount} / 2";
        SetButtonLabel(amIReady ? "UNREADY" : "PLAY AGAIN");

        if (readyCount >= 2)
        {
            StopAllAnimations();
            if (PhotonNetwork.IsMasterClient && countdownCoroutine == null)
            {
                photonView.RPC("StartCountdown_RPC", RpcTarget.AllBuffered);
            }
        }
        else if (amIReady && readyCount == 1)
        {
            if (dotsCoroutine == null) dotsCoroutine = StartCoroutine(WaitingDotsRoutine());
        }
        else
        {
            StopAllAnimations();
            if (countdownText != null) countdownText.text = "";
        }
    }

    private void StopAllAnimations()
    {
        if (dotsCoroutine != null) StopCoroutine(dotsCoroutine);
        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
        dotsCoroutine = null;
        countdownCoroutine = null;
    }

    private IEnumerator WaitingDotsRoutine()
    {
        string[] dotStates = { "Waiting for opponent", "Waiting for opponent.", "Waiting for opponent..", "Waiting for opponent..." };
        int index = 0;
        while (true)
        {
            if (countdownText != null) countdownText.text = dotStates[index % dotStates.Length];
            index++;
            yield return new WaitForSeconds(0.5f);
        }
    }

    [PunRPC]
    private void StartCountdown_RPC()
    {
        StopAllAnimations();

        if (readyButton != null) readyButton.interactable = false;
        if (mainMenuButton != null) mainMenuButton.interactable = false;
        
        countdownCoroutine = StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        for (int i = 3; i > 0; i--)
        {
            if (countdownText != null) countdownText.text = $"Starting in {i}...";
            yield return new WaitForSecondsRealtime(1f);
        }

        if (countdownText != null) countdownText.text = "GO!";
        yield return new WaitForSecondsRealtime(0.5f);
        
        Time.timeScale = 1f; 

        if (PhotonNetwork.IsMasterClient)
        {
            GameManager.Instance.RequestMultiplayerReplay();
        }
    }
}