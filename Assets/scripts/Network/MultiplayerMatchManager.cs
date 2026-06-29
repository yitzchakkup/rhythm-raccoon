using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(PhotonView))]
public class MultiplayerMatchManager : MonoBehaviourPun
{
    [Header("End Screen Music")]
    public AudioClip winMusicSting;
    public AudioClip[] winPlaylist;
    public AudioClip loseMusicSting;
    public AudioClip[] losePlaylist;
    public AudioClip gameLoopMusic; // Your original BossaBossa track
    
    public static MultiplayerMatchManager Instance { get; private set; }

    private float currentMyScore = 0f;
    private float currentOpponentScore = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Start()
    {
        ResetData();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetData();
    }

    private void ResetData()
    {
        currentMyScore = 0f;
        currentOpponentScore = 0f;

        if (!IsMultiplayerGame())
        {
            if (SceneUIRefs.tugOfWarUI != null) SceneUIRefs.tugOfWarUI.SetActive(false);
        }
        else
        {
            if (SceneUIRefs.staminaBar != null) SceneUIRefs.staminaBar.SetActive(false);
        }
    }

    public bool IsMultiplayerGame()
    {
        return !PhotonNetwork.OfflineMode && PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount > 1;
    }

    public void SyncMyScore(float myTotalScore)
    {
        currentMyScore = myTotalScore;
        if (!IsMultiplayerGame()) return;
        photonView.RPC("ReceiveOpponentScore_RPC", RpcTarget.Others, myTotalScore);
    }

    [PunRPC]
    private void ReceiveOpponentScore_RPC(float opponentScore)
    {
        if (MatchSyncManager.Instance == null || !MatchSyncManager.Instance.matchStarted) return;
        currentOpponentScore = opponentScore;
    }

    public void SendAttackToOpponent(string attackName)
    {
        if (!IsMultiplayerGame()) return;
        photonView.RPC("ReceiveAttack_RPC", RpcTarget.Others, attackName);
    }

    [PunRPC]
    private void ReceiveAttack_RPC(string attackName)
    {
        if (MatchSyncManager.Instance == null || !MatchSyncManager.Instance.matchStarted) return;

        if (AvatarController.Instance != null) AvatarController.Instance.PlayLocalDamageEffect();

        float duration = 0f;
        switch (attackName)
        {
            case "HalveScore":
                duration = 10f;
                if (ScoreAndStaminaManager.Instance != null)
                    ScoreAndStaminaManager.Instance.ActivateScoreMultiplier(0.5f, duration);
                break;
            case "TempoShift":
                duration = 4f;
                WordGenerator generator = FindAnyObjectByType<WordGenerator>();
                if (generator != null)
                {
                    generator.TriggerSpeedAttack(2.5f, duration);
                }
                break;
        }

        if (PowerupUIManager.Instance != null) PowerupUIManager.Instance.ActivateIcon(attackName, duration);
    }

    // --- THE NEW CINEMATIC SOFT RESET ---
    public void TriggerSoftReset()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("ExecuteSoftReset_RPC", RpcTarget.All);
        }
    }

    [PunRPC]
    private void ExecuteSoftReset_RPC()
    {
        StartCoroutine(SoftResetRoutine());
    }

    private IEnumerator SoftResetRoutine()
    {
        Debug.Log("<color=green>[Soft Reset]</color> Wiping board, fading UI, and restarting generators!");

        // 1. Wipe the scores and destroy leftover letters INSTANTLY so the board is clean behind the fading UI
        ResetMatch();

        // 2. Gather all active end screens to fade them out
        List<CanvasGroup> groupsToFade = new List<CanvasGroup>();

        void AddGroup(GameObject obj)
        {
            if (obj != null && obj.activeSelf)
            {
                if (!obj.TryGetComponent<CanvasGroup>(out CanvasGroup cg))
                {
                    cg = obj.AddComponent<CanvasGroup>();
                }
                groupsToFade.Add(cg);
            }
        }

        AddGroup(SceneUIRefs.multiplayerEndLayout);
        AddGroup(SceneUIRefs.possumWinBackground);
        AddGroup(SceneUIRefs.raccoonWinBackground);
        AddGroup(SceneUIRefs.sharedEndGameLayout);

        // 3. Fade them out smoothly over 0.5 seconds to reveal the clean board
        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / duration);
            foreach (CanvasGroup cg in groupsToFade)
            {
                if (cg != null) cg.alpha = alpha;
            }
            yield return null;
        }

        // 4. Turn them off and reset alpha back to 1 for the next time the match ends
        foreach (CanvasGroup cg in groupsToFade)
        {
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.gameObject.SetActive(false);
            }
        }

        // 5. Reset the Tug of War logic lock
        TugOfWarUI tugUI = FindAnyObjectByType<TugOfWarUI>();
        if (tugUI != null) tugUI.ResetTugOfWar();

        // 6. Reset our Network "Ready" vote 
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable { { GameConstants.PLAY_AGAIN_KEY, false } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        if (AudioManager.Instance != null && gameLoopMusic != null)
        {
            AudioManager.Instance.PlayMusic(gameLoopMusic);
        }

        // 7. Ensure the tutorial doesn't accidentally trigger again
        if (MatchSyncManager.Instance != null) MatchSyncManager.Instance.matchStarted = true;

        // 8. Turn the Generators back on!
        WordGenerator wg = FindAnyObjectByType<WordGenerator>();
        if (wg != null)
        {
            wg.enabled = true;
            wg.StartGameLoop();
        }

        PowerupGenerator pg = FindAnyObjectByType<PowerupGenerator>();
        if (pg != null) pg.enabled = true;
    }

    public void ResetMatch()
    {
        if (ScoreAndStaminaManager.Instance != null) ScoreAndStaminaManager.Instance.Initialize();

        currentMyScore = 0f;
        currentOpponentScore = 0f;

        FallingLetter[] activeLetters = FindObjectsByType<FallingLetter>(FindObjectsSortMode.None);
        foreach (FallingLetter letter in activeLetters) Destroy(letter.gameObject);
    }
    
    public void PlayEndGameAudio(bool iWon)
    {
        if (AudioManager.Instance == null) return;

        // Use a 0.8s fade for the "Sting" so it hits hard but isn't jarring
        AudioManager.Instance.PlayMusic(iWon ? winMusicSting : loseMusicSting, 0.8f);

        // Start the playlist routine shortly after
        StartCoroutine(DelayedPlaylist(iWon ? winPlaylist : losePlaylist));
    }

    private IEnumerator DelayedPlaylist(AudioClip[] playlist)
    {
        // Give the sting 1.5 seconds to shine before looping the playlist
        yield return new WaitForSeconds(1.5f); 
        
        // Pass the fade duration to the playlist so it knows how to overlap
        AudioManager.Instance.PlayPlaylist(playlist, 1.2f);
    }

    public float GetMyScore() => currentMyScore;
    public float GetOpponentScore() => currentOpponentScore;
}