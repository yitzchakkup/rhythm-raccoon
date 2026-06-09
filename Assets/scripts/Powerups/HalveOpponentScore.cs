using UnityEngine;

public class HalveOpponentScore : Powerup
{
    // We can use the Reset() method to automatically set the dropdown 
    // to "Opponent" in the Inspector when you attach this script!
    private void Reset()
    {
        target = PowerupTarget.Opponent;
    }

    public override void ApplyEffect()
    {
        if (MultiplayerMatchManager.Instance != null)
        {
            Debug.Log($"Sending {powerupName} attack to opponent!");
            // Fire the specific attack name over the internet
            MultiplayerMatchManager.Instance.SendAttackToOpponent("HalveScore");
        }
    }
}