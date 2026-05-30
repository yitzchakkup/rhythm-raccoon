using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // Import the new Input System namespace

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int Score { get; private set; } = 0;

    [Header("Hit Colors")]
    public Color perfectHitColor = new Color(0f, 0.5f, 0f); // Dark Green
    public Color goodHitColor = Color.green; // Light Green

    // --- UPDATED ---
    // Track letters using the new Input System 'Key' enum
    private Dictionary<Key, List<GameObject>> innerZoneLetters = new Dictionary<Key, List<GameObject>>();
    private Dictionary<Key, List<GameObject>> outerZoneLetters = new Dictionary<Key, List<GameObject>>();

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    void Update()
    {
        // --- UPDATED ---
        // Ensure we have a keyboard connected before checking for input
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return; // No keyboard connected, do nothing.
        }

        // Build a list of unique keys to check from both zones
        List<Key> keysToCheck = new List<Key>();
        keysToCheck.AddRange(innerZoneLetters.Keys);
        
        foreach (var key in outerZoneLetters.Keys)
        {
            if (!keysToCheck.Contains(key))
            {
                keysToCheck.Add(key);
            }
        }

        // Check for key presses using the new Input System
        foreach (Key key in keysToCheck)
        {
            // Check if the key was pressed during this frame
            if (keyboard[key].wasPressedThisFrame)
            {
                ProcessKeyPress(key);
            }
        }
    }

    // --- UPDATED ---
    // Method now accepts 'Key' instead of 'KeyCode'
    private void ProcessKeyPress(Key key)
    {
        // 1. Check Inner Zone first
        if (innerZoneLetters.ContainsKey(key) && innerZoneLetters[key].Count > 0)
        {
            GameObject hitLetter = innerZoneLetters[key][0];
            
            Score += 2;
            Debug.Log($"Perfect Hit! +2 points. Total Score: {Score}");

            FallingLetter fallingLetter = hitLetter.GetComponent<FallingLetter>();
            if (fallingLetter != null)
            {
                fallingLetter.WasHit(perfectHitColor);
            }

            RemoveLetterFromAllLists(hitLetter);
            return;
        }

        // 2. Check Outer Zone
        if (outerZoneLetters.ContainsKey(key) && outerZoneLetters[key].Count > 0)
        {
            GameObject hitLetter = outerZoneLetters[key][0];
            
            Score += 1;
            Debug.Log($"Good Hit! +1 point. Total Score: {Score}");

            FallingLetter fallingLetter = hitLetter.GetComponent<FallingLetter>();
            if (fallingLetter != null)
            {
                fallingLetter.WasHit(goodHitColor);
            }

            RemoveLetterFromAllLists(hitLetter);
        }
    }

    private void RemoveLetterFromAllLists(GameObject letter)
    {
        foreach (var kvp in innerZoneLetters)
        {
            kvp.Value.Remove(letter);
        }
        foreach (var kvp in outerZoneLetters)
        {
            kvp.Value.Remove(letter);
        }
    }

    // --- Public Methods Updated to use 'Key' ---

    public void LetterEnteredOuterZone(Key key, GameObject letter)
    {
        if (!outerZoneLetters.ContainsKey(key))
        {
            outerZoneLetters[key] = new List<GameObject>();
        }
        if (!outerZoneLetters[key].Contains(letter))
        {
             outerZoneLetters[key].Add(letter);
        }
    }

    public void LetterEnteredInnerZone(Key key, GameObject letter)
    {
        if (!innerZoneLetters.ContainsKey(key))
        {
            innerZoneLetters[key] = new List<GameObject>();
        }
        if (!innerZoneLetters[key].Contains(letter))
        {
             innerZoneLetters[key].Add(letter);
        }
        if (outerZoneLetters.ContainsKey(key))
        {
            outerZoneLetters[key].Remove(letter);
        }
    }

    public void LetterLeftInnerZone(Key key, GameObject letter)
    {
        if (innerZoneLetters.ContainsKey(key))
        {
            innerZoneLetters[key].Remove(letter);
        }
    }

    public void LetterLeftOuterZone(Key key, GameObject letter)
    {
        if (outerZoneLetters.ContainsKey(key))
        {
            outerZoneLetters[key].Remove(letter);
        }
    }
}