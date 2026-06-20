using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SceneUIRefs : MonoBehaviour
{
    public static SceneUIRefs Instance { get; private set; }

    [Header("Inspector References")]
    [SerializeField] private GameObject inspectorStaminaBar; // Parent object for the whole stamina UI
    [SerializeField] private Image inspectorStaminaFill;
    [SerializeField] private RectTransform inspectorStaminaHead;
    [SerializeField] private TMP_Text inspectorScoreText;
    [SerializeField] private GameObject inspectorGameOverUI;
    [SerializeField] private GameObject inspectorWinUI;
    [SerializeField] private GameObject inspectorLoseUI;
    [SerializeField] private GameObject inspectorTugOfWarUI;
    [SerializeField] private Sprite inspectorLowStaminaFillSprite;
    [SerializeField] private Sprite inspectorSadHeadSprite;

    // --- Static properties for easy access ---
    public static GameObject staminaBar { get; private set; }
    public static Image staminaFill { get; private set; }
    public static RectTransform staminaHead { get; private set; }
    public static TMP_Text scoreText { get; private set; }
    public static GameObject gameOverUI { get; private set; }
    public static GameObject winUI { get; private set; }
    public static GameObject loseUI { get; private set; }
    public static GameObject tugOfWarUI { get; private set; }
    public static Sprite lowStaminaFillSprite { get; private set; }
    public static Sprite sadHeadSprite { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        // Map the inspector fields to the static properties
        staminaBar = inspectorStaminaBar;
        staminaFill = inspectorStaminaFill;
        staminaHead = inspectorStaminaHead;
        scoreText = inspectorScoreText;
        gameOverUI = inspectorGameOverUI;
        winUI = inspectorWinUI;
        loseUI = inspectorLoseUI;
        tugOfWarUI = inspectorTugOfWarUI;
        lowStaminaFillSprite = inspectorLowStaminaFillSprite;
        sadHeadSprite = inspectorSadHeadSprite;
    }
}