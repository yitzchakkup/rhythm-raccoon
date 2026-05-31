using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PowerupGenerator : MonoBehaviour
{
    [Header("Spawn Zone (Anchor Points)")]
    public Transform leftSpawnBound;
    public Transform rightSpawnBound;

    [Header("Powerup Settings")]
    public GameObject[] powerupPrefabs; 
    public float spawnInterval = 15f; 
    public float fallSpeed = 3f;

    private float spawnTimer;
    private List<FallingLetter> activePowerups = new List<FallingLetter>();

    // --- NEW: A list of harder keys and their visual symbols ---
    private readonly (Key key, string symbol)[] hardKeys = new (Key, string)[]
    {
        (Key.Digit1, "1"), (Key.Digit2, "2"), (Key.Digit3, "3"), 
        (Key.Digit4, "4"), (Key.Digit5, "5"), (Key.Digit6, "6"), 
        (Key.Digit7, "7"), (Key.Digit8, "8"), (Key.Digit9, "9"), (Key.Digit0, "0"),
        (Key.Minus, "-"), (Key.Equals, "="), (Key.LeftBracket, "["), 
        (Key.RightBracket, "]"), (Key.Semicolon, ";"), (Key.Quote, "'"), 
        (Key.Comma, ","), (Key.Period, "."), (Key.Slash, "/")
    };

    void Update()
    {
        if (leftSpawnBound == null || rightSpawnBound == null) return;

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval)
        {
            SpawnPowerup();
            spawnTimer = 0f;
        }

        CheckActivePowerups();
    }

    private void SpawnPowerup()
    {
        if (powerupPrefabs.Length == 0) return;

        float randomX = Random.Range(leftSpawnBound.position.x, rightSpawnBound.position.x);
        Vector3 spawnPosition = new Vector3(randomX, leftSpawnBound.position.y, 0f);

        GameObject prefab = powerupPrefabs[Random.Range(0, powerupPrefabs.Length)];
        GameObject spawnedObj = Instantiate(prefab, spawnPosition, Quaternion.identity);

        FallingLetter letterScript = spawnedObj.GetComponent<FallingLetter>();
        if (letterScript != null)
        {
            letterScript.SetFallSpeed(fallSpeed);
            
            // --- NEW: Pick a random symbol/number from our hardKeys array ---
            var randomHardKey = hardKeys[Random.Range(0, hardKeys.Length)];
            
            // Pass BOTH the key logic AND the visual symbol to display
            letterScript.SetupRandomLetter(randomHardKey.key, randomHardKey.symbol);

            activePowerups.Add(letterScript);
        }
    }

    private void CheckActivePowerups()
    {
        for (int i = activePowerups.Count - 1; i >= 0; i--)
        {
            FallingLetter powerupLetter = activePowerups[i];

            if (powerupLetter == null)
            {
                activePowerups.RemoveAt(i);
                continue;
            }

            // Powerups are collected individually, so we just check if this one is pressed
            if (powerupLetter.inZone && powerupLetter.isPressed)
            {
                if (powerupLetter.TryGetComponent<Powerup>(out Powerup powerupComponent))
                {
                    powerupComponent.ApplyEffect();
                }

                Destroy(powerupLetter.gameObject);
                activePowerups.RemoveAt(i);
            }
        }
    }
}