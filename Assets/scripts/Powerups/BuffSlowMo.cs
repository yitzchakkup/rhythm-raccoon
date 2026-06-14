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
        // Buffs affect us directly, so we find our local generator and warp the speed!
        WordGenerator generator = FindObjectOfType<WordGenerator>();
        
        if (generator != null)
        {
            generator.TriggerSpeedAttack(speedMultiplier, duration);
            Debug.Log($"Applied {powerupName}! Time slowed.");
        }
    }
}