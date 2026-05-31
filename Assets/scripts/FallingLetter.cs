using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class FallingLetter : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private Key letterKey;
    [Header("Visuals")]
    [SerializeField] private TMP_Text letterText;
    [Header("Movement Settings")]
    [SerializeField] private float fallSpeed = 2f;

    // --- UPDATED: Split into specific zone tracking ---
    public bool inInnerZone { get; private set; } = false;
    public bool inOuterZone { get; private set; } = false;
    
    // Helper property so the rest of your code still easily knows if it's in ANY zone
    public bool inZone => inInnerZone || inOuterZone;
    
    public bool isPressed { get; private set; } = false;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    void Update()
    {
        // 1. Move downward
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

        if (Keyboard.current != null && letterKey != Key.None)
        {
            // 2. Check if the key is currently being physically held down right now
            bool isKeyCurrentlyHeld = Keyboard.current[letterKey].isPressed;

            // 3. The letter is only "successful" if the key is held AND it is in the zone
            isPressed = isKeyCurrentlyHeld && inZone;
        }

        // 4. Dynamically update the color every frame based on that real-time state
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

    // --- NEW: Helper method to check score value ---
    public int GetScoreValue()
    {
        if (inInnerZone) return 2;
        if (inOuterZone) return 1;
        return 0;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("TargetLineInner")) inInnerZone = true;
        else if (other.CompareTag("TargetLineOuter")) inOuterZone = true;
        else if (other.CompareTag("LineOnderGame")) Destroy(gameObject); 
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("TargetLineInner")) inInnerZone = false;
        else if (other.CompareTag("TargetLineOuter")) inOuterZone = false;

        // --- NEW: Reset state if it falls completely out of the hit zones ---
        if (!inZone)
        {
            // Unlock the press so the word generator knows this letter failed
            isPressed = false; 
            
            // Revert the color back to normal
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor; 
                
                // (Optional: You could change this to Color.red 
                // to give the player visual feedback that they missed it!)
            }
        }
    }
}