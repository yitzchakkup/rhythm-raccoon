using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;

    [Header("Multiplayer Controllers")]
    [Tooltip("Drag the PossumController here (Host)")]
    [SerializeField] private RuntimeAnimatorController possumController;
    [Tooltip("Drag the RaccoonController here (Client)")]
    [SerializeField] private RuntimeAnimatorController raccoonController;

    [Header("Settings")]
    [Tooltip("How many pose animations do you have in the Animator?")]
    [SerializeField] private int totalPoses = 6;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Call this exactly ONCE when the multiplayer match starts to assign the correct animal!
    /// </summary>
    public void SetupCharacter(bool isHost)
    {
        if (animator == null) return;

        // Swap the "brain" of the animator based on network role
        if (isHost)
        {
            animator.runtimeAnimatorController = possumController;
        }
        else
        {
            animator.runtimeAnimatorController = raccoonController;
        }
    }

    /// <summary>
    /// Call this every time a letter is correctly typed!
    /// </summary>
    /// <summary>
    /// Call this ONLY on the local player when they type a letter!
    /// </summary>
    public void TriggerRandomPose()
    {
        if (animator == null || totalPoses <= 0 || animator.runtimeAnimatorController == null) return;

        // 1. Roll the random number locally
        int randomIndex = Random.Range(0, totalPoses);

        // 2. Play it on our own screen
        PlaySpecificPose(randomIndex);

        // 3. Tell the AvatarController to send this exact number across the internet!
        if (AvatarController.Instance != null)
        {
            AvatarController.Instance.SendPoseToOpponent(randomIndex);
        }
    }

    /// <summary>
    /// Plays an exact pose. The network will use this to sync the opponent.
    /// </summary>
    public void PlaySpecificPose(int poseIndex)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        
        string animationStateName = "Pose_" + poseIndex;
        animator.Play(animationStateName, 0, 0f);
    }
}