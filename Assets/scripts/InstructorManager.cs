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

    // --- NEW: Audio Settings ---
    [Header("Audio")]
    [Tooltip("The swoosh/pop sound when a new slide appears")]
    [SerializeField] private AudioClip slideTransitionSound;

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
            MatchSyncManager.Instance.LocalPlayerFinishedTutorial();
            return;
        }

        StartCoroutine(PlaySlideshowRoutine());
    }

    private IEnumerator PlaySlideshowRoutine()
    {
        isTutorialActive = true;
        isMessageActiveOrOnCooldown = true; 

        // Initial setup for the very first slide
        if (instructorDisplay != null)
        {
            // --- NEW: Play sound for the very first slide! ---
            if (slideTransitionSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(slideTransitionSound, true);
            }

            instructorDisplay.sprite = tutorialSlides[0];
            instructorDisplay.rectTransform.localScale = new Vector3(reactionScale, reactionScale, reactionScale);
        }

        // Loop through the slides (starting at index 1 since we already showed index 0)
        for (int i = 1; i < tutorialSlides.Length; i++)
        {
            yield return new WaitForSeconds(timePerSlide);

            if (instructorDisplay != null) 
            {
                yield return StartCoroutine(TransitionSlide(tutorialSlides[i]));
            }
        }

        yield return new WaitForSeconds(timePerSlide);

        if (instructorDisplay != null && defaultIdleSprite != null)
        {
            instructorDisplay.sprite = defaultIdleSprite;
            instructorDisplay.rectTransform.localScale = new Vector3(idleScale, idleScale, idleScale);
        }

        isTutorialActive = false;
        
        if (MatchSyncManager.Instance != null)
        {
            MatchSyncManager.Instance.LocalPlayerFinishedTutorial();
        }

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

        // --- OPTIONAL: You could also add a sound effect here for when she yells during the game! ---

        yield return new WaitForSeconds(displayTime);

        if (defaultIdleSprite != null)
        {
            instructorDisplay.sprite = defaultIdleSprite;
            instructorDisplay.rectTransform.localScale = new Vector3(idleScale, idleScale, idleScale);
        }

        yield return new WaitForSeconds(cooldownTime);

        isMessageActiveOrOnCooldown = false;
    }
    
    private IEnumerator TransitionSlide(Sprite nextSprite)
    {
        // --- NEW: Play the swoosh sound right as the squish anticipation starts! ---
        if (slideTransitionSound != null && AudioManager.Instance != null)
        {
            // Passing 'true' randomizes the pitch slightly so the swooshes don't sound repetitive
            AudioManager.Instance.PlaySFX(slideTransitionSound, true);
        }

        Vector3 baseScale = new Vector3(reactionScale, reactionScale, reactionScale);
        Vector3 squishScale = baseScale * 0.7f; 
        Vector3 stretchScale = baseScale * 1.1f; 

        float shrinkDuration = 0.1f;
        float growDuration = 0.15f;
        float settleDuration = 0.05f;

        float timer = 0f;

        // Phase 1: Anticipation (Squish Down)
        while (timer < shrinkDuration)
        {
            instructorDisplay.rectTransform.localScale = Vector3.Lerp(baseScale, squishScale, timer / shrinkDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        // --- THE MAGIC MOMENT: Swap the image while it is squished! ---
        instructorDisplay.sprite = nextSprite;

        // Phase 2: The Pop (Overshoot)
        timer = 0f;
        while (timer < growDuration)
        {
            instructorDisplay.rectTransform.localScale = Vector3.Lerp(squishScale, stretchScale, timer / growDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        // Phase 3: Settle into place
        timer = 0f;
        while (timer < settleDuration)
        {
            instructorDisplay.rectTransform.localScale = Vector3.Lerp(stretchScale, baseScale, timer / settleDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        instructorDisplay.rectTransform.localScale = baseScale;
    }
}