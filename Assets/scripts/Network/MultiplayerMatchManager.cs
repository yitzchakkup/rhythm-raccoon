using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))] 
public class MultiplayerMatchManager : MonoBehaviourPun
{
    public static MultiplayerMatchManager Instance { get; private set; }

    private int currentMyScore = 0;
    private int currentOpponentScore = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
    }
    
    private void Start()
    {
        if (!IsMultiplayerGame())
        {
            if (SceneUIRefs.tugOfWarUI != null) SceneUIRefs.tugOfWarUI.SetActive(false);
        }
        else
        {
            if (SceneUIRefs.staminaBar != null) SceneUIRefs.staminaBar.SetActive(false);
        }
    }

    /// <summary>
    /// Checks if the current game is a multiplayer match.
    /// </summary>
    public bool IsMultiplayerGame()
    {
        return !PhotonNetwork.OfflineMode && PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount > 1;
    }

    // --- SCORE SYNC ---
    public void SyncMyScore(int myTotalScore)
    {
        currentMyScore = myTotalScore;

        if (!IsMultiplayerGame()) return;
        
        photonView.RPC("ReceiveOpponentScore_RPC", RpcTarget.Others, myTotalScore);
    }

    [PunRPC]
    private void ReceiveOpponentScore_RPC(int opponentScore)
    {
        currentOpponentScore = opponentScore;
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

        float duration = 0f;
        switch (attackName)
        {
            case "HalveScore":
                duration = 10f;
                if (ScoreAndStaminaManager.Instance != null)
                    ScoreAndStaminaManager.Instance.ActivateScoreMultiplier(0.5f, duration);
                break;
            case "TempoShift":
                duration = 4f;
                WordGenerator generator = FindAnyObjectByType<WordGenerator>();
                if (generator != null)
                {
                    generator.TriggerSpeedAttack(2.5f, duration); 
                }
                break;
        }
        
        if (PowerupUIManager.Instance != null)
        {
            PowerupUIManager.Instance.ActivateIcon(attackName, duration);
        }
    }

    // --- Public Score Getters ---
    public int GetMyScore() => currentMyScore;
    public int GetOpponentScore() => currentOpponentScore;
}