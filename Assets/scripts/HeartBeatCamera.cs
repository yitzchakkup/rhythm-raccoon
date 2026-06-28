using UnityEngine;
using UnityEngine.UI; // --- NEW: Required for the Image component ---

public class HeartbeatCamera : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Camera mainCamera;

    [Header("Vignette UI")]
    [SerializeField] private Image vignetteOverlay; // Drag your DangerVignette here
    [Tooltip("Keep this low (e.g., 0.15) so the red effect is subtle and not blinding.")]
    [Range(0f, 1f)] [SerializeField] private float maxVignetteAlpha = 0.15f; 

    [Header("Heartbeat Settings")]
    [SerializeField] private float zoomIntensity = 0.8f; 
    [SerializeField] private float heartbeatSpeed = 1.2f;
    [SerializeField] private AnimationCurve heartbeatCurve;

    [Header("Danger Thresholds")]
    [SerializeField] private int multiplayerDangerDeficit = 15; 
    [SerializeField] private float singlePlayerDangerPct = 0.3f;
    
    [Header("Heartbeat Audio")]
    [SerializeField] private AudioClip heartbeatSfx;
    private float lastPlayedTime = 0f;

    private float defaultOrthoSize;
    private bool isHeartbeatActive = false;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        
        if (mainCamera != null)
        {
            defaultOrthoSize = mainCamera.orthographicSize;
        }

        if (heartbeatCurve == null || heartbeatCurve.keys.Length == 0)
        {
            heartbeatCurve = new AnimationCurve(
                new Keyframe(0f, 0f),    
                new Keyframe(0.15f, 1f), 
                new Keyframe(0.3f, 0f),  
                new Keyframe(0.45f, 0.6f),
                new Keyframe(0.6f, 0f),  
                new Keyframe(1f, 0f)     
            );
        }

        // Ensure the vignette starts invisible
        if (vignetteOverlay != null)
        {
            Color c = vignetteOverlay.color;
            c.a = 0f;
            vignetteOverlay.color = c;
        }
    }

    void Update()
    {
        if (mainCamera == null) return;

        CheckDangerState();

        if (isHeartbeatActive)
        {
            float curveTime = (Time.time * heartbeatSpeed) % 1f;
            float curveValue = heartbeatCurve.Evaluate(curveTime);
            
            if (curveTime < 0.1f && (Time.time - lastPlayedTime) > 0.5f)
            {
                if (AudioManager.Instance != null && heartbeatSfx != null)
                {
                    AudioManager.Instance.PlaySFX(heartbeatSfx, false);
                    lastPlayedTime = Time.time;
                }
            }
            
            // 1. Sync the Camera Zoom
            mainCamera.orthographicSize = defaultOrthoSize - (curveValue * zoomIntensity);

            // 2. Sync the Red Vignette Fade
            if (vignetteOverlay != null)
            {
                Color c = vignetteOverlay.color;
                // Multiplies the curve (0 to 1) by your max alpha limit (e.g., 0.15)
                c.a = curveValue * maxVignetteAlpha; 
                vignetteOverlay.color = c;
            }
        }
        else
        {
            // Smoothly ease the camera back out
            mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, defaultOrthoSize, Time.deltaTime * 5f);

            // Smoothly fade the red vignette away
            if (vignetteOverlay != null)
            {
                Color c = vignetteOverlay.color;
                c.a = Mathf.Lerp(c.a, 0f, Time.deltaTime * 5f);
                vignetteOverlay.color = c;
            }
        }
    }

    private void CheckDangerState()
    {
        bool inDanger = false;

        if (MultiplayerMatchManager.Instance != null && MultiplayerMatchManager.Instance.IsMultiplayerGame())
        {
            float myScore = MultiplayerMatchManager.Instance.GetMyScore();
            float oppScore = MultiplayerMatchManager.Instance.GetOpponentScore();
            
            if ((myScore - oppScore) <= -multiplayerDangerDeficit)
            {
                inDanger = true;
            }
        }
        else if (ScoreAndStaminaManager.Instance != null)
        {
            float current = ScoreAndStaminaManager.Instance.GetCurrentStamina();
            float max = ScoreAndStaminaManager.Instance.GetMaxStamina();
            
            if ((current / max) <= singlePlayerDangerPct)
            {
                inDanger = true;
            }
        }

        isHeartbeatActive = inDanger;
    }
}