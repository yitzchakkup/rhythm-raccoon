using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
// --- NEW: Required for Photon functionality ---
using Photon.Pun;

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

        // --- CRITICAL NEW CHECK ---
        // Only the Host handles powerup timers and generation
        if (!PhotonNetwork.IsMasterClient) return;

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
        float leftEdge = leftSpawnBound.position.x;
        float rightEdge = rightSpawnBound.position.x;
        float randomX = Random.Range(leftEdge, rightEdge);
        Vector3 spawnPosition = new Vector3(randomX, leftSpawnBound.position.y, 0f);

        GameObject prefab = powerupPrefabs[Random.Range(0, powerupPrefabs.Length)];
        
        // --- UPDATED FOR NETWORKING ---
        GameObject spawnedObj = PhotonNetwork.Instantiate(prefab.name, spawnPosition, Quaternion.identity);

        FallingLetter letterScript = spawnedObj.GetComponent<FallingLetter>();
        if (letterScript != null)
        {
            letterScript.SetFallSpeed(fallSpeed);
            
            var randomHardKey = hardKeys[Random.Range(0, hardKeys.Length)];
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

            if (powerupLetter.inZone && powerupLetter.isPressed)
            {
                if (powerupLetter.TryGetComponent<Powerup>(out Powerup powerupComponent))
                {
                    powerupComponent.ApplyEffect();
                }

                // --- UPDATED FOR NETWORKING ---
                // Instead of local Destroy, tell the network to wipe out this object
                PhotonNetwork.Destroy(powerupLetter.gameObject);
                activePowerups.RemoveAt(i);
            }
        }
    }
}