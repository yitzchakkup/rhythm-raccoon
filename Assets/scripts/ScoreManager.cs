using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int Score { get; private set; } = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    // The WordGenerator will call this when a word is successfully typed
    public void AddScore(int pointsToAdd)
    {
        Score += pointsToAdd;
        Debug.Log($"Word cleared! +{pointsToAdd} points. Total Score: {Score}");
    }
}