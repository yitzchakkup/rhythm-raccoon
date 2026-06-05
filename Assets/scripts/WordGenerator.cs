using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
// --- NEW: Required for Photon functionality ---
using Photon.Pun; 

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

    private List<List<FallingLetter>> activeWaves = new List<List<FallingLetter>>();

    void Start()
    {
        currentSpawnDelay = initialSpawnDelay;
        currentFallSpeed = initialFallSpeed;

        // --- NEW: Only the Host initializes the first wave ---
        if (PhotonNetwork.IsMasterClient)
        {
            SpawnWave();
        }
    }

    void Update()
    {
        if (leftSpawnBound == null || rightSpawnBound == null) return;

        // --- CRITICAL NEW CHECK ---
        // If we are multiplayer and I am the guest client, do absolutely nothing.
        // The Host's computer will handle all timers and network spawning.
        if (!PhotonNetwork.IsMasterClient) return;

        gameTimer += Time.deltaTime;
        spawnTimer += Time.deltaTime;

        currentSpawnDelay = Mathf.Max(minimumSpawnDelay, initialSpawnDelay - (gameTimer * delayDecreaseRate));
        currentFallSpeed = Mathf.Min(maxFallSpeed, initialFallSpeed + (gameTimer * speedIncreaseRate));

        if (spawnTimer >= currentSpawnDelay)
        {
            SpawnWave();
            spawnTimer = 0f;
        }
    }

    void SpawnWave()
    {
        float leftEdge = leftSpawnBound.position.x;
        float rightEdge = rightSpawnBound.position.x;
        float totalWidth = rightEdge - leftEdge;

        float progress = Mathf.Clamp01(gameTimer / timeToReachMaxLetters);
        int lettersToSpawn = Mathf.FloorToInt(Mathf.Lerp(minLettersPerWave, maxLettersLimit + 1, progress));
        lettersToSpawn = Mathf.Clamp(lettersToSpawn, minLettersPerWave, maxLettersLimit);

        float spacing = totalWidth / (lettersToSpawn + 1);
        float spawnY = leftSpawnBound.position.y; 

        List<FallingLetter> newWave = new List<FallingLetter>();

        List<Key> availableKeys = new List<Key>();
        for (int k = (int)Key.A; k <= (int)Key.Z; k++)
        {
            availableKeys.Add((Key)k);
        }

        for (int i = 0; i < lettersToSpawn; i++)
        {
            GameObject prefab = spawnablePrefabs[Random.Range(0, spawnablePrefabs.Length)];
            Vector3 position = new Vector3(leftEdge + (spacing * (i + 1)), spawnY, 0f);
            
            // --- UPDATED FOR NETWORKING ---
            // Instead of standard Instantiate, we use PhotonNetwork.Instantiate.
            // It tracks objects using the prefab's string name inside the Resources folder.
            GameObject spawnedObj = PhotonNetwork.Instantiate(prefab.name, position, Quaternion.identity);

            FallingLetter letterScript = spawnedObj.GetComponent<FallingLetter>();
            if (letterScript != null)
            {
                letterScript.SetFallSpeed(currentFallSpeed);
                
                int randomIndex = Random.Range(0, availableKeys.Count);
                Key assignedKey = availableKeys[randomIndex];
                availableKeys.RemoveAt(randomIndex); 

                // --- IMPORTANT NOTE FOR LATER ---
                // SetupRandomLetter modifies the visual text. We will want to sync this 
                // inside FallingLetter.cs using a Photon view initialization or an RPC.
                letterScript.SetupRandomLetter(assignedKey);
                newWave.Add(letterScript);
            }
        }

        activeWaves.Add(newWave);
    }
}