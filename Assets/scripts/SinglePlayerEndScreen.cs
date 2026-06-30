using UnityEngine;
using TMPro;
using UnityEngine.UI; 

public class SinglePlayerEndScreen : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text scoreText;
    
    [Header("Buttons")]
    public HangingSignButton retryButton; 
    public HangingSignButton mainMenuButton;         

    private void OnEnable()
    {
        // Safely clear old listeners
        if (retryButton != null) retryButton.onClick.RemoveAllListeners();
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveAllListeners();

        // Assign the clicks via code
        if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    public void SetupScreen(int finalScore)
    {
        // --- JUICY DEBUGGING ---
        // This will print exactly what number the GameManager handed to the UI
        Debug.Log($"<color=cyan>[SinglePlayer UI]</color> SetupScreen called! Received score: {finalScore}");

        if (scoreText != null)
        {
            scoreText.text = $"{finalScore}";
        }
        else 
        {
            Debug.LogWarning("<color=red>[SinglePlayer UI]</color> You forgot to drag the Score Text into the Inspector!");
        }
    }

    private void OnRetryClicked()
    {
        // Disable instantly to prevent double-clicking the juice
        if (retryButton != null) retryButton.interactable = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartCurrentLevel(); 
        }
    }

    private void OnMainMenuClicked()
    {
        // Disable the standard button instantly so they can't double-click it
        if (mainMenuButton != null) mainMenuButton.interactable = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu();
        }
    }
}