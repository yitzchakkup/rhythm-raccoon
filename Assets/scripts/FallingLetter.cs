using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class FallingLetter : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private Key letterKey;
    [Header("Visuals")]
    [SerializeField] private TMP_Text letterText;
    [Header("Movement Settings")]
    [SerializeField] private float fallSpeed = 2f;

    // --- UPDATED: Organic Juice Settings ---
    [Header("Juice Settings (Organic)")]
    [SerializeField] private float swayAmount = 0.5f;      // Max drift distance
    [SerializeField] private float swaySpeed = 0.8f;       // How fast the wind pushes
    [SerializeField] private float maxRockAngle = 15f;     // NEVER flips! (Locks between -15 and 15 degrees)
    [SerializeField] private float rockSpeed = 1f;         // How fast it wobbles
    
    [Header("Effects")]
    [SerializeField] private GameObject explosionPrefab;

    public bool inZone { get; private set; } = false;
    public bool isPressed { get; private set; } = false;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private float startX;
    private float noiseOffset;
    
    private bool isPopping = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        startX = transform.position.x; 
        
        // Give each letter a completely unique starting point in the Perlin noise map
        noiseOffset = Random.Range(0f, 1000f); 
    }

    void Update()
    {
        if (isPopping) return;
        
        // 1. Calculate downward movement
        float newY = transform.position.y - (fallSpeed * Time.deltaTime);
        
        // 2. Calculate organic side-to-side sway using Perlin Noise
        // PerlinNoise returns 0 to 1. We multiply by 2 and subtract 1 to get -1 to 1.
        float noiseValX = Mathf.PerlinNoise(Time.time * swaySpeed, noiseOffset);
        float organicSway = (noiseValX * 2f - 1f) * swayAmount;
        float newX = startX + organicSway;

        // Apply Position
        transform.position = new Vector3(newX, newY, transform.position.z);

        // 3. Calculate organic rocking (using reversed coordinates so it doesn't perfectly match the sway)
        float noiseValRot = Mathf.PerlinNoise(noiseOffset, Time.time * rockSpeed);
        float organicRock = (noiseValRot * 2f - 1f) * maxRockAngle;

        // Apply Rotation (Strictly clamped by maxRockAngle so it stays readable!)
        transform.rotation = Quaternion.Euler(0, 0, organicRock);

        // --- Standard Logic ---
        if (Keyboard.current != null && letterKey != Key.None)
        {
            bool isKeyCurrentlyHeld = Keyboard.current[letterKey].isPressed;
            isPressed = isKeyCurrentlyHeld && inZone;
        }

        if (isPressed)
        {
            spriteRenderer.color = Color.green;
        }
        else
        {
            spriteRenderer.color = originalColor; 
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
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("TargetZone"))
        {
            inZone = false;
        }
        else if (other.CompareTag("LineOnderGame")) Destroy(gameObject); 

        if (!inZone)
        {
            isPressed = false; 
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor; 
            }
        }
    }
    
    public void TriggerPopAndDestroy()
    {
        // Prevent it from triggering twice if multiple frames overlap
        if (!isPopping) 
        {
            StartCoroutine(PopRoutine());
        }
    }

    private IEnumerator PopRoutine()
    {
        isPopping = true;

        // 1. Freeze the movement so it pops exactly where the player typed it
        fallSpeed = 0f;
        swayAmount = 0f;

        Vector3 startScale = transform.localScale;
        Vector3 popScale = startScale * 1.3f; // The "swell" size before it disappears

        float popUpTime = 0.05f; // Super fast swell
        float shrinkTime = 0.1f; // Fast shrink
        float timer = 0f;

        // Phase 1: Swell up
        while (timer < popUpTime)
        {
            transform.localScale = Vector3.Lerp(startScale, popScale, timer / popUpTime);
            timer += Time.deltaTime;
            yield return null;
        }

        timer = 0f;

        // Phase 2: Shrink to zero
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

        // 3. Actually destroy the object now that the animation is finished
        Destroy(gameObject);
    }
}