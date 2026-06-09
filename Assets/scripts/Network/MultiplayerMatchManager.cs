using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UI; // Needed for the Image fill

public class MultiplayerMatchManager : MonoBehaviourPun
{
    public static MultiplayerMatchManager Instance { get; private set; }

    [Header("Opponent UI")]
    public TMP_Text opponentScoreText;
    public Image opponentStaminaBarFill; // Swapped to an Image to match your new system

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
    }

    // --- SCORE SYNC ---
    public void SyncMyScore(int myTotalScore)
    {
        if (PhotonNetwork.OfflineMode || !PhotonNetwork.IsConnected) return;
        photonView.RPC("ReceiveOpponentScore_RPC", RpcTarget.Others, myTotalScore);
    }

    [PunRPC]
    private void ReceiveOpponentScore_RPC(int opponentScore)
    {
        if (opponentScoreText != null) 
        {
            opponentScoreText.text = $"Opponent: {opponentScore}";
        }
    }

    // --- STAMINA SYNC ---
    public void SyncMyStamina(float currentStamina, float maxStamina)
    {
        if (PhotonNetwork.OfflineMode || !PhotonNetwork.IsConnected) return;
        
        // We calculate the fraction (0.0 to 1.0) before sending it over the network
        float fillFraction = currentStamina / maxStamina;
        photonView.RPC("ReceiveOpponentStamina_RPC", RpcTarget.Others, fillFraction);
    }

    [PunRPC]
    private void ReceiveOpponentStamina_RPC(float opponentFillFraction)
    {
        if (opponentStaminaBarFill != null) 
        {
            opponentStaminaBarFill.fillAmount = opponentFillFraction;
        }
    }

    // --- ATTACK SYNC ---
    public void SendAttackToOpponent(string attackName)
    {
        if (PhotonNetwork.OfflineMode || !PhotonNetwork.IsConnected) return;
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
            // --- NEW: Catching the Stamina Attack ---
            case "HalveStamina":
                if (ScoreAndStaminaManager.Instance != null)
                    ScoreAndStaminaManager.Instance.ActivateStaminaMultiplier(0.5f, 10f);
                break;
        }
    }
}