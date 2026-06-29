using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
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

    [Header("Events")]
    public UnityEvent onClick;

    // Internal state tracking
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Vector3 targetScale;
    private Quaternion targetRotation;
    
    private bool isHovered = false;
    private bool isFlipping = false;
    private bool isPressed = false;
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
        isPressed = false;
        transform.localScale = originalScale;
        transform.localRotation = originalRotation;
        targetScale = originalScale;
        targetRotation = originalRotation;
    }

    private void Update()
    {
        // --- CHANGED: Now using Time.unscaledDeltaTime to ignore time pauses ---
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);

        if (isFlipping) return;

        if (!isHovered)
        {
            // --- CHANGED: Now using Time.unscaledTime so it swings while paused ---
            float currentAngle = Mathf.Sin((Time.unscaledTime + randomTimeOffset) * swingSpeed) * swingAngle;
            targetRotation = originalRotation * Quaternion.Euler(0, 0, currentAngle);
        }

        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.unscaledDeltaTime * animationSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetScale = originalScale * hoverScaleMultiplier;
        
        float randomZ = Random.Range(-maxTiltAngle, maxTiltAngle);
        targetRotation = originalRotation * Quaternion.Euler(0, 0, randomZ);

        if (!isPressed && !isFlipping)
        {
            PlaySound(hoverSound);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
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

        isPressed = true;
        targetScale = originalScale * clickScaleMultiplier;
        PlaySound(clickSound);
        
        StartCoroutine(FlipRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
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

        Vector3 startAngles = transform.localRotation.eulerAngles;

        while (elapsed < flipDuration)
        {
            // --- CHANGED: Now using unscaledDeltaTime for the animation loop ---
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / flipDuration;
            
            float ease = t * t * (3f - 2f * t); 
            float spinAmount = Mathf.Lerp(0f, 360f, ease);
            
            transform.localRotation = Quaternion.Euler(startAngles.x, startAngles.y + spinAmount, startAngles.z);
            
            yield return null;
        }

        transform.localRotation = originalRotation;
        isFlipping = false;

        onClick?.Invoke();
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
        {
            // Note: If your AudioManager uses pitched sounds tied to time, 
            // you might need to ensure its AudioSources ignore timeScale too!
            AudioManager.Instance.PlaySFX(clip, true);
        }
    }
}