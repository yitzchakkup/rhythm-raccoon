using UnityEngine;

public class UIRestartHelper : MonoBehaviour
{
    // The button will trigger this function
    public void TriggerRestart()
    {
        // We use the Singleton Instance so we don't need the Inspector!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            Debug.LogError("Button clicked, but GameManager.Instance is missing!");
        }
    }
}