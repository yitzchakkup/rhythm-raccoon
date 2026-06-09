using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun; // --- NEW: Needed to check our network status ---

public class PowerupGenerator : MonoBehaviour
{
    [Header("Spawn Zone (Anchor Points)")]
    public Transform leftSpawnBound;
    public Transform rightSpawnBound;

    // --- UPDATED: Two distinct pools of powerups ---
    [Header("Powerup Pools")]
    public GameObject[] standardBuffs;     // Put DoubleScore, DoubleStamina here
    public GameObject[] opponentAttacks;   // Put HalveScore, HalveStamina here

    [Header("Generator Settings")]
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
        // 1. Always start with the standard buffs
        List<GameObject> validPrefabs = new List<GameObject>(standardBuffs);

        // 2. If we are online AND connected to another player, add the attacks to the pool!
        if (!PhotonNetwork.OfflineMode && PhotonNetwork.IsConnected && PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            validPrefabs.AddRange(opponentAttacks);
        }

        // Safety check just in case the inspector lists are empty
        if (validPrefabs.Count == 0) return;

        float randomX = Random.Range(leftSpawnBound.position.x, rightSpawnBound.position.x);
        Vector3 spawnPosition = new Vector3(randomX, leftSpawnBound.position.y, 0f);

        // 3. Pick randomly from the combined valid list
        GameObject prefab = validPrefabs[Random.Range(0, validPrefabs.Count)];
        GameObject spawnedObj = Instantiate(prefab, spawnPosition, Quaternion.identity);

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

                Destroy(powerupLetter.gameObject);
                activePowerups.RemoveAt(i);
            }
        }
    }
}