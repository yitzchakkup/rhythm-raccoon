using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class ScoreAndStaminaManager : MonoBehaviour
{
    public static ScoreAndStaminaManager Instance { get; private set; }

    [Header("Score Settings")]
    public int Score { get; private set; } = 0;
    private float scoreMultiplier = 1f;
    private float scoreMultiplierTimer = 0f;

    [Header("Stamina Settings")]
    [SerializeField] private float staminaRewardAmount = 5f;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float startingStamina = 75f;
    [SerializeField] private float staminaDrainAmount = 1f;
    [SerializeField] private float staminaDrainTickRate = 0.05f;
    private float currentStamina;
    private float staminaMultiplier = 1f;
    private float staminaMultiplierTimer = 0f;
    private Coroutine drainCoroutine;

    private TMP_Text scoreText;
    private Image staminaBarFill;

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

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SceneUIRefs.Instance != null)
        {
            scoreText = SceneUIRefs.scoreText;
            staminaBarFill = SceneUIRefs.staminaBarFill;
        }
        Initialize();
    }

    private void Initialize()
    {
        Score = 0;
        scoreMultiplier = 1;
        scoreMultiplierTimer = 0f;
        UpdateScoreUI();

        if (drainCoroutine != null) StopCoroutine(drainCoroutine);
        
        currentStamina = startingStamina;
        staminaMultiplier = 1f;
        staminaMultiplierTimer = 0f;
        UpdateStaminaUI();
        
        drainCoroutine = StartCoroutine(DrainStamina());
    }

    void Update()
    {
        if (scoreMultiplierTimer > 0)
        {
            scoreMultiplierTimer -= Time.deltaTime;
            if (scoreMultiplierTimer <= 0) scoreMultiplier = 1;
        }

        if (staminaMultiplierTimer > 0)
        {
            staminaMultiplierTimer -= Time.deltaTime;
            if (staminaMultiplierTimer <= 0) staminaMultiplier = 1f;
        }
    }

    public void AddScoreAndStamina(int pointsToAdd)
    {
        int calculatedPoints = Mathf.RoundToInt(pointsToAdd * scoreMultiplier);
        Score += calculatedPoints;
        UpdateScoreUI();
        
        // --- MULTIPLAYER HOOK: Broadcast Score ---
        if (MultiplayerMatchManager.Instance != null)
        {
            MultiplayerMatchManager.Instance.SyncMyScore(Score);
        }
        
        float calculatedStamina = staminaRewardAmount * staminaMultiplier;
        AddStamina(calculatedStamina);
    }

    public void AddStamina(float amount)
    {
        if (currentStamina <= 0) return;

        currentStamina = Mathf.Clamp(currentStamina + amount, 0, maxStamina);
        UpdateStaminaUI();

        // --- MULTIPLAYER HOOK: Broadcast Stamina on Gain ---
        if (MultiplayerMatchManager.Instance != null)
        {
            MultiplayerMatchManager.Instance.SyncMyStamina(currentStamina, maxStamina);
        }
    }

    public void ActivateScoreMultiplier(float multiplier, float duration)
    {
        scoreMultiplier = multiplier;
        scoreMultiplierTimer = duration; 
    }

    public void ActivateStaminaMultiplier(float multiplier, float duration)
    {
        staminaMultiplier = multiplier;
        staminaMultiplierTimer = duration;
    }

    private IEnumerator DrainStamina()
    {
        while (true)
        {
            currentStamina -= staminaDrainAmount;
            UpdateStaminaUI();

            // --- MULTIPLAYER HOOK: Broadcast Stamina on Drain ---
            if (MultiplayerMatchManager.Instance != null)
            {
                MultiplayerMatchManager.Instance.SyncMyStamina(currentStamina, maxStamina);
            }

            if (currentStamina <= 0)
            {
                currentStamina = 0;
                UpdateStaminaUI();
                
                if (GameManager.Instance != null) GameManager.Instance.EndGame();
                yield break;
            }

            yield return new WaitForSeconds(staminaDrainTickRate);
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = $"Score: {Score}";
    }

    private void UpdateStaminaUI()
    {
        if (staminaBarFill != null) staminaBarFill.fillAmount = currentStamina / maxStamina;
    }
}