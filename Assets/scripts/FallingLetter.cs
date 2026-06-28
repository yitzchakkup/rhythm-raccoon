using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class FallingLetter : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("Sound played when successfully typed")]
    [SerializeField] private AudioClip popSound;
    // ----------------------------------

    [Header("Input Settings")]
    [SerializeField] private Key letterKey;
    
    [Header("Visuals")]
    [SerializeField] private TMP_Text letterText;
    
    [Header("Movement Settings")]
    [SerializeField] private float fallSpeed = 2f;

    [Header("Juice Settings (Organic)")]
    [SerializeField] private float swayAmount = 0.5f;      
    [SerializeField] private float swaySpeed = 0.8f;       
    [SerializeField] private float maxRockAngle = 15f;     
    [SerializeField] private float rockSpeed = 1f;         
    
    [Header("Juice Settings (Zone Feedback)")]
    [Tooltip("The color when the letter is falling and cannot be pressed")]
    [SerializeField] private Color disabledColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [Tooltip("How much it inflates when in the zone")]
    [SerializeField] private float scaleMultiplier = 1.7f; 
    [Tooltip("How fast the heartbeat pulses")]
    [SerializeField] private float pulseSpeed = 6f;

    [Header("Effects")]
    [SerializeField] private GameObject explosionPrefab;

    public bool inZone { get; private set; } = false;
    public bool isPressed { get; private set; } = false;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private float startX;
    private float noiseOffset;
    
    private bool isPopping = false;
    private bool hasEnteredZone = false;
    private Vector3 baseScale;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) 
        {
            originalColor = spriteRenderer.color;
            // Start grayed out immediately
            spriteRenderer.color = disabledColor; 
        }

        startX = transform.position.x; 
        noiseOffset = Random.Range(0f, 1000f); 
        baseScale = transform.localScale;
    }

    void Update()
    {
        if (isPopping) return;
        
        // 1. Calculate downward movement
        float newY = transform.position.y - (fallSpeed * Time.deltaTime);
        
        // 2. Calculate organic side-to-side sway
        float noiseValX = Mathf.PerlinNoise(Time.time * swaySpeed, noiseOffset);
        float organicSway = (noiseValX * 2f - 1f) * swayAmount;
        float newX = startX + organicSway;

        transform.position = new Vector3(newX, newY, transform.position.z);

        // 3. Calculate organic rocking
        float noiseValRot = Mathf.PerlinNoise(noiseOffset, Time.time * rockSpeed);
        float organicRock = (noiseValRot * 2f - 1f) * maxRockAngle;

        transform.rotation = Quaternion.Euler(0, 0, organicRock);

        // 4. Input Checking
        if (Keyboard.current != null && letterKey != Key.None)
        {
            bool isKeyCurrentlyHeld = Keyboard.current[letterKey].isPressed;
            isPressed = isKeyCurrentlyHeld && inZone;
        }

        // 5. Zone Feedback (Heartbeat & Color)
        if (inZone)
        {
            if (!hasEnteredZone)
            {
                hasEnteredZone = true;
                // Capture the exact scale set by the WordGenerator right as it enters
                baseScale = transform.localScale; 
            }

            // Apply Heartbeat Scale
            float wave = (Mathf.Sin(Time.time * pulseSpeed) * 0.5f) + 0.5f;
            float currentScaleLerp = Mathf.Lerp(1f, scaleMultiplier, wave);
            transform.localScale = baseScale * currentScaleLerp;
        }
        else if (hasEnteredZone)
        {
            // If it falls completely past the zone, reset its scale
            hasEnteredZone = false;
            transform.localScale = baseScale;
        }

        // Apply visual colors
        if (spriteRenderer != null)
        {
            if (isPressed)
            {
                spriteRenderer.color = Color.green;
            }
            else
            {
                // Snap to original color if in zone, otherwise stay gray
                spriteRenderer.color = inZone ? originalColor : disabledColor; 
            }
        }
    }

    public void SetFallSpeed(float newSpeed) { fallSpeed = newSpeed; }

    public void SetupRandomLetter(Key newKey, string displayText = null)
    {
        letterKey = newKey;
        if (letterText != null) 
        {
            letterText.text = string.IsNullOrEmpty(displayText) ? newKey.ToString() : displayText;
        }
    }

    public int GetScoreValue()
    {
        if (inZone) return 1;
        return 0;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("TargetZone"))
        {
            inZone = true;
        }
        else if (other.CompareTag("missed"))
        {
            if (ScoreAndStaminaManager.Instance != null)
            {
                ScoreAndStaminaManager.Instance.MissedLetter();
            }

            Destroy(gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("TargetZone"))
        {
            inZone = false;
            isPressed = false; 
        }
    }
    
    public void TriggerPopAndDestroy()
    {
        if (!isPopping) 
        {
            // --- NEW: Play Pop Sound ---
            if (popSound != null && AudioManager.Instance != null)
            {
                // Passing 'true' to randomize pitch for that juicy typewriter feel!
                AudioManager.Instance.PlaySFX(popSound, true); 
            }

            StartCoroutine(PopRoutine());
        }
    }

    private IEnumerator PopRoutine()
    {
        isPopping = true;

        fallSpeed = 0f;
        swayAmount = 0f;

        // Use baseScale so it doesn't accidentally pop at a massive size if clicked during a heartbeat peak
        Vector3 startScale = baseScale; 
        Vector3 popScale = startScale * 1.3f; 

        float popUpTime = 0.05f; 
        float shrinkTime = 0.1f; 
        float timer = 0f;

        while (timer < popUpTime)
        {
            transform.localScale = Vector3.Lerp(startScale, popScale, timer / popUpTime);
            timer += Time.deltaTime;
            yield return null;
        }

        timer = 0f;

        while (timer < shrinkTime)
        {
            transform.localScale = Vector3.Lerp(popScale, Vector3.zero, timer / shrinkTime);
            timer += Time.deltaTime;
            yield return null;
        }
        
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}