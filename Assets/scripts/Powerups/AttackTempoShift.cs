using UnityEngine;

public class AttackTempoShift : Powerup
{
    [Header("Tempo Shift Settings")]
    public float speedMultiplier = 2.5f; 
    public float duration = 4f;

    private void Reset()
    {
        target = PowerupTarget.Opponent;
        powerupName = "Tempo Shift";
    }

    public override void ApplyEffect()
    {
        if (MultiplayerMatchManager.Instance != null)
        {
            Debug.Log($"Sending {powerupName} attack to opponent!");
            
            // Fire the specific attack name over the internet.
            // (The opponent's MultiplayerMatchManager will receive this string and trigger the speed up on their own WordGenerator).
            MultiplayerMatchManager.Instance.SendAttackToOpponent("TempoShift");
        }
    }
}