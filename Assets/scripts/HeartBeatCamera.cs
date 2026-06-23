using UnityEngine;

public class HeartbeatCamera : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Camera mainCamera;

    [Header("Heartbeat Settings")]
    [Tooltip("How aggressively the camera zooms in during a thump.")]
    [SerializeField] private float zoomIntensity = 0.8f; 
    
    [Tooltip("How fast the heartbeat plays.")]
    [SerializeField] private float heartbeatSpeed = 1.2f;

    [Tooltip("Draw your thump-thump rhythm here!")]
    [SerializeField] private AnimationCurve heartbeatCurve;

    [Header("Danger Thresholds")]
    [Tooltip("How many points behind before the heartbeat starts in Multiplayer.")]
    [SerializeField] private int multiplayerDangerDeficit = 15; 
    [Tooltip("At what stamina percentage the heartbeat starts in Single Player.")]
    [SerializeField] private float singlePlayerDangerPct = 0.3f;

    private float defaultOrthoSize;
    private bool isHeartbeatActive = false;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        
        if (mainCamera != null)
        {
            defaultOrthoSize = mainCamera.orthographicSize;
        }

        // Failsafe: If you forget to draw a curve in the Inspector, this builds a default "Double Thump"
        if (heartbeatCurve == null || heartbeatCurve.keys.Length == 0)
        {
            heartbeatCurve = new AnimationCurve(
                new Keyframe(0f, 0f),    // Start flat
                new Keyframe(0.15f, 1f), // First thump (strong)
                new Keyframe(0.3f, 0f),  // Back down
                new Keyframe(0.45f, 0.6f),// Second thump (weaker)
                new Keyframe(0.6f, 0f),  // Back down
                new Keyframe(1f, 0f)     // Pause until next beat
            );
        }
    }

    void Update()
    {
        if (mainCamera == null) return;

        CheckDangerState();

        if (isHeartbeatActive)
        {
            // The modulo (%) operator loops the time from 0.0 to 1.0 continuously
            float curveTime = (Time.time * heartbeatSpeed) % 1f;
            float curveValue = heartbeatCurve.Evaluate(curveTime);
            
            // Subtracting size zooms an orthographic camera IN
            mainCamera.orthographicSize = defaultOrthoSize - (curveValue * zoomIntensity);
        }
        else
        {
            // Smoothly ease the camera back to normal when they escape danger
            mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, defaultOrthoSize, Time.deltaTime * 5f);
        }
    }

    private void CheckDangerState()
    {
        bool inDanger = false;

        // 1. Multiplayer Check (Am I losing badly?)
        if (MultiplayerMatchManager.Instance != null && MultiplayerMatchManager.Instance.IsMultiplayerGame())
        {
            int myScore = MultiplayerMatchManager.Instance.GetMyScore();
            int oppScore = MultiplayerMatchManager.Instance.GetOpponentScore();
            
            if ((myScore - oppScore) <= -multiplayerDangerDeficit)
            {
                inDanger = true;
            }
        }
        // 2. Single Player Check (Is my stamina critically low?)
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