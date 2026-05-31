using UnityEngine;
using TMPro; // --- NEW: Required to talk to TextMeshPro UI ---

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private TMP_Text scoreText; // Drag your UI text here

    public int Score { get; private set; } = 0;

    private int currentMultiplier = 1;
    private float multiplierTimer = 0f;

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

    private void Start()
    {
        // Force the UI to say "Score: 0" the moment the game starts
        UpdateScoreUI(); 
    }

    void Update()
    {
        if (multiplierTimer > 0)
        {
            multiplierTimer -= Time.deltaTime;
            
            if (multiplierTimer <= 0)
            {
                currentMultiplier = 1;
                multiplierTimer = 0f;
                Debug.Log("Score multiplier has ended! Back to 1x.");
            }
        }
    }

    public void AddScore(int pointsToAdd)
    {
        int calculatedPoints = pointsToAdd * currentMultiplier;
        Score += calculatedPoints;
        
        // --- NEW: Update the visual text on the screen ---
        UpdateScoreUI();
        
        Debug.Log($"Word cleared! +{calculatedPoints} points (Multiplier: {currentMultiplier}x). Total Score: {Score}");
    }

    public void ActivateMultiplier(int multiplier, float duration)
    {
        currentMultiplier = multiplier;
        multiplierTimer = duration; 
        Debug.Log($"Multiplier Activated! {currentMultiplier}x score for {duration} seconds!");
    }

    // --- NEW: Helper method to format and update the text ---
    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {Score}";
        }
    }
}