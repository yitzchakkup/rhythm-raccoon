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

    [Header("Tutorial Sequence")]
    [Tooltip("Drag all your designer's tutorial banners here in order")]
    [SerializeField] private Sprite[] tutorialSlides; 
    [Tooltip("How long each image stays on screen")]
    [SerializeField] private float timePerSlide = 2.5f; 

    [Header("Scale Settings")]
    [SerializeField] private float idleScale = 2.5f;
    [SerializeField] private float reactionScale = 8.4f;

    [Header("Settings")]
    [SerializeField] private float displayTime = 3f; 
    [SerializeField] private float cooldownTime = 5f; 
    [SerializeField] private float singlePlayerStaminaDangerPct = 0.3f; 

    private bool isMessageActiveOrOnCooldown = false;
    public bool isTutorialActive { get; private set; } = false; 

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

    // --- 1. AUTOMATIC TUTORIAL LOGIC ---

    public void StartTutorialSequence()
    {
        if (tutorialSlides == null || tutorialSlides.Length == 0)
        {
            // Failsafe: If you forgot to assign the images, just skip to the game!
            MatchSyncManager.Instance.LocalPlayerFinishedTutorial();
            return;
        }

        StartCoroutine(PlaySlideshowRoutine());
    }

    private IEnumerator PlaySlideshowRoutine()
    {
        isTutorialActive = true;
        isMessageActiveOrOnCooldown = true; // Lock normal gameplay reactions

        // Scale up the UI so the banner is readable
        if (instructorDisplay != null)
        {
            instructorDisplay.rectTransform.localScale = new Vector3(reactionScale, reactionScale, reactionScale);
        }

        // Loop through the array of images
        for (int i = 0; i < tutorialSlides.Length; i++)
        {
            if (instructorDisplay != null) instructorDisplay.sprite = tutorialSlides[i];
            
            // Wait for the players to read it
            yield return new WaitForSeconds(timePerSlide);
        }

        // Slideshow finished! Return to normal idle state
        if (instructorDisplay != null && defaultIdleSprite != null)
        {
            instructorDisplay.sprite = defaultIdleSprite;
            instructorDisplay.rectTransform.localScale = new Vector3(idleScale, idleScale, idleScale);
        }

        isTutorialActive = false;
        
        // Tell the network this player is done reading!
        if (MatchSyncManager.Instance != null)
        {
            MatchSyncManager.Instance.LocalPlayerFinishedTutorial();
        }

        // Start cooldown so they don't immediately get yelled at when the game starts
        yield return new WaitForSeconds(cooldownTime);
        isMessageActiveOrOnCooldown = false; 
    }

    // --- 2. NORMAL GAMEPLAY LOGIC ---

    public void EvaluateGameState()
    {
        if (isMessageActiveOrOnCooldown || isTutorialActive) return;

        bool shouldReact = false;
        bool isPositive = true;

        if (MultiplayerMatchManager.Instance != null && MultiplayerMatchManager.Instance.IsMultiplayerGame())
        {
            float myScore = MultiplayerMatchManager.Instance.GetMyScore();
            float oppScore = MultiplayerMatchManager.Instance.GetOpponentScore();

            if (myScore >= oppScore)
            {
                isPositive = true;
                shouldReact = true;
            }
            else
            {
                isPositive = false;
                shouldReact = true;
            }
        }
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