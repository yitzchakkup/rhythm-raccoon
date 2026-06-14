using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum WavePattern 
{ 
    Standard, // Staggered heights, caught individually (but can randomly cluster!)
    Chord     // All drop at the exact same height simultaneously
}

public class WordGenerator : MonoBehaviour
{
    [Header("Spawn Zone (Anchor Points)")]
    public Transform leftSpawnBound;
    public Transform rightSpawnBound;

    [Header("Prefabs")]
    public GameObject[] spawnablePrefabs; 

    [Header("Difficulty: Timing & Speed")]
    public float initialSpawnDelay = 4f;
    public float minimumSpawnDelay = 1.5f; 
    public float delayDecreaseRate = 0.02f;

    public float initialFallSpeed = 2f;
    public float maxFallSpeed = 7f;
    public float speedIncreaseRate = 0.05f;

    [Header("Difficulty: Amount")]
    public int minLettersPerWave = 1;
    public int maxLettersLimit = 5;
    public float timeToReachMaxLetters = 60f;

    [Header("Wave Patterns")]
    public float standardVerticalStagger = 1.5f;   
    [Range(0f, 1f)] public float chordProbability = 0.5f; 
    
    [Header("Visuals")]
    public GameObject connectionCordPrefab; // Drag your new prefab here!
    
    // --- NEW: The chance that a standard letter groups up with the previous one ---
    [Range(0f, 1f)] public float clusterProbability = 0.3f; 

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

        currentSpawnDelay = Mathf.Max(minimumSpawnDelay, initialSpawnDelay - (gameTimer * delayDecreaseRate));
        currentFallSpeed = Mathf.Min(maxFallSpeed, initialFallSpeed + (gameTimer * speedIncreaseRate));

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

    private void SpawnWave()
    {
        float progress = Mathf.Clamp01(gameTimer / timeToReachMaxLetters);
        int lettersToSpawn = Mathf.RoundToInt(Mathf.Lerp(minLettersPerWave, maxLettersLimit, progress));

        float leftEdge = leftSpawnBound.position.x;
        float rightEdge = rightSpawnBound.position.x;
        float spacing = (rightEdge - leftEdge) / (lettersToSpawn + 1);
        float spawnY = leftSpawnBound.position.y; 

        List<Key> availableKeys = new List<Key>();
        for (int k = (int)Key.A; k <= (int)Key.Z; k++)
        {
            availableKeys.Add((Key)k);
        }

        WavePattern currentPattern = Random.value < chordProbability ? WavePattern.Chord : WavePattern.Standard;

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

        // --- UPDATED: Unified Grouping Logic ---
        List<FallingLetter> currentWorkingGroup = new List<FallingLetter>();
        float currentY = spawnY;

        for (int i = 0; i < lettersToSpawn; i++)
        {
            GameObject prefab = spawnablePrefabs[Random.Range(0, spawnablePrefabs.Length)];
            
            // If it's a Standard pattern AND it's not the very first letter, we check for a cluster
            if (currentPattern == WavePattern.Standard && i > 0)
            {
                if (Random.value < clusterProbability)
                {
                    // CLUSTER TRIGGERED! 
                    // We DO NOT increase currentY, so it falls alongside the previous letter.
                }
                else
                {
                    // NO CLUSTER. Move it higher up the screen.
                    currentY += standardVerticalStagger;

                    FinalizeWaveGroup(currentWorkingGroup); // TO THIS
                    currentWorkingGroup = new List<FallingLetter>();
                }
            }
            // (If it's a Chord, currentY just stays at spawnY for the entire loop!)

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

                // Add the letter to whatever the current working group is
                currentWorkingGroup.Add(letterScript);
            }
        }

        // After the loop finishes, save whatever group was being worked on at the end
        if (currentWorkingGroup.Count > 0)
        {
            FinalizeWaveGroup(currentWorkingGroup); // TO THIS
        }
    }
    
    // --- NEW: Helper method to handle finalizing a group and adding the cord ---
    private void FinalizeWaveGroup(List<FallingLetter> group)
    {
        if (group.Count == 0) return;
        
        activeWaves.Add(group);

        // If the group has more than one letter, spawn the visual cord!
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
}