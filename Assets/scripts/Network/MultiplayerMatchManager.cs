using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class MultiplayerMatchManager : MonoBehaviourPun
{
    public static MultiplayerMatchManager Instance { get; private set; }

    private float currentMyScore = 0f;
    private float currentOpponentScore = 0f;

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
    public void SyncMyScore(float myTotalScore)
    {
        currentMyScore = myTotalScore;

        if (!IsMultiplayerGame()) return;

        photonView.RPC("ReceiveOpponentScore_RPC", RpcTarget.Others, myTotalScore);
    }

    [PunRPC]
    private void ReceiveOpponentScore_RPC(float opponentScore)
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

    public void ResetMatch()
    {
        if (ScoreAndStaminaManager.Instance != null)
        {
            ScoreAndStaminaManager.Instance.Initialize();
        }

        currentMyScore = 0f;
        currentOpponentScore = 0f;

        FallingLetter[] activeLetters = FindObjectsOfType<FallingLetter>();
        foreach (FallingLetter letter in activeLetters)
        {
            Destroy(letter.gameObject);
        }
    }

    // --- Public Score Getters ---
    public float GetMyScore() => currentMyScore;
    public float GetOpponentScore() => currentOpponentScore;
}