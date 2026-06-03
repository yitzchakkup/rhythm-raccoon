using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Stamina")]
    [SerializeField] private float staminaRewardAmount = 5f;

    public int Score { get; private set; } = 0;

    private TMP_Text scoreText;
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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SceneUIRefs.Instance != null)
        {
            // --- DEBUGGING: Check if the specific reference is valid ---
            if (SceneUIRefs.Instance.scoreText != null)
            {
                scoreText = SceneUIRefs.Instance.scoreText;
                Debug.Log("ScoreManager successfully found its UI reference.");
            }
            else
            {
                Debug.LogError("CRITICAL: SceneUIRefs exists, but its 'scoreText' field is not assigned in the Inspector!");
            }
        }
        else
        {
            Debug.LogError("CRITICAL: ScoreManager could not find SceneUIRefs instance!");
        }

        InitializeScore();
    }

    private void InitializeScore()
    {
        Score = 0;
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
            }
        }
    }

    public void AddScore(int pointsToAdd)
    {
        int calculatedPoints = pointsToAdd * currentMultiplier;
        Score += calculatedPoints;
        
        UpdateScoreUI();
        
        if (StaminaManager.Instance != null)
        {
            StaminaManager.Instance.AddStamina(staminaRewardAmount);
        }
    }

    public void ActivateMultiplier(int multiplier, float duration)
    {
        currentMultiplier = multiplier;
        multiplierTimer = duration; 
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {Score}";
        }
        else
        {
            // This log will now only appear if the reference was truly never assigned.
            Debug.LogWarning("ScoreManager cannot update UI because its scoreText reference is missing.");
        }
    }
}