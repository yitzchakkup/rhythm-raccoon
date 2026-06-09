using UnityEngine;

public class HalveOpponentStamina : Powerup
{
    private void Reset()
    {
        target = PowerupTarget.Opponent;
    }

    public override void ApplyEffect()
    {
        if (MultiplayerMatchManager.Instance != null)
        {
            Debug.Log($"Sending {powerupName} attack to opponent!");
            MultiplayerMatchManager.Instance.SendAttackToOpponent("HalveStamina");
        }
    }
}