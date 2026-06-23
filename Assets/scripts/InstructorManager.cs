using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Photon.Pun; 

public class InstructorManager : MonoBehaviour
{
    public static InstructorManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Image instructorDisplay;

    [Header("Character Art")]
    [SerializeField] private Sprite defaultIdleSprite; 
    [SerializeField] private Sprite[] encouragingSprites; 
    [SerializeField] private Sprite[] discouragingSprites; 

    [Header("Scale Settings")]
    [SerializeField] private float idleScale = 2.5f;
    [SerializeField] private float reactionScale = 8.4f;

    [Header("Settings")]
    [SerializeField] private float displayTime = 3f; 
    [SerializeField] private float cooldownTime = 5f; 
    
    // --- REMOVED: multiplayerScoreLeadThreshold ---
    [SerializeField] private float singlePlayerStaminaDangerPct = 0.3f; 

    private bool isMessageActiveOrOnCooldown = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        if (instructorDisplay != null && defaultIdleSprite != null)
        {
            instructorDisplay.sprite = defaultIdleSprite;
            instructorDisplay.rectTransform.localScale = new Vector3(idleScale, idleScale, idleScale);
        }
    }

    public void EvaluateGameState()
    {
        if (isMessageActiveOrOnCooldown) return;

        bool shouldReact = false;
        bool isPositive = true;

        // --- 1. MULTIPLAYER LOGIC ---
        if (MultiplayerMatchManager.Instance != null && MultiplayerMatchManager.Instance.IsMultiplayerGame())
        {
            float myScore = MultiplayerMatchManager.Instance.GetMyScore();
            float oppScore = MultiplayerMatchManager.Instance.GetOpponentScore();

            // If you are tied or leading, encourage!
            if (myScore >= oppScore)
            {
                isPositive = true;
                shouldReact = true;
            }
            // If you are losing, yell!
            else
            {
                isPositive = false;
                shouldReact = true;
            }
        }
        // --- 2. SINGLE PLAYER LOGIC ---
        else if (ScoreAndStaminaManager.Instance != null)
        {
            float maxStamina = ScoreAndStaminaManager.Instance.GetMaxStamina();
            float currentStamina = ScoreAndStaminaManager.Instance.GetCurrentStamina();
            
            if ((currentStamina / maxStamina) <= singlePlayerStaminaDangerPct)
            {
                isPositive = false;
                shouldReact = true;
            }
            else if (ScoreAndStaminaManager.Instance.GetCurrentScore() > 0 && ScoreAndStaminaManager.Instance.GetCurrentScore() % 20 == 0)
            {
                isPositive = true;
                shouldReact = true;
            }
        }

        if (shouldReact)
        {
            StartCoroutine(DisplayReactionRoutine(isPositive));
        }
    }

    private IEnumerator DisplayReactionRoutine(bool isPositive)
    {
        isMessageActiveOrOnCooldown = true;

        Sprite[] activePool = isPositive ? encouragingSprites : discouragingSprites;

        if (activePool == null || activePool.Length == 0)
        {
            isMessageActiveOrOnCooldown = false;
            yield break;
        }

        int randomIndex = Random.Range(0, activePool.Length);
        instructorDisplay.sprite = activePool[randomIndex];
        
        instructorDisplay.rectTransform.localScale = new Vector3(reactionScale, reactionScale, reactionScale);

        yield return new WaitForSeconds(displayTime);

        if (defaultIdleSprite != null)
        {
            instructorDisplay.sprite = defaultIdleSprite;
            instructorDisplay.rectTransform.localScale = new Vector3(idleScale, idleScale, idleScale);
        }

        yield return new WaitForSeconds(cooldownTime);

        isMessageActiveOrOnCooldown = false;
    }
}