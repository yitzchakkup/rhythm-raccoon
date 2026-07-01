using UnityEngine;
using UnityEngine.UI; 

public class HeartbeatCamera : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Camera mainCamera;

    [Header("Vignette UI")]
    [SerializeField] private Image vignetteOverlay; 
    [Tooltip("Keep this low (e.g., 0.15) so the red effect is subtle and not blinding.")]
    [Range(0f, 1f)] [SerializeField] private float maxVignetteAlpha = 0.15f; 

    [Header("Heartbeat Settings")]
    [SerializeField] private float zoomIntensity = 0.8f; 
    [Tooltip("The curve X-axis now represents the exact length of your audio clip (0.0 to 1.0).")]
    [SerializeField] private AnimationCurve heartbeatCurve;

    [Header("Danger Thresholds")]
    [SerializeField] private int multiplayerDangerDeficit = 15; 
    [SerializeField] private float singlePlayerDangerPct = 0.3f;
    
    [Header("Heartbeat Audio")]
    [Tooltip("Attach an AudioSource to this camera and drag it here.")]
    [SerializeField] private AudioSource heartbeatAudioSource;

    private float defaultOrthoSize;
    private bool isHeartbeatActive = false;
    private bool gameHasEnded = false; 

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        
        if (mainCamera != null)
        {
            defaultOrthoSize = mainCamera.orthographicSize;
        }

        // Failsafe default curve if you haven't drawn one in the inspector yet
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

        // Ensure the vignette starts completely invisible
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
            // 1. Manage the Audio Playback
            if (heartbeatAudioSource != null && !heartbeatAudioSource.isPlaying)
            {
                heartbeatAudioSource.Play();
            }

            // 2. Get the exact playback percentage of the audio file (0.0 to 1.0)
            float normalizedAudioTime = 0f;
            if (heartbeatAudioSource != null && heartbeatAudioSource.clip != null)
            {
                // Prevent division by zero just in case
                if (heartbeatAudioSource.clip.length > 0)
                {
                    normalizedAudioTime = heartbeatAudioSource.time / heartbeatAudioSource.clip.length;
                }
            }

            // 3. Feed the audio's exact progress into your visual curve
            float curveValue = heartbeatCurve.Evaluate(normalizedAudioTime);
            
            // 4. Sync the Camera Zoom
            mainCamera.orthographicSize = defaultOrthoSize - (curveValue * zoomIntensity);

            // 5. Sync the Red Vignette Fade
            if (vignetteOverlay != null)
            {
                Color c = vignetteOverlay.color;
                c.a = curveValue * maxVignetteAlpha; 
                vignetteOverlay.color = c;
            }
        }
        else
        {
            // Stop the audio if we are safe
            if (heartbeatAudioSource != null && heartbeatAudioSource.isPlaying)
            {
                heartbeatAudioSource.Stop();
            }

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
        // Instantly abort if the game is over
        if (gameHasEnded) return;

        bool inDanger = false;

        // Check Multiplayer Deficit
        if (MultiplayerMatchManager.Instance != null && MultiplayerMatchManager.Instance.IsMultiplayerGame())
        {
            float myScore = MultiplayerMatchManager.Instance.GetMyScore();
            float oppScore = MultiplayerMatchManager.Instance.GetOpponentScore();
            
            if ((myScore - oppScore) <= -multiplayerDangerDeficit)
            {
                inDanger = true;
            }
        }
        // Check Singleplayer Stamina
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

    // Call this from GameManager exactly when the match ends!
    public void StopHeartbeatForEndScreen()
    {
        gameHasEnded = true;
        isHeartbeatActive = false; 
    }
}