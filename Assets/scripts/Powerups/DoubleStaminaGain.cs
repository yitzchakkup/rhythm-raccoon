using UnityEngine;

public class DoubleStaminaGain : Powerup
{
    [Header("Double Stamina Settings")]
    public float staminaMultiplier = 2f;
    public float durationInSeconds = 10f;

    public override void ApplyEffect()
    {
        // --- UPDATED: Calling the new unified manager ---
        if (ScoreAndStaminaManager.Instance != null)
        {
            ScoreAndStaminaManager.Instance.ActivateStaminaMultiplier(staminaMultiplier, durationInSeconds);
            Debug.Log($"Applied {powerupName}!");
        }
    }
}