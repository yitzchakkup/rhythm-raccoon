using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WordGenerator : MonoBehaviour
{
    [Header("Spawn Zone (Anchor Points)")]
    public Transform leftSpawnBound;
    public Transform rightSpawnBound;

    [Header("Prefabs")]
    public GameObject[] spawnablePrefabs; 
    public GameObject connectionCordPrefab; 

    [Header("Difficulty: Limits")]
    public float timeToReachMaxDifficulty = 180f; // E.g., 3 minutes to reach max
    
    public float initialSpawnDelay = 4f;
    public float minimumSpawnDelay = 1.5f; 

    public float initialFallSpeed = 2f;
    public float maxFallSpeed = 7f;

    public int minLettersPerWave = 1;
    public int maxLettersLimit = 5;

    [Header("Difficulty: Clustering")]
    [Tooltip("Distance between non-clustered letters.")]
    public float standardVerticalStagger = 1.5f;   
    [Tooltip("Starting chance for a letter to cluster (0 = 0%, 1 = 100%)")]
    [Range(0f, 1f)] public float minClusterProbability = 0.0f; 
    [Tooltip("Maximum chance for a letter to cluster when the game gets hard")]
    [Range(0f, 1f)] public float maxClusterProbability = 0.6f; 

    [Header("Difficulty: Trajectory Curves (0.0 to 1.0)")]
    [Tooltip("Draw how fast the speed increases over time")]
    public AnimationCurve speedCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    [Tooltip("Draw how fast the delay between spawns shrinks")]
    public AnimationCurve spawnDelayCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    [Tooltip("Draw how quickly the game starts clustering letters")]
    public AnimationCurve clusterCurve = AnimationCurve.Linear(0, 0, 1, 1);

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

        // 1. Calculate our overall timeline progress (0.0 to 1.0)
        float progress = Mathf.Clamp01(gameTimer / timeToReachMaxDifficulty);

        // 2. Look at the graphs to see exactly where our multipliers should be!
        float delayMultiplier = spawnDelayCurve.Evaluate(progress);
        float speedMultiplier = speedCurve.Evaluate(progress);

        // 3. Apply the multipliers to our min/max limits
        currentSpawnDelay = Mathf.Lerp(initialSpawnDelay, minimumSpawnDelay, delayMultiplier);
        currentFallSpeed = Mathf.Lerp(initialFallSpeed, maxFallSpeed, speedMultiplier);

        if (spawnTimer >= currentSpawnDelay)
        {
            SpawnWave();
            spawnTimer = 0f;
        }

        CheckActiveWaves();
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

            // If a letter was missed and destroyed by hitting the bottom line, remove the whole group
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

        // Shuffle the X positions so they don't spawn in a predictable diagonal
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
                    // CLUSTER: Do not increase Y, stay grouped with the previous letter
                }
                else
                {
                    // NO CLUSTER: Move higher up, finalize the previous group
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