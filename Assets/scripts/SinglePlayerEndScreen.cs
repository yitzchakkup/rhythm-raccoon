using UnityEngine;
using TMPro;
using UnityEngine.UI; // --- BROUGHT THIS BACK: Required for standard Unity Buttons ---

public class SinglePlayerEndScreen : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text scoreText;
    
    [Header("Buttons")]
    public HangingSignButton retryButton; // The juicy custom sign
    public Button mainMenuButton;         // The standard Unity button

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
        if (scoreText != null)
        {
            scoreText.text = $"{finalScore}";
        }
    }

    private void OnRetryClicked()
    {
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