using UnityEngine;
using UnityEngine.UI;
using Photon.Pun; 

public class TugOfWarUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider tugOfWarSlider;
    
    [Header("Dynamic Progress Bars")]
    [SerializeField] private Image localFillBar;
    [SerializeField] private Image opponentFillBar;
    
    [Header("Avatar Face Renderers")]
    [SerializeField] private Image localFaceImage;
    [SerializeField] private Image opponentFaceImage;

    [Header("Host Assets (Master Client)")]
    [SerializeField] private Sprite hostNormalSprite;
    [SerializeField] private Sprite hostFearSprite;
    [SerializeField] private Sprite hostBarSprite; // --- NEW: Host Bar Color ---

    [Header("Client Assets (Player 2)")]
    [SerializeField] private Sprite clientNormalSprite;
    [SerializeField] private Sprite clientFearSprite;
    [SerializeField] private Sprite clientBarSprite; // --- NEW: Client Bar Color ---

    [Header("Settings")]
    [SerializeField] private int maxScoreDifference = 25;
    [SerializeField] private int fearThreshold = 5;

    private bool isGameOver = false;

    private Sprite myNormalSprite;
    private Sprite myFearSprite;
    private Sprite opponentNormalSprite;
    private Sprite opponentFearSprite;

    void Start()
    {
        if (tugOfWarSlider != null)
        {
            tugOfWarSlider.minValue = -maxScoreDifference;
            tugOfWarSlider.maxValue = maxScoreDifference;
            tugOfWarSlider.value = 0;
        }

        AssignNetworkSprites();
        ResetFaces();
    }

    private void AssignNetworkSprites()
    {
        // Assign both the Faces and the Colored Bars based on who is playing
        if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)
        {
            myNormalSprite = hostNormalSprite;
            myFearSprite = hostFearSprite;
            if (localFillBar != null) localFillBar.sprite = hostBarSprite;
            
            opponentNormalSprite = clientNormalSprite;
            opponentFearSprite = clientFearSprite;
            if (opponentFillBar != null) opponentFillBar.sprite = clientBarSprite;
        }
        else 
        {
            myNormalSprite = clientNormalSprite;
            myFearSprite = clientFearSprite;
            if (localFillBar != null) localFillBar.sprite = clientBarSprite;
            
            opponentNormalSprite = hostNormalSprite;
            opponentFearSprite = hostFearSprite;
            if (opponentFillBar != null) opponentFillBar.sprite = hostBarSprite;
        }
    }

    void Update()
    {
        if (isGameOver || MultiplayerMatchManager.Instance == null) return;

        int myScore = MultiplayerMatchManager.Instance.GetMyScore();
        int opponentScore = MultiplayerMatchManager.Instance.GetOpponentScore();
        
        // --- THE PULL FIX ---
        // If Local is on the Left, scoring points drops the value (pulling the slider Left)
        int scoreDifference = opponentScore - myScore;

        if (tugOfWarSlider != null) tugOfWarSlider.value = scoreDifference;

        UpdateProgressBars(scoreDifference);
        UpdateFaceExpressions(scoreDifference);

        if (scoreDifference >= maxScoreDifference || scoreDifference <= -maxScoreDifference)
        {
            isGameOver = true;
            Debug.Log("Tug of War game over condition met!");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndGameMultiplayer();
            }
        }
    }

    private void UpdateProgressBars(int scoreDifference)
    {
        float totalRange = maxScoreDifference * 2f;
        // Normalize the slider position between 0.0 (Far Left) and 1.0 (Far Right)
        float normalizedValue = (scoreDifference + maxScoreDifference) / totalRange;

        // The Local Bar (anchored Left) shrinks as you pull the handle towards the Left
        if (localFillBar != null) localFillBar.fillAmount = normalizedValue;
        
        // The Opponent Bar (anchored Right) stretches to follow the handle
        if (opponentFillBar != null) opponentFillBar.fillAmount = 1f - normalizedValue;
    }

    private void UpdateFaceExpressions(int scoreDifference)
    {
        if (localFaceImage != null && myNormalSprite != null && myFearSprite != null)
        {
            int localLossBoundary = maxScoreDifference - fearThreshold;
            // Local fears when slider is pulled too far Right (away from them)
            localFaceImage.sprite = (scoreDifference >= localLossBoundary) ? myFearSprite : myNormalSprite;
        }

        if (opponentFaceImage != null && opponentNormalSprite != null && opponentFearSprite != null)
        {
            int opponentLossBoundary = -maxScoreDifference + fearThreshold;
            // Opponent fears when slider is pulled too far Left (away from them)
            opponentFaceImage.sprite = (scoreDifference <= opponentLossBoundary) ? opponentFearSprite : opponentNormalSprite;
        }
    }

    private void ResetFaces()
    {
        if (localFaceImage != null && myNormalSprite != null) 
            localFaceImage.sprite = myNormalSprite;
            
        if (opponentFaceImage != null && opponentNormalSprite != null) 
            opponentFaceImage.sprite = opponentNormalSprite;
    }
}