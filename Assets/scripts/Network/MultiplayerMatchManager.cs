using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;

public class MultiplayerMatchManager : MonoBehaviourPun
{
    public static MultiplayerMatchManager Instance { get; private set; }

    [Header("Opponent UI")]
    public TMP_Text opponentScoreText;
    public Image opponentStaminaBarFill;

    private int currentOpponentScore = 0;
    private int currentMyScore = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
    }
    
    private void Start()
    {
        if (IsMultiplayerGame())
        {
            // --- MULTIPLAYER ---
            // Hide the opponent's stamina bar
            if (opponentStaminaBarFill != null) opponentStaminaBarFill.transform.parent.gameObject.SetActive(false);
        
            // Hide your local stamina bar using our safe UI bridge!
            if (SceneUIRefs.staminaBarFill != null) 
            {
                SceneUIRefs.staminaBarFill.transform.parent.gameObject.SetActive(false);
            }
        }
        else
        {
            // --- SINGLE PLAYER ---
            // Hide the opponent's score and stamina
            if (opponentScoreText != null) opponentScoreText.gameObject.SetActive(false);
            if (opponentStaminaBarFill != null) opponentStaminaBarFill.transform.parent.gameObject.SetActive(false); 
        }
    }

    /// <summary>
    /// Checks if the current game is a multiplayer match.
    /// </summary>
    /// <returns>True if online and in a room with more than one player, false otherwise.</returns>
    public bool IsMultiplayerGame()
    {
        return !PhotonNetwork.OfflineMode && PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount > 1;
    }

    // --- SCORE SYNC ---
    public void SyncMyScore(int myTotalScore)
    {
        if (!IsMultiplayerGame()) return;
        currentMyScore = myTotalScore;
        photonView.RPC("ReceiveOpponentScore_RPC", RpcTarget.Others, myTotalScore);
    }

    [PunRPC]
    private void ReceiveOpponentScore_RPC(int opponentScore)
    {
        currentOpponentScore = opponentScore;
        if (opponentScoreText != null) 
        {
            opponentScoreText.text = $"Opponent: {opponentScore}";
        }
    }

    // --- STAMINA SYNC (No longer used, but kept for reference) ---
    public void SyncMyStamina(float currentStamina, float maxStamina)
    {
        // This functionality is now disabled in multiplayer.
    }

    [PunRPC]
    private void ReceiveOpponentStamina_RPC(float opponentFillFraction)
    {
        // This functionality is now disabled in multiplayer.
    }

    // --- ATTACK SYNC ---
    public void SendAttackToOpponent(string attackName)
    {
        if (!IsMultiplayerGame()) return;
        photonView.RPC("ReceiveAttack_RPC", RpcTarget.Others, attackName);
    }
    
    [PunRPC]
    private void ReceiveAttack_RPC(string attackName)
    {
        Debug.Log($"Hit by attack: {attackName}");
        
        if (AvatarController.Instance != null) AvatarController.Instance.PlayLocalDamageEffect();

        switch (attackName)
        {
            case "HalveScore":
                if (ScoreAndStaminaManager.Instance != null)
                    ScoreAndStaminaManager.Instance.ActivateScoreMultiplier(0.5f, 10f);
                break;
            case "TempoShift":
                WordGenerator generator = FindObjectOfType<WordGenerator>();
                if (generator != null)
                {
                    // The opponent hit us! Hardcode the punishment values here:
                    generator.TriggerSpeedAttack(2.5f, 4f); 
                }
                break;
            case "HalveStamina":
                // This attack is now effectively disabled as stamina is not used in multiplayer.
                break;
        }
    }

    // --- Public Score Getters ---
    public int GetOpponentScore()
    {
        return currentOpponentScore;
    }

    public int GetMyScore()
    {
        return currentMyScore;
    }
}