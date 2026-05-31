using UnityEngine;
using UnityEngine.Tilemaps; // Required for Tilemap components

public class WordGenerator : MonoBehaviour
{
    [Header("Spawn Zone")]
    [Tooltip("Drag the Tilemap that represents the play area here.")]
    public TilemapRenderer boardTilemap;
    [Tooltip("Adds a little extra height above the tilemap so letters spawn just off-screen.")]
    public float spawnHeightOffset = 1f;

    [Header("Prefabs")]
    public GameObject[] spawnablePrefabs; 

    [Header("Difficulty: Timing & Speed")]
    public float initialSpawnDelay = 3f;
    public float minimumSpawnDelay = 1f;
    public float delayDecreaseRate = 0.05f;

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

    void Start()
    {
        currentSpawnDelay = initialSpawnDelay;
        currentFallSpeed = initialFallSpeed;

        if (boardTilemap == null)
        {
            Debug.LogError("WordSpawner is missing a reference to the Tilemap!");
        }
    }

    void Update()
    {
        if (boardTilemap == null) return; // Prevent errors if not assigned

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

    private void SpawnWave()
    {
        float progress = Mathf.Clamp01(gameTimer / timeToReachMaxLetters);
        int lettersToSpawn = Mathf.RoundToInt(Mathf.Lerp(minLettersPerWave, maxLettersLimit, progress));

        // Get the absolute world boundaries of the Tilemap
        Bounds mapBounds = boardTilemap.bounds;
        float leftEdge = mapBounds.min.x;
        float rightEdge = mapBounds.max.x;
        
        // Spawn slightly above the top edge of the tilemap
        float spawnY = mapBounds.max.y + spawnHeightOffset; 

        // Calculate horizontal spacing
        float availableWidth = rightEdge - leftEdge;
        float spacing = availableWidth / (lettersToSpawn + 1);

        for (int i = 0; i < lettersToSpawn; i++)
        {
            GameObject prefabToSpawn = spawnablePrefabs[Random.Range(0, spawnablePrefabs.Length)];

            float spawnX = leftEdge + (spacing * (i + 1));
            Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0f);

            GameObject spawnedObj = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

            FallingLetter letterScript = spawnedObj.GetComponent<FallingLetter>();
            if (letterScript != null)
            {
                letterScript.SetFallSpeed(currentFallSpeed);
            }
        }
    }
}