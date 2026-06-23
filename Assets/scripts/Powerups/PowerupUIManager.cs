using UnityEngine;
using System.Collections.Generic;

public class PowerupUIManager : MonoBehaviour
{
    public static PowerupUIManager Instance { get; private set; }

    [Header("UI Containers")]
    public List<PowerupIconUI> localIcons;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    /// <summary>
    /// Searches the local player's UI and triggers the wipe animation for a specific powerup or debuff.
    /// </summary>
    public void ActivateIcon(string pName, float duration)
    {
        foreach (var icon in localIcons)
        {
            if (icon.powerupName == pName) 
            {
                icon.TriggerClockWipe(duration);
                return;
            }
        }
    }
}