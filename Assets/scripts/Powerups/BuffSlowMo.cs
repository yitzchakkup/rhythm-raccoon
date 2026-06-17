using UnityEngine;

public class BuffSlowMo : Powerup
{
    [Header("Slow Mo Settings")]
    public float speedMultiplier = 0.4f; // 40% speed
    public float duration = 5f;          // Lasts 5 seconds

    private void Reset()
    {
        target = PowerupTarget.Self;
        powerupName = "Zen Mode";
    }

    public override void ApplyEffect()
    {
        // --- MULTIPLAYER/UNITY 6 FIX: Must be FindAnyObjectByType ---
        WordGenerator generator = Object.FindAnyObjectByType<WordGenerator>();
        
        if (generator != null)
        {
            generator.TriggerSpeedAttack(speedMultiplier, duration);
            Debug.Log($"Applied {powerupName}! Time slowed.");
        }
    }
}