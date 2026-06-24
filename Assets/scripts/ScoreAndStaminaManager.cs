using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class ScoreAndStaminaManager : MonoBehaviour
{
    public static ScoreAndStaminaManager Instance { get; private set; }

    [Header("Score Settings")]
    public float Score { get; private set; } = 0f;
    private float scoreMultiplier = 1f;
    private float scoreMultiplierTimer = 0f;

    [Header("Stamina Settings")]
    [SerializeField] private float staminaRewardAmount = 5f;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float startingStamina = 75f;
    [SerializeField] private float staminaDrainAmount = 1f;
    [SerializeField] private float staminaDrainTickRate = 0.05f;
    [SerializeField] private float staminaHeadEmptyYPosition; // Y position when stamina is zero
    [SerializeField] private float staminaHeadYOffset = 0f;
    [SerializeField] private float lowStaminaThreshold = 25f;
    [SerializeField] private float gracePeriodDuration = 4f;
    [SerializeField] private float gracePeriodDrainMultiplier = 0.1f;
    private float gracePeriodTimer = 0f;
    [SerializeField] private float singlePlayerMissPenaltyMultiplier = 0.5f;
    [SerializeField] private float multiplayerMissScorePenalty = 0.5f;
    private float currentStamina;
    private float staminaMultiplier = 1f;
    private float staminaMultiplierTimer = 0f;
    private Coroutine drainCoroutine;
    private float staminaHeadFullYPosition; // Starting Y position when stamina is full

    private TMP_Text scoreText;
    private Image staminaFill;
    private RectTransform staminaHead;
    private Image staminaHeadImage;
    private Sprite normalStaminaFillSprite;
    private Sprite normalHeadSprite;

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
            staminaFill = SceneUIRefs.staminaFill;
            staminaHead = SceneUIRefs.staminaHead;
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
        gracePeriodTimer = gracePeriodDuration;

        if (staminaHead != null)
        {
            staminaHeadFullYPosition = staminaHead.anchoredPosition.y;
        }

        if (staminaFill != null)
        {
            normalStaminaFillSprite = staminaFill.sprite;
        }
        if (staminaHead != null)
        {
            staminaHeadImage = staminaHead.GetComponent<Image>();
            if (staminaHeadImage != null)
            {
                normalHeadSprite = staminaHeadImage.sprite;
            }
        }
        
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

        if (gracePeriodTimer > 0)
        {
            gracePeriodTimer -= Time.deltaTime;
        }
    }

    public void AddScoreAndStamina(float pointsToAdd)
    {
        float calculatedPoints = pointsToAdd * scoreMultiplier;
        Score += calculatedPoints;
        UpdateScoreUI();
        
        if (MultiplayerMatchManager.Instance != null && MultiplayerMatchManager.Instance.IsMultiplayerGame())
        {
            MultiplayerMatchManager.Instance.SyncMyScore(Score);
        }
        
        float calculatedStamina = staminaRewardAmount * staminaMultiplier;
        AddStamina(calculatedStamina);
        
        if (InstructorManager.Instance != null)
        {
            InstructorManager.Instance.EvaluateGameState();
        }
    }

    public void MissedLetter()
    {
        bool isMultiplayer = MultiplayerMatchManager.Instance != null && MultiplayerMatchManager.Instance.IsMultiplayerGame();

        if (!isMultiplayer)
        {
            float penalty = staminaRewardAmount * singlePlayerMissPenaltyMultiplier;
            currentStamina = Mathf.Max(0, currentStamina - penalty);
            UpdateStaminaUI();
            if (currentStamina <= 0)
            {
                GameManager.Instance.EndGame();
            }
        }
        else
        {
            Score = Score - multiplayerMissScorePenalty;
            UpdateScoreUI();
            MultiplayerMatchManager.Instance.SyncMyScore(Score);
        }
    }

    public void AddStamina(float amount)
    {
        if (MultiplayerMatchManager.Instance != null && MultiplayerMatchManager.Instance.IsMultiplayerGame())
        {
            return;
        }

        if (currentStamina <= 0) return;

        currentStamina = Mathf.Clamp(currentStamina + amount, 0, maxStamina);
        UpdateStaminaUI();
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
        if (MultiplayerMatchManager.Instance != null && MultiplayerMatchManager.Instance.IsMultiplayerGame())
        {
            Debug.Log("Stamina drain disabled for multiplayer match.");
            yield break;
        }

        while (true)
        {
            float currentDrain = (gracePeriodTimer > 0) ? staminaDrainAmount * gracePeriodDrainMultiplier : staminaDrainAmount;
            currentStamina -= currentDrain;
            UpdateStaminaUI();

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
        if (scoreText == null) return;

        bool isMultiplayer = MultiplayerMatchManager.Instance != null && MultiplayerMatchManager.Instance.IsMultiplayerGame();

        if (!isMultiplayer)
        {
            scoreText.text = $"Score: {Mathf.RoundToInt(Score)}";
        }
        else
        {
            scoreText.text = $"Score: {Score:F1}";
        }
    }

    private void UpdateStaminaUI()
    {
        if (staminaFill != null)
        {
            staminaFill.fillAmount = currentStamina / maxStamina;
        }

        if (staminaHead != null)
        {
            float percentage = currentStamina / maxStamina;
            float newY = Mathf.Lerp(staminaHeadEmptyYPosition, staminaHeadFullYPosition, percentage) + staminaHeadYOffset;
            staminaHead.anchoredPosition = new Vector2(staminaHead.anchoredPosition.x, newY);
        }

        if (currentStamina <= lowStaminaThreshold)
        {
            if (staminaFill != null && SceneUIRefs.lowStaminaFillSprite != null)
            {
                staminaFill.sprite = SceneUIRefs.lowStaminaFillSprite;
            }
            if (staminaHeadImage != null && SceneUIRefs.sadHeadSprite != null)
            {
                staminaHeadImage.sprite = SceneUIRefs.sadHeadSprite;
            }
        }
        else
        {
            if (staminaFill != null)
            {
                staminaFill.sprite = normalStaminaFillSprite;
            }
            if (staminaHeadImage != null)
            {
                staminaHeadImage.sprite = normalHeadSprite;
            }
        }
    }
    
    public float GetCurrentScore()
    {
        return Score;
    }

    public float GetCurrentStamina()
    {
        return currentStamina;
    }

    public float GetMaxStamina()
    {
        return maxStamina;
    }
}