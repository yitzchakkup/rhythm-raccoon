using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events; // --- NEW: Required for custom button events ---
using System.Collections;

public class HangingSignButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Idle Settings (The Sign)")]
    public float swingAngle = 4f;
    public float swingSpeed = 2f;

    [Header("Hover Settings")]
    public float hoverScaleMultiplier = 1.1f;
    public float maxTiltAngle = 2f;
    public float animationSpeed = 15f;

    [Header("Click Settings")]
    public float clickScaleMultiplier = 0.95f;
    public float flipDuration = 0.4f;

    [Header("Audio Settings")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    // --- NEW: The event we will trigger after the flip ---
    [Header("Events")]
    public UnityEvent onClick;

    // Internal state tracking
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Vector3 targetScale;
    private Quaternion targetRotation;
    
    private bool isPressed = false;
    private bool isHovered = false;
    private bool isFlipping = false;
    private float randomTimeOffset;

    private void Awake()
    {
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;
        
        targetScale = originalScale;
        targetRotation = originalRotation;

        randomTimeOffset = Random.Range(0f, 100f);
    }

    private void OnDisable()
    {
        isFlipping = false;
        isHovered = false;
        transform.localScale = originalScale;
        transform.localRotation = originalRotation;
        targetScale = originalScale;
        targetRotation = originalRotation;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);

        if (isFlipping) return;

        if (!isHovered)
        {
            float currentAngle = Mathf.Sin((Time.time + randomTimeOffset) * swingSpeed) * swingAngle;
            targetRotation = originalRotation * Quaternion.Euler(0, 0, currentAngle);
        }

        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * animationSpeed);
    }

    // --- MOUSE INTERACTIONS ---

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetScale = originalScale * hoverScaleMultiplier;
        
        float randomZ = Random.Range(-maxTiltAngle, maxTiltAngle);
        targetRotation = originalRotation * Quaternion.Euler(0, 0, randomZ);

        // THE FIX: Only play the hover sound if we aren't actively clicking or flipping
        if (!isPressed && !isFlipping)
        {
            PlaySound(hoverSound);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // THE FIX: Do not reset the button if it only "exited" because it squished away from the mouse
        if (!isPressed)
        {
            isHovered = false;
            targetScale = originalScale;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (isFlipping || !gameObject.activeInHierarchy) return;

        // Lock the button into a pressed state
        isPressed = true; 

        targetScale = originalScale * clickScaleMultiplier;
        PlaySound(clickSound);
        
        StartCoroutine(FlipRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Release the pressed state
        isPressed = false; 

        if (isHovered)
        {
            targetScale = originalScale * hoverScaleMultiplier;
        }
    }
    private IEnumerator FlipRoutine()
    {
        isFlipping = true;
        float elapsed = 0f;

        // Grab the exact angles the sign is currently resting at
        Vector3 startAngles = transform.localRotation.eulerAngles;

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flipDuration;
            
            // Smooth ease in/out math for a snappy physical flip
            float ease = t * t * (3f - 2f * t); 
            
            // Manually calculate the 0 to 360 spin
            float spinAmount = Mathf.Lerp(0f, 360f, ease);
            
            // Apply the spin to the Y axis, while keeping the current X and Z tilts!
            transform.localRotation = Quaternion.Euler(startAngles.x, startAngles.y + spinAmount, startAngles.z);
            
            yield return null;
        }

        // Lock it perfectly back to normal, then return control to the Update loop
        transform.localRotation = originalRotation;
        isFlipping = false;

        // Trigger the screen transition NOW, after the animation is 100% done!
        onClick?.Invoke();
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip, true);
        }
    }
}