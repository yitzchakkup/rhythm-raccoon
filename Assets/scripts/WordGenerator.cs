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

    private float currentSpawnDelay;
    private float currentFallSpeed;
    private float spawnTimer;
    private float gameTimer;

    // --- NEW: Tracking the words/waves ---
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

        // --- NEW: Check active waves every frame ---
        CheckActiveWaves();
    }

    private void CheckActiveWaves()
    {
        // We loop backwards because we might remove waves from the list while checking them
        for (int i = activeWaves.Count - 1; i >= 0; i--)
        {
            List<FallingLetter> wave = activeWaves[i];

            // 1. Check if the player missed this wave (any letter hit the bottom and was destroyed)
            // If any letter is null, they failed this wave. We stop tracking it.
            bool missedLetter = false;
            foreach (FallingLetter letter in wave)
            {
                if (letter == null) missedLetter = true;
            }

            if (missedLetter)
            {
                activeWaves.RemoveAt(i);
                continue; // Skip to the next wave
            }

            // 2. Check if all letters in this specific wave are in the zone AND pressed
            bool waveComplete = true;
            foreach (FallingLetter letter in wave)
            {
                if (!letter.inZone || !letter.isPressed)
                {
                    waveComplete = false;
                    break;
                }
            }

            // 3. If they successfully held the whole word in the zone!
            // 3. If they successfully held the whole word in the zone!
            if (waveComplete)
            {
                // --- UPDATED: Calculate dynamic score based on zones ---
                if (ScoreManager.Instance != null)
                {
                    int totalWaveScore = 0;
                    foreach (FallingLetter letter in wave)
                    {
                        totalWaveScore += letter.GetScoreValue();
                    }
                    ScoreManager.Instance.AddScore(totalWaveScore);
                }
                // -------------------------------------------------------

                // Apply powerups and destroy the objects
                foreach (FallingLetter letter in wave)
                {
                    if (letter.TryGetComponent<Powerup>(out Powerup powerup))
                    {
                        powerup.ApplyEffect();
                    }
                    Destroy(letter.gameObject);
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

        List<FallingLetter> newWave = new List<FallingLetter>();

        // --- NEW: Create a pool of available keys (A to Z) ---
        List<Key> availableKeys = new List<Key>();
        for (int k = (int)Key.A; k <= (int)Key.Z; k++)
        {
            availableKeys.Add((Key)k);
        }
        // -----------------------------------------------------

        for (int i = 0; i < lettersToSpawn; i++)
        {
            GameObject prefab = spawnablePrefabs[Random.Range(0, spawnablePrefabs.Length)];
            Vector3 position = new Vector3(leftEdge + (spacing * (i + 1)), spawnY, 0f);
            GameObject spawnedObj = Instantiate(prefab, position, Quaternion.identity);

            FallingLetter letterScript = spawnedObj.GetComponent<FallingLetter>();
            if (letterScript != null)
            {
                letterScript.SetFallSpeed(currentFallSpeed);
                
                // --- NEW: Pick a random key from the pool and remove it ---
                int randomIndex = Random.Range(0, availableKeys.Count);
                Key assignedKey = availableKeys[randomIndex];
                availableKeys.RemoveAt(randomIndex); // Prevents duplicates in this wave
                // ----------------------------------------------------------

                letterScript.SetupRandomLetter(assignedKey);
                newWave.Add(letterScript);
            }
        }

        activeWaves.Add(newWave);
    }
}