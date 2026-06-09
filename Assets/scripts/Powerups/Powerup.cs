using UnityEngine;

// --- NEW: Define the categories of powerups ---
public enum PowerupTarget 
{ 
    Self,       // Buffs (Double Score, Stamina, etc.)
    Opponent    // Attacks (Halve Score, Drain Stamina, etc.)
}

public abstract class Powerup : MonoBehaviour
{
    [Header("Powerup Settings")]
    public string powerupName = "Unknown Powerup";
    
    // --- NEW: Expose the target type to the Inspector ---
    public PowerupTarget target = PowerupTarget.Self; 

    public abstract void ApplyEffect();
}