using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;

    [Header("Settings")]
    [Tooltip("How many pose animations do you have in the Animator?")]
    [SerializeField] private int totalPoses = 6;

    private void Awake()
    {
        // Automatically grab the Animator component if it's on the same object
        if (animator == null) animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Call this every time a letter is correctly typed!
    /// </summary>
    public void TriggerRandomPose()
    {
        if (animator == null || totalPoses <= 0) return;

        // Pick a random number between 1 and your total number of poses (e.g., 1 through 6)
        int randomIndex = Random.Range(0, totalPoses);

        // Construct the exact name of the state you typed in the Animator window
        string animationStateName = "possum_" + randomIndex;

        // Force the Animator to play this state immediately.
        // '-1' targets the base animation layer.
        // '0f' forces the animation to restart from frame 0 even if they type super fast!
        animator.Play(animationStateName, -1, 0f);
    }
}