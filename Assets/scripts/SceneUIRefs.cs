using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SceneUIRefs : MonoBehaviour
{
    public static SceneUIRefs Instance { get; private set; }

    [Header("Inspector References")]
    [SerializeField] private Image inspectorStaminaFill;
    [SerializeField] private RectTransform inspectorStaminaHead;
    [SerializeField] private TMP_Text inspectorScoreText;
    [SerializeField] private GameObject inspectorGameOverUI;
    [SerializeField] private GameObject inspectorWinUI;
    [SerializeField] private GameObject inspectorLoseUI;
    // --- NEW: Tug of War UI Reference ---
    [SerializeField] private GameObject inspectorTugOfWarUI;

    // --- Static properties for easy access ---
    public static Image staminaFill { get; private set; }
    public static RectTransform staminaHead { get; private set; }
    public static TMP_Text scoreText { get; private set; }
    public static GameObject gameOverUI { get; private set; }
    public static GameObject winUI { get; private set; }
    public static GameObject loseUI { get; private set; }
    // --- NEW: Tug of War UI Static Property ---
    public static GameObject tugOfWarUI { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        // Map the inspector fields to the static properties
        staminaFill = inspectorStaminaFill;
        staminaHead = inspectorStaminaHead;
        scoreText = inspectorScoreText;
        gameOverUI = inspectorGameOverUI;
        winUI = inspectorWinUI;
        loseUI = inspectorLoseUI;
        tugOfWarUI = inspectorTugOfWarUI;
    }
}