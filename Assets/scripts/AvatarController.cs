using UnityEngine;
using Photon.Pun; 

public class AvatarController : MonoBehaviour
{
    public static AvatarController Instance { get; private set; }

    [Header("Avatars (Sprite Renderers)")]
    public SpriteRenderer localPlayer;
    public SpriteRenderer opponent;

    [Header("Avatars (Animators)")]
    [Tooltip("The PlayerAnimatorController script attached to the Local Player object")]
    public PlayerAnimatorController localAnimator;
    [Tooltip("The PlayerAnimatorController script attached to the Opponent object")]
    public PlayerAnimatorController opponentAnimator;

    [Header("Character Art (.png Assets)")]
    public Sprite hostSprite;   // Possum PNG
    public Sprite clientSprite; // Raccoon PNG

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

        // Single Player Logic
        if (PhotonNetwork.OfflineMode || PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.PlayerCount <= 1)
        {
            if (opponent != null) opponent.gameObject.SetActive(false);
            
            // Set Host Image
            if (localPlayer != null) localPlayer.sprite = hostSprite;
            
            // Set Host Animations (true = isHost)
            if (localAnimator != null) localAnimator.SetupCharacter(true); 
        }
        else // Multiplayer Logic
        {
            if (PhotonNetwork.IsMasterClient)
            {
                // We are Host (Possum)
                if (localPlayer != null) localPlayer.sprite = hostSprite;         
                if (opponent != null) opponent.sprite = clientSprite;  

                // Assign Animations
                if (localAnimator != null) localAnimator.SetupCharacter(true);
                if (opponentAnimator != null) opponentAnimator.SetupCharacter(false);
            }
            else
            {
                // We are Client (Raccoon)
                if (localPlayer != null) localPlayer.sprite = clientSprite;        
                if (opponent != null) opponent.sprite = hostSprite;   

                // Assign Animations
                if (localAnimator != null) localAnimator.SetupCharacter(false);
                if (opponentAnimator != null) opponentAnimator.SetupCharacter(true);
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

    private void ResetLocalColor() => localPlayer.color = Color.white;
    private void ResetOpponentColor() => opponent.color = Color.white;
}