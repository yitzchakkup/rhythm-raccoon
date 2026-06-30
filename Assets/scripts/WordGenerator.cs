using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class WordGenerator : MonoBehaviour
{
    [Header("Spawn Zone (Play Area)")]
    public BoxCollider2D spawnArea;

    [Header("Prefabs")]
    public GameObject[] spawnablePrefabs; 
    public GameObject connectionCordPrefab; 

    [Header("Difficulty: Limits")]
    public float timeToReachMaxDifficulty = 180f; 
    public float initialSpawnDelay = 4f;
    public float minimumSpawnDelay = 1.5f; 
    public float initialFallSpeed = 2f;
    public float maxFallSpeed = 7f;
    public int minLettersPerWave = 1;
    public int maxLettersLimit = 5;

    [Header("Layout & Scaling")]
    [Range(0f, 0.9f)] public float letterPadding = 0.2f;

    [Header("Difficulty: Clustering")]
    public float standardVerticalStagger = 1.5f;   
    [Range(0f, 1f)] public float minClusterProbability = 0.0f; 
    [Range(0f, 1f)] public float maxClusterProbability = 0.6f; 

    [Header("Difficulty: Trajectory Curves")]
    public AnimationCurve speedCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public AnimationCurve spawnDelayCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public AnimationCurve clusterCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public AnimationCurve letterCountCurve = AnimationCurve.Linear(0, 0, 1, 1); 

    [Header("Powerup States")]
    public float powerupSpeedMultiplier = 1f; 
    private float speedMultiplierTimer = 0f;

    private float currentSpawnDelay;
    private float currentFallSpeed;
    private float spawnTimer;
    private float gameTimer;

    private List<List<FallingLetter>> activeWaves = new List<List<FallingLetter>>();
    private List<int> previousWaveColumns = new List<int>(); // Tracks columns to prevent overlap
    
    void Start()
    {
        currentSpawnDelay = initialSpawnDelay;
        currentFallSpeed = initialFallSpeed;
    }

    public void StartGameLoop()
    {
        SpawnWave();
    }
    
    private void OnEnable()
    {
        gameTimer = 0f;
        spawnTimer = 0f;
        speedMultiplierTimer = 0f;
        powerupSpeedMultiplier = 1f;
        activeWaves.Clear(); 
        previousWaveColumns.Clear();
    }

    void Update()
    {
        if (spawnArea == null) return;
        
        if (speedMultiplierTimer > 0)
        {
            speedMultiplierTimer -= Time.deltaTime;
            if (speedMultiplierTimer <= 0) powerupSpeedMultiplier = 1f; 
        }

        gameTimer += Time.deltaTime;
        spawnTimer += Time.deltaTime;

        float progress = Mathf.Clamp01(gameTimer / timeToReachMaxDifficulty);
        float delayMultiplier = spawnDelayCurve.Evaluate(progress);
        float speedMultiplier = speedCurve.Evaluate(progress);

        float baseSpawnDelay = Mathf.Lerp(initialSpawnDelay, minimumSpawnDelay, delayMultiplier);
        float baseSpeed = Mathf.Lerp(initialFallSpeed, maxFallSpeed, speedMultiplier);

        currentFallSpeed = baseSpeed * powerupSpeedMultiplier;
        
        if (powerupSpeedMultiplier > 0)
        {
            currentSpawnDelay = baseSpawnDelay / powerupSpeedMultiplier;
        }

        foreach (List<FallingLetter> wave in activeWaves)
        {
            foreach (FallingLetter letter in wave)
            {
                if (letter != null) letter.SetFallSpeed(currentFallSpeed);
            }
        }

        if (spawnTimer >= currentSpawnDelay)
        {
            SpawnWave();
            spawnTimer = 0f;
        }

        CheckActiveWaves();
    }

    public void TriggerSpeedAttack(float multiplier, float duration)
    {
        powerupSpeedMultiplier = multiplier;
        speedMultiplierTimer = duration; 
    }

    private void CheckActiveWaves()
    {
        for (int i = activeWaves.Count - 1; i >= 0; i--)
        {
            List<FallingLetter> wave = activeWaves[i];
            bool missedLetter = false;
            foreach (FallingLetter letter in wave)
            {
                if (letter == null) missedLetter = true;
            }

            if (missedLetter)
            {
                activeWaves.RemoveAt(i);
                continue; 
            }

            bool waveComplete = true;
            foreach (FallingLetter letter in wave)
            {
                if (!letter.inZone || !letter.isPressed)
                {
                    waveComplete = false;
                    break;
                }
            }

            if (waveComplete)
            {
                if (ScoreAndStaminaManager.Instance != null)
                {
                    ScoreAndStaminaManager.Instance.AddScoreAndStamina(1);
                }

                foreach (FallingLetter letter in wave)
                {
                    if (letter.TryGetComponent<Powerup>(out Powerup powerup)) powerup.ApplyEffect();
                    letter.TriggerPopAndDestroy(); 
                }

                if (AvatarController.Instance != null && AvatarController.Instance.localAnimator != null)
                {
                    AvatarController.Instance.localAnimator.TriggerRandomPose();
                }

                activeWaves.RemoveAt(i);
            }
        }
    }

    private void FinalizeWaveGroup(List<FallingLetter> group)
    {
        if (group.Count == 0) return;
        activeWaves.Add(group);

        if (group.Count > 1 && connectionCordPrefab != null)
        {
            GameObject cordObj = Instantiate(connectionCordPrefab, Vector3.zero, Quaternion.identity);
            LetterConnectionCord cordScript = cordObj.GetComponent<LetterConnectionCord>();
            if (cordScript != null) cordScript.Setup(group);
        }
    }

    private void SpawnWave()
    {
        float progress = Mathf.Clamp01(gameTimer / timeToReachMaxDifficulty);
        float letterCountMultiplier = letterCountCurve.Evaluate(progress);
        int lettersToSpawn = Mathf.RoundToInt(Mathf.Lerp(minLettersPerWave, maxLettersLimit, letterCountMultiplier));
        
        float clusterMultiplier = clusterCurve.Evaluate(progress);
        float currentClusterChance = Mathf.Lerp(minClusterProbability, maxClusterProbability, clusterMultiplier);

        float leftEdge = spawnArea.bounds.min.x;
        float rightEdge = spawnArea.bounds.max.x;
        float spawnY = spawnArea.bounds.max.y; 

        float zoneWidth = rightEdge - leftEdge;
        float columnWidth = zoneWidth / maxLettersLimit;
        float targetLetterWidth = columnWidth * (1f - letterPadding);

        // --- ANTI-OVERLAP RULE 1: Prevent spawning directly on top of the last wave ---
        List<int> availableColumns = new List<int>();
        for (int i = 0; i < maxLettersLimit; i++) availableColumns.Add(i);

        List<int> safeColumns = new List<int>(availableColumns);
        foreach (int lastCol in previousWaveColumns)
        {
            // Only remove if we still have enough columns to fulfill the spawn request
            if (safeColumns.Count > lettersToSpawn)
            {
                safeColumns.Remove(lastCol);
            }
        }

        // Pick columns randomly from the safe pool
        List<int> chosenColumns = new List<int>();
        for (int i = 0; i < lettersToSpawn; i++)
        {
            int randomIndex = Random.Range(0, safeColumns.Count);
            chosenColumns.Add(safeColumns[randomIndex]);
            safeColumns.RemoveAt(randomIndex);
        }

        previousWaveColumns = new List<int>(chosenColumns); // Save for the next wave

        Debug.Log($"<color=cyan>[WordGenerator]</color> Wave Spawned: {lettersToSpawn} letters (Cols: {string.Join(", ", chosenColumns)})");

        List<Key> availableKeys = new List<Key>();
        for (int k = (int)Key.A; k <= (int)Key.Z; k++) availableKeys.Add((Key)k);

        List<FallingLetter> currentWorkingGroup = new List<FallingLetter>();
        float currentY = spawnY;
        int lastSpawnedColumn = -2; // Dummy value so the first check always passes

        for (int i = 0; i < lettersToSpawn; i++)
        {
            int currentColumn = chosenColumns[i];
            GameObject prefab = spawnablePrefabs[Random.Range(0, spawnablePrefabs.Length)];
            
            if (i > 0) 
            {
                // --- ANTI-OVERLAP RULE 2: Prevent side-by-side letters in the same cluster ---
                bool isAdjacentColumn = Mathf.Abs(currentColumn - lastSpawnedColumn) <= 1;
                bool forceStagger = isAdjacentColumn;

                if (forceStagger || Random.value >= currentClusterChance)
                {
                    if (forceStagger) 
                    {
                        Debug.Log($"<color=yellow>[WordGenerator]</color> Forced vertical stagger! Prevented columns {lastSpawnedColumn} and {currentColumn} from touching side-by-side.");
                    }

                    currentY += standardVerticalStagger;
                    FinalizeWaveGroup(currentWorkingGroup);
                    currentWorkingGroup = new List<FallingLetter>();
                }
            }

            float xPos = leftEdge + (columnWidth * 0.5f) + (columnWidth * currentColumn);
            Vector3 position = new Vector3(xPos, currentY, 0f);

            GameObject spawnedObj = Instantiate(prefab, position, Quaternion.identity);

            Renderer objRenderer = spawnedObj.GetComponentInChildren<Renderer>();
            if (objRenderer != null)
            {
                float currentWidth = objRenderer.bounds.size.x;
                if (currentWidth > 0)
                {
                    float scaleFactor = targetLetterWidth / currentWidth;
                    spawnedObj.transform.localScale *= scaleFactor;
                }
            }

            FallingLetter letterScript = spawnedObj.GetComponent<FallingLetter>();
            if (letterScript != null)
            {
                letterScript.SetFallSpeed(currentFallSpeed);
                int randomKeyIndex = Random.Range(0, availableKeys.Count);
                Key assignedKey = availableKeys[randomKeyIndex];
                availableKeys.RemoveAt(randomKeyIndex); 

                letterScript.SetupRandomLetter(assignedKey);
                currentWorkingGroup.Add(letterScript);
            }

            lastSpawnedColumn = currentColumn;
        }

        if (currentWorkingGroup.Count > 0) FinalizeWaveGroup(currentWorkingGroup);
    }
}