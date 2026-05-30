using UnityEngine;
using UnityEngine.InputSystem; // Import the new Input System namespace

public class FallingLetter : MonoBehaviour
{
    [Header("Input Settings")]
    // --- UPDATED ---
    // Use the 'Key' enum from the new Input System.
    // You can now select keys like 'A', 'S', 'D', 'F', etc. in the Inspector.
    [SerializeField] private Key letterKey;

    [Header("Movement Settings")]
    [SerializeField] private float fallSpeed = 2f;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // The logic here remains the same, but it now passes a 'Key' to the ScoreManager
        if (other.CompareTag("TargetLineOuter"))
        {
            Debug.Log("Collided with outer"+letterKey);
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.LetterEnteredOuterZone(letterKey, gameObject);
            }
        }
        else if (other.CompareTag("TargetLineInner"))
        {
            Debug.Log("Collided with inner"+letterKey);

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.LetterEnteredInnerZone(letterKey, gameObject);
            }
        }
        else if (other.CompareTag("LineOnderGame"))
        {
            Debug.Log("Collided with onder_game");
            Destroy(gameObject); // Destroy the letter when it hits the "onder_game" object
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("TargetLineOuter"))
        {
            if (ScoreManager.Instance != null)
            {
                // This handles the case where a letter is missed and falls past the hit zones
                ScoreManager.Instance.LetterLeftOuterZone(letterKey, gameObject);
            }
        }
    }

    public void WasHit(Color hitColor)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = hitColor;
        }
        // Disable the collider so it can't be hit again
        GetComponent<Collider2D>().enabled = false;
    }
}