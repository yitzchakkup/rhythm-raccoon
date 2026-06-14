using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class WordGenerator : MonoBehaviour
{
    [Header("Spawn Zone (Anchor Points)")]
    public Transform leftSpawnBound;
    public Transform rightSpawnBound;

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

    [Header("Difficulty: Clustering")]
    public float standardVerticalStagger = 1.5f;   
    [Range(0f, 1f)] public float minClusterProbability = 0.0f; 
    [Range(0f, 1f)] public float maxClusterProbability = 0.6f; 

    [Header("Difficulty: Trajectory Curves (0.0 to 1.0)")]
    public AnimationCurve speedCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public AnimationCurve spawnDelayCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public AnimationCurve clusterCurve = AnimationCurve.Linear(0, 0, 1, 1);

    // --- NEW: The Master Speed Multiplier for Powerups ---
    [Header("Powerup States")]
    public float powerupSpeedMultiplier = 1f; 

    private float currentSpawnDelay;
    private float currentFallSpeed;
    private float spawnTimer;
    private float gameTimer;

    private List<List<FallingLetter>> activeWaves = new List<List<FallingLetter>>();

    void Start()
    {
        currentSpawnDelay = initialSpawnDelay;
        currentFallSpeed = initialFallSpeed;
        SpawnWave();
    }

    void Update()
    {
        if (leftSpawnBound == null || rightSpawnBound == null) return;

        gameTimer += Time.deltaTime;
        spawnTimer += Time.deltaTime;

        float progress = Mathf.Clamp01(gameTimer / timeToReachMaxDifficulty);

        float delayMultiplier = spawnDelayCurve.Evaluate(progress);
        float speedMultiplier = speedCurve.Evaluate(progress);

        currentSpawnDelay = Mathf.Lerp(initialSpawnDelay, minimumSpawnDelay, delayMultiplier);
        
        // --- THE FIX: Calculate base speed, then warp it with the powerup ---
        float baseSpeed = Mathf.Lerp(initialFallSpeed, maxFallSpeed, speedMultiplier);
        currentFallSpeed = baseSpeed * powerupSpeedMultiplier;

        // --- THE FIX: Actively push the new speed to every living letter ---
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

        if (spawnTimer >= currentSpawnDelay)
        {
            SpawnWave();
            spawnTimer = 0f;
        }

        CheckActiveWaves();
    }

    // --- NEW: The Powerup Coroutine ---
    public void TriggerSpeedAttack(float multiplier, float duration)
    {
        StartCoroutine(SpeedWarpRoutine(multiplier, duration));
    }

    private IEnumerator SpeedWarpRoutine(float multiplier, float duration)
    {
        powerupSpeedMultiplier = multiplier; 
        yield return new WaitForSeconds(duration);
        powerupSpeedMultiplier = 1f; 
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
                    int totalWaveScore = 0;
                    foreach (FallingLetter letter in wave)
                    {
                        totalWaveScore += letter.GetScoreValue();
                    }
                    ScoreAndStaminaManager.Instance.AddScoreAndStamina(totalWaveScore);
                }

                foreach (FallingLetter letter in wave)
                {
                    if (letter.TryGetComponent<Powerup>(out Powerup powerup))
                    {
                        powerup.ApplyEffect();
                    }
                    letter.TriggerPopAndDestroy();
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
        int lettersToSpawn = Mathf.RoundToInt(Mathf.Lerp(minLettersPerWave, maxLettersLimit, progress));
        
        float clusterMultiplier = clusterCurve.Evaluate(progress);
        float currentClusterChance = Mathf.Lerp(minClusterProbability, maxClusterProbability, clusterMultiplier);

        float leftEdge = leftSpawnBound.position.x;
        float rightEdge = rightSpawnBound.position.x;
        float spacing = (rightEdge - leftEdge) / (lettersToSpawn + 1);
        float spawnY = leftSpawnBound.position.y; 

        List<Key> availableKeys = new List<Key>();
        for (int k = (int)Key.A; k <= (int)Key.Z; k++)
        {
            availableKeys.Add((Key)k);
        }

        List<float> xPositions = new List<float>();
        for (int i = 0; i < lettersToSpawn; i++)
        {
            xPositions.Add(leftEdge + (spacing * (i + 1)));
        }

        for (int i = 0; i < xPositions.Count; i++)
        {
            float temp = xPositions[i];
            int randomIndex = Random.Range(i, xPositions.Count);
            xPositions[i] = xPositions[randomIndex];
            xPositions[randomIndex] = temp;
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

            FallingLetter letterScript = spawnedObj.GetComponent<FallingLetter>();
            if (letterScript != null)
            {
                // Assign the warped speed right at birth
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