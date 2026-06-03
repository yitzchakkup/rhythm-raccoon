using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Required for scene management
using System.Collections;

public class StaminaManager : MonoBehaviour
{
    public static StaminaManager Instance { get; private set; }

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float startingStamina = 75f;
    private float currentStamina;
    
    [Header("Stamina Drain")]
    [SerializeField] private float staminaDrainAmount = 1f;
    [SerializeField] private float staminaDrainTickRate = 0.05f;

    private Image staminaBarFill;
    private Coroutine drainCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// When a new scene loads, this manager is responsible for finding its UI and re-initializing.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SceneUIRefs.Instance != null)
        {
            staminaBarFill = SceneUIRefs.Instance.staminaBarFill;
            Debug.Log("StaminaManager found its UI reference.");
        }
        else
        {
            Debug.LogError("StaminaManager could not find SceneUIRefs instance!");
        }
        
        // Now, re-initialize the state
        InitializeStamina();
    }

    private void InitializeStamina()
    {
        if (drainCoroutine != null)
        {
            StopCoroutine(drainCoroutine);
        }

        currentStamina = startingStamina;
        UpdateStaminaUI();
        
        drainCoroutine = StartCoroutine(DrainStamina());
        Debug.Log("StaminaManager has been initialized/reset.");
    }

    private IEnumerator DrainStamina()
    {
        while (true)
        {
            currentStamina -= staminaDrainAmount;
            UpdateStaminaUI();

            if (currentStamina <= 0)
            {
                currentStamina = 0;
                UpdateStaminaUI();
                
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.EndGame();
                }
                
                yield break;
            }

            yield return new WaitForSeconds(staminaDrainTickRate);
        }
    }

    public void AddStamina(float amount)
    {
        if (currentStamina <= 0) return;

        currentStamina = Mathf.Clamp(currentStamina + amount, 0, maxStamina);
        UpdateStaminaUI();
    }

    private void UpdateStaminaUI()
    {
        if (staminaBarFill != null)
        {
            staminaBarFill.fillAmount = currentStamina / maxStamina;
        }
    }
}