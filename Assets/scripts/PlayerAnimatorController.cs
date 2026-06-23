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
    public void TriggerRandomPose()
    {
        // Safety check: Don't try to pose if a controller hasn't been assigned yet
        if (animator == null || totalPoses <= 0 || animator.runtimeAnimatorController == null) return;

        int randomIndex = Random.Range(0, totalPoses);
        string animationStateName = "Pose_" + randomIndex;

        animator.Play(animationStateName, 0, 0f);
    }
}