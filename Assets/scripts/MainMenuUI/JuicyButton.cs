using UnityEngine;
using UnityEngine.EventSystems;

public class JuicyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Animation Settings")]
    [Tooltip("How big it gets when hovered")]
    public float hoverScaleMultiplier = 1.1f;
    [Tooltip("How small it gets when clicked")]
    public float clickScaleMultiplier = 0.95f;
    [Tooltip("How much it randomly tilts on hover")]
    public float maxTiltAngle = 2f;
    [Tooltip("How fast it snaps to the new size/rotation")]
    public float animationSpeed = 15f;

    [Header("Audio Settings")]
    public AudioSource uiAudioSource;
    public AudioClip hoverSound;
    public AudioClip clickSound;
    [Tooltip("Randomizes pitch between 1-pitchVariance and 1+pitchVariance")]
    public float pitchVariance = 0.1f;

    // Internal state tracking
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Vector3 targetScale;
    private Quaternion targetRotation;

    private void Awake()
    {
        // Store the starting size and rotation so we always return to normal
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;
        
        targetScale = originalScale;
        targetRotation = originalRotation;
    }

    private void Update()
    {
        // Smoothly animate towards the target scale and rotation every frame
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * animationSpeed);
    }

    // --- MOUSE INTERACTIONS ---

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Mouse hovers over: Scale up and tilt slightly
        targetScale = originalScale * hoverScaleMultiplier;
        
        float randomZ = Random.Range(-maxTiltAngle, maxTiltAngle);
        targetRotation = originalRotation * Quaternion.Euler(0, 0, randomZ);

        PlaySound(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Mouse leaves: Return to normal
        targetScale = originalScale;
        targetRotation = originalRotation;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Mouse clicks down: Squish inwards
        targetScale = originalScale * clickScaleMultiplier;
        
        PlaySound(clickSound);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Mouse releases: Pop back to hover size
        targetScale = originalScale * hoverScaleMultiplier;
    }

    // --- AUDIO LOGIC ---

    private void PlaySound(AudioClip clip)
    {
        if (uiAudioSource != null && clip != null)
        {
            // Randomize the pitch slightly for that piano-key feel
            uiAudioSource.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            uiAudioSource.PlayOneShot(clip);
        }
    }
}