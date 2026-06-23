using UnityEngine;

public class DoubleScorePowerup : Powerup
{
    [Header("Double Score Settings")]
    public int scoreMultiplier = 2;
    public float durationInSeconds = 10f;

    public override void ApplyEffect()
    {
        // --- UPDATED: Calling the new unified manager ---
        if (ScoreAndStaminaManager.Instance != null)
        {
            ScoreAndStaminaManager.Instance.ActivateScoreMultiplier(scoreMultiplier, durationInSeconds);
            Debug.Log($"Applied {powerupName}!");
            
            if (PowerupUIManager.Instance != null)
            {
                // Match the exact string you typed in the Inspector, and pass the duration!
                PowerupUIManager.Instance.ActivateIcon("Double Score", durationInSeconds); 
            }
        }
    }
}