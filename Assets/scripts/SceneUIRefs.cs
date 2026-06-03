using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A scene-specific singleton that holds references to all essential UI elements.
/// This allows persistent managers to easily find them after a scene reload.
/// </summary>
public class SceneUIRefs : MonoBehaviour
{
    // --- NEW: Static instance for easy access ---
    public static SceneUIRefs Instance { get; private set; }

    [Header("Scene UI References")]
    public  Image staminaBarFill;
    public   TMP_Text scoreText;
    public   GameObject gameOverUI;

    private void Awake()
    {
        // This ensures there is only one instance in the scene.
        // If another one exists, this new one destroys itself.
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        
        // Set the static instance to this object.
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
}
