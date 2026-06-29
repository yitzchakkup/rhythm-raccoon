using UnityEngine;

public class JuicyLogo : MonoBehaviour
{
    [Header("Float Settings")]
    [Tooltip("How fast it bobs up and down")]
    public float floatSpeed = 2f;
    [Tooltip("How many pixels it moves up and down")]
    public float floatAmount = 10f; 

    [Header("Breathe Settings (Organic)")]
    [Tooltip("How fast it randomly expands and contracts")]
    public float breatheSpeed = 0.8f; 
    [Tooltip("How much it grows/shrinks")]
    public float scaleAmount = 0.05f; 

    private RectTransform rectTransform;
    private Vector2 startAnchoredPos;
    private Vector3 startScale;
    
    // We use this to ensure the random breathing starts at a different point every time you play
    private float noiseSeed; 

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        startAnchoredPos = rectTransform.anchoredPosition;
        startScale = rectTransform.localScale;

        noiseSeed = Random.Range(0f, 1000f);
    }

    private void Update()
    {
        // 1. The Float (Y-Axis movement via Sine Wave - kept rhythmic so it doesn't look glitchy)
        float newY = startAnchoredPos.y + (Mathf.Sin(Time.time * floatSpeed) * floatAmount);
        rectTransform.anchoredPosition = new Vector2(startAnchoredPos.x, newY);

        // 2. The Organic Breathe (Scaling via Perlin Noise)
        // PerlinNoise returns a value between 0 and 1. We multiply by 2 and subtract 1 to make it swing between -1 and 1.
        float noiseValue = Mathf.PerlinNoise(Time.time * breatheSpeed, noiseSeed);
        float organicOffset = (noiseValue * 2f - 1f) * scaleAmount;

        rectTransform.localScale = startScale + new Vector3(organicOffset, organicOffset, 0f);
    }
}