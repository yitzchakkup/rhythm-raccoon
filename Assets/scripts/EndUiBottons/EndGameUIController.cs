using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class EndGameUIController : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    [SerializeField] private Button playAgainButton;
    [SerializeField] private TMP_Text readyCountText;

    private void OnEnable()
    {
        // Reset button states whenever the panel is turned on
        if (playAgainButton != null)
        {
            playAgainButton.interactable = true;
            playAgainButton.onClick.RemoveAllListeners();
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        }

        UpdateReadyCountUI();
    }

    private void OnPlayAgainClicked()
    {
        // Disable button so the player can't spam it
        if (playAgainButton != null) playAgainButton.interactable = false;

        // Tell GameManager to flag us as ready to replay
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RequestMultiplayerReplay();
        }
    }

    // Listen for custom property updates (when either player clicks the button)
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("PlayAgain"))
        {
            UpdateReadyCountUI();
        }
    }

    private void UpdateReadyCountUI()
    {
        if (readyCountText == null || !PhotonNetwork.InRoom) return;

        int readyCount = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue("PlayAgain", out object isReady) && (bool)isReady)
            {
                readyCount++;
            }
        }

        readyCountText.text = $"Players Ready: {readyCount} / 2";
    }
}