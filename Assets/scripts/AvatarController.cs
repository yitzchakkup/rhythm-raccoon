using UnityEngine;
using Photon.Pun; 

public class AvatarController : MonoBehaviour
{
    public static AvatarController Instance { get; private set; }

    [Header("Avatars")]
    public SpriteRenderer localPlayer;
    public SpriteRenderer opponent;

    [Header("Character Art (.png Assets)")]
    public Sprite hostSprite;   // Drag your Host character PNG here
    public Sprite clientSprite; // Drag your Client character PNG here

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        // Ensure both renderers start with clean white coloring so sprites aren't tinted
        if (localPlayer != null) localPlayer.color = Color.white;
        if (opponent != null) opponent.color = Color.white;

        // Hide opponent if playing Single Player
        if (PhotonNetwork.OfflineMode || PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.PlayerCount <= 1)
        {
            if (opponent != null) opponent.gameObject.SetActive(false);
            
            // Default our player to the Host sprite in single-player
            if (localPlayer != null) localPlayer.sprite = hostSprite;
        }
        else
        {
            // If Multiplayer, assign sprites based on who is the Master Client
            if (PhotonNetwork.IsMasterClient)
            {
                if (localPlayer != null) localPlayer.sprite = hostSprite;         
                if (opponent != null) opponent.sprite = clientSprite;  
            }
            else
            {
                if (localPlayer != null) localPlayer.sprite = clientSprite;        
                if (opponent != null) opponent.sprite = hostSprite;   
            }
        }
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

    // Revert color back to pure white so the original PNG artwork shows perfectly
    private void ResetLocalColor() => localPlayer.color = Color.white;
    private void ResetOpponentColor() => opponent.color = Color.white;
}