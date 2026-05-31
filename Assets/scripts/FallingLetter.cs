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

    public bool inZone { get; private set; } = false;
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
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

        if (Keyboard.current != null && letterKey != Key.None)
        {
            isPressed = Keyboard.current[letterKey].isPressed;
        }

        if (inZone && isPressed) spriteRenderer.color = Color.green;
        else spriteRenderer.color = originalColor; 
    }

    public void SetFallSpeed(float newSpeed) { fallSpeed = newSpeed; }

    // Replace your old SetupRandomLetter method with this one:
    public void SetupRandomLetter(Key newKey, string displayText = null)
    {
        letterKey = newKey;
        if (letterText != null) 
        {
            // If a custom display text (like a symbol) is provided, use it. 
            // Otherwise, just default to the key's name.
            letterText.text = string.IsNullOrEmpty(displayText) ? newKey.ToString() : displayText;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("TargetLineOuter") || other.CompareTag("TargetLineInner"))
        {
            inZone = true;
        }
        else if (other.CompareTag("LineOnderGame"))
        {
            Destroy(gameObject); // Destroys itself if it hits the bottom
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("TargetLineOuter") || other.CompareTag("TargetLineInner"))
        {
            inZone = false;
        }
    }
}