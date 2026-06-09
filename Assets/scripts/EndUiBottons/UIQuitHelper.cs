using UnityEngine;

public class UIQuitHelper : MonoBehaviour
{
    // The button will trigger this function
    public void TriggerQuit()
    {
        // We use the Singleton Instance so we don't need the Inspector!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
        else
        {
            Debug.LogError("Button clicked, but GameManager.Instance is missing!");
        }
    }
}