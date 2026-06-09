using UnityEngine;
using Photon.Pun; // --- NEW: We need this to check our Host status ---

public class AvatarController : MonoBehaviour
{
    public static AvatarController Instance { get; private set; }

    [Header("Avatars")]
    public SpriteRenderer localPlayer;
    public SpriteRenderer opponent;

    // --- NEW: Variables to remember our assigned colors ---
    private Color myBaseColor;
    private Color opponentBaseColor;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        // --- NEW: Hide opponent if playing Single Player ---
        if (PhotonNetwork.OfflineMode || PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.PlayerCount <= 1)
        {
            if (opponent != null) opponent.gameObject.SetActive(false);
            myBaseColor = Color.green; // Default our player to Green in single-player
        }
        else
        {
            // If Multiplayer, do our standard color assignment
            if (PhotonNetwork.IsMasterClient)
            {
                myBaseColor = Color.green;         
                opponentBaseColor = Color.yellow;  
            }
            else
            {
                myBaseColor = Color.yellow;        
                opponentBaseColor = Color.green;   
            }

            if (opponent != null) opponent.color = opponentBaseColor;
        }

        // Always apply our local color
        if (localPlayer != null) localPlayer.color = myBaseColor;
    }

    public void PlayLocalDamageEffect()
    {
        if (localPlayer == null) return;
        
        localPlayer.color = Color.red;
        Invoke(nameof(ResetLocalColor), 0.5f);
    }

    public void PlayOpponentDamageEffect()
    {
        if (opponent == null) return;

        opponent.color = Color.red;
        Invoke(nameof(ResetOpponentColor), 0.5f);
    }

    // --- UPDATED: Revert back to our specific identity colors, not just White ---
    private void ResetLocalColor() => localPlayer.color = myBaseColor;
    private void ResetOpponentColor() => opponent.color = opponentBaseColor;
}