using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages a UI Slider to visually represent the score difference, update face expressions,
/// animate dynamic team progress bars, and trigger the multiplayer game over condition.
/// </summary>
public class TugOfWarUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider tugOfWarSlider;
    
    [Header("Dynamic Progress Bars (.png Assets)")]
    [SerializeField] private Image localFillBar;
    [SerializeField] private Image opponentFillBar;
    
    [Header("Avatar Face Renderers")]
    [SerializeField] private Image localFaceImage;
    [SerializeField] private Image opponentFaceImage;

    [Header("Local Player Sprites")]
    [SerializeField] private Sprite localNormalSprite;
    [SerializeField] private Sprite localFearSprite;

    [Header("Opponent Sprites")]
    [SerializeField] private Sprite opponentNormalSprite;
    [SerializeField] private Sprite opponentFearSprite;

    [Header("Settings")]
    [SerializeField] private int maxScoreDifference = 25;
    [Tooltip("How many points away from losing before a player's face changes to fear.")]
    [SerializeField] private int fearThreshold = 5;

    private bool isGameOver = false;

    void Start()
    {
        if (tugOfWarSlider != null)
        {
            // Configure the slider's range based on the max score difference.
            tugOfWarSlider.minValue = -maxScoreDifference;
            tugOfWarSlider.maxValue = maxScoreDifference;
            tugOfWarSlider.value = 0;
        }
        else
        {
            Debug.LogError("TugOfWarSlider is not assigned in the Inspector!", this.gameObject);
        }

        // Initialize faces to their normal expressions
        ResetFaces();
    }

    void Update()
    {
        // If the game is over or the manager doesn't exist, do nothing.
        if (isGameOver || MultiplayerMatchManager.Instance == null)
        {
            return;
        }

        // Calculate the current difference in scores.
        int myScore = MultiplayerMatchManager.Instance.GetMyScore();
        int opponentScore = MultiplayerMatchManager.Instance.GetOpponentScore();
        int scoreDifference = myScore - opponentScore;

        // Apply the difference to the slider's value.
        if (tugOfWarSlider != null)
        {
            tugOfWarSlider.value = scoreDifference;
        }

        // --- NEW: Calculate Territory and Update Bars ---
        UpdateProgressBars(scoreDifference);

        // Update the expressive face assets based on who is losing
        UpdateFaceExpressions(scoreDifference);

        // Check if one player has reached the max score difference.
        if (scoreDifference >= maxScoreDifference || scoreDifference <= -maxScoreDifference)
        {
            isGameOver = true;
            Debug.Log("Tug of War game over condition met!");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndGameMultiplayer();
            }
        }
    }

    /// <summary>
    /// Translates the raw score delta into filling percentages for both individual player bars.
    /// </summary>
    private void UpdateProgressBars(int scoreDifference)
    {
        // Total points spread possible (e.g., from -25 to +25 is a 50 point window)
        float totalRange = maxScoreDifference * 2f;

        // Normalize the position to a clean 0.0 to 1.0 percentage float scale
        float normalizedValue = (scoreDifference + maxScoreDifference) / totalRange;

        // Update the filling boundaries
        if (localFillBar != null)
        {
            // Local wins as value approaches 1.0, loses as it hits 0.0
            localFillBar.fillAmount = normalizedValue;
        }

        if (opponentFillBar != null)
        {
            // Opponent fills from the opposite direction (grows when local losing)
            opponentFillBar.fillAmount = 1f - normalizedValue;
        }
    }

    /// <summary>
    /// Checks the current score delta and swaps sprites if a player is dangerously close to losing.
    /// </summary>
    private void UpdateFaceExpressions(int scoreDifference)
    {
        // Check Local Player State (Losing when slider goes highly negative)
        if (localFaceImage != null && localNormalSprite != null && localFearSprite != null)
        {
            int localLossBoundary = -maxScoreDifference + fearThreshold;
            
            if (scoreDifference <= localLossBoundary)
            {
                localFaceImage.sprite = localFearSprite;
            }
            else
            {
                localFaceImage.sprite = localNormalSprite;
            }
        }

        // Check Opponent State (Losing when slider goes highly positive)
        if (opponentFaceImage != null && opponentNormalSprite != null && opponentFearSprite != null)
        {
            int opponentLossBoundary = maxScoreDifference - fearThreshold;
            
            if (scoreDifference >= opponentLossBoundary)
            {
                opponentFaceImage.sprite = opponentFearSprite;
            }
            else
            {
                opponentFaceImage.sprite = opponentNormalSprite;
            }
        }
    }

    private void ResetFaces()
    {
        if (localFaceImage != null && localNormalSprite != null) 
            localFaceImage.sprite = localNormalSprite;
            
        if (opponentFaceImage != null && opponentNormalSprite != null) 
            opponentFaceImage.sprite = opponentNormalSprite;
    }
}