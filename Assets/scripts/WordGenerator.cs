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
    
    [Tooltip("Controls how fast the game ramps up from minLetters to maxLetters")]
    public AnimationCurve letterCountCurve = AnimationCurve.Linear(0, 0, 1, 1); // <-- NEW VARIABLE

    [Header("Powerup States")]
    public float powerupSpeedMultiplier = 1f; 
    private float speedMultiplierTimer = 0f;

    private float currentSpawnDelay;
    private float currentFallSpeed;
    private float spawnTimer;
    private float gameTimer;

    private List<List<FallingLetter>> activeWaves = new List<List<FallingLetter>>();

    // Inside WordGenerator.cs
    
    void Start()
    {
        currentSpawnDelay = initialSpawnDelay;
        currentFallSpeed = initialFallSpeed;
    }

    public void StartGameLoop()
    {
        SpawnWave();
    }

    void Update()
    {
        if (spawnArea == null) return;

        // 1. Check Powerup Timers
        if (speedMultiplierTimer > 0)
        {
            speedMultiplierTimer -= Time.deltaTime;
            if (speedMultiplierTimer <= 0) 
            {
                powerupSpeedMultiplier = 1f; // Revert to normal speed
            }
        }

        gameTimer += Time.deltaTime;
        spawnTimer += Time.deltaTime;

        // 2. Calculate Base Difficulty
        float progress = Mathf.Clamp01(gameTimer / timeToReachMaxDifficulty);
        float delayMultiplier = spawnDelayCurve.Evaluate(progress);
        float speedMultiplier = speedCurve.Evaluate(progress);

        float baseSpawnDelay = Mathf.Lerp(initialSpawnDelay, minimumSpawnDelay, delayMultiplier);
        float baseSpeed = Mathf.Lerp(initialFallSpeed, maxFallSpeed, speedMultiplier);

        // 3. Apply Powerup Multiplier
        currentFallSpeed = baseSpeed * powerupSpeedMultiplier;
        
        if (powerupSpeedMultiplier > 0)
        {
            currentSpawnDelay = baseSpawnDelay / powerupSpeedMultiplier;
        }

        // 4. Apply speed to already falling letters
        foreach (List<FallingLetter> wave in activeWaves)
        {
            foreach (FallingLetter letter in wave)
            {
                if (letter != null)
                {
                    letter.SetFallSpeed(currentFallSpeed);
                }
            }
        }

        // 5. Check if it is time to spawn
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
                    if (letter.TryGetComponent<Powerup>(out Powerup powerup))
                    {
                        powerup.ApplyEffect();
                    }
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
            if (cordScript != null)
            {
                cordScript.Setup(group);
            }
        }
    }

    private void SpawnWave()
    {
        float progress = Mathf.Clamp01(gameTimer / timeToReachMaxDifficulty);
        
        // --- THE FIX: Evaluate the new curve to get the multiplier before Lerping ---
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

        List<float> availableColumns = new List<float>();
        for (int i = 0; i < maxLettersLimit; i++)
        {
            availableColumns.Add(leftEdge + (columnWidth * 0.5f) + (columnWidth * i));
        }

        for (int i = 0; i < availableColumns.Count; i++)
        {
            float temp = availableColumns[i];
            int randomIndex = Random.Range(i, availableColumns.Count);
            availableColumns[i] = availableColumns[randomIndex];
            availableColumns[randomIndex] = temp;
        }

        List<float> xPositions = new List<float>();
        for (int i = 0; i < lettersToSpawn; i++)
        {
            xPositions.Add(availableColumns[i]);
        }

        List<Key> availableKeys = new List<Key>();
        for (int k = (int)Key.A; k <= (int)Key.Z; k++)
        {
            availableKeys.Add((Key)k);
        }

        List<FallingLetter> currentWorkingGroup = new List<FallingLetter>();
        float currentY = spawnY;

        for (int i = 0; i < lettersToSpawn; i++)
        {
            GameObject prefab = spawnablePrefabs[Random.Range(0, spawnablePrefabs.Length)];
            
            if (i > 0) 
            {
                if (Random.value < currentClusterChance)
                {
                    // Cluster
                }
                else
                {
                    currentY += standardVerticalStagger;
                    FinalizeWaveGroup(currentWorkingGroup);
                    currentWorkingGroup = new List<FallingLetter>();
                }
            }

            Vector3 position = new Vector3(xPositions[i], currentY, 0f);
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
        }

        if (currentWorkingGroup.Count > 0)
        {
            FinalizeWaveGroup(currentWorkingGroup);
        }
    }
}