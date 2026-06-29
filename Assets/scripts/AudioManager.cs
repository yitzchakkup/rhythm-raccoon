using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("Dedicated source for background music")]
    [SerializeField] private AudioSource musicSource;
    [Tooltip("Dedicated source for sound effects")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Pitch Settings")]
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    // --- NEW: Fade Tracking Variables ---
    private Coroutine activeMusicFade;
    private float targetMusicVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
    }

    private void Start()
    {
        // Remember whatever volume the AudioSource started at in the Inspector
        if (musicSource != null)
        {
            targetMusicVolume = musicSource.volume;
        }
    }

    public void PlaySFX(AudioClip clip, bool randomizePitch = false)
    {
        if (clip == null || sfxSource == null) return;

        if (randomizePitch)
        {
            sfxSource.pitch = Random.Range(minPitch, maxPitch);
        }
        else
        {
            sfxSource.pitch = 1f; 
        }

        sfxSource.PlayOneShot(clip);
    }

    // --- UPGRADED: PlayMusic now accepts an optional fade duration ---
    public void PlayMusic(AudioClip musicClip, float fadeDuration = 1.5f)
    {
        if (musicClip == null || musicSource == null) return;

        // If the exact same song is already playing, do nothing!
        if (musicSource.clip == musicClip) return;

        // Stop any fade currently happening so the math doesn't glitch out
        if (activeMusicFade != null)
        {
            StopCoroutine(activeMusicFade);
        }

        // Start the crossfade sequence
        activeMusicFade = StartCoroutine(CrossfadeMusicRoutine(musicClip, fadeDuration));
    }

    private IEnumerator CrossfadeMusicRoutine(AudioClip newClip, float totalFadeDuration)
    {
        // We split the time in half: Half for fading out, half for fading in
        float halfDuration = totalFadeDuration / 2f;

        // 1. FADE OUT (If a song is currently playing)
        if (musicSource.isPlaying)
        {
            float currentVol = musicSource.volume;
            float timer = 0f;

            while (timer < halfDuration)
            {
                musicSource.volume = Mathf.Lerp(currentVol, 0f, timer / halfDuration);
                timer += Time.deltaTime;
                yield return null;
            }
        }
        
        // Guarantee volume is at exactly 0 before the swap
        musicSource.volume = 0f; 

        // 2. SWAP THE TRACK
        musicSource.clip = newClip;
        musicSource.loop = true;
        musicSource.Play();

        // 3. FADE IN
        float fadeTimer = 0f;
        while (fadeTimer < halfDuration)
        {
            musicSource.volume = Mathf.Lerp(0f, targetMusicVolume, fadeTimer / halfDuration);
            fadeTimer += Time.deltaTime;
            yield return null;
        }

        // Guarantee volume is perfectly restored
        musicSource.volume = targetMusicVolume;
        activeMusicFade = null;
    }
    
    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }
    
    // --- NEW: Playlist functionality ---
    public void PlayPlaylist(AudioClip[] playlist, float fadeDuration = 1.0f)
    {
        if (playlist == null || playlist.Length == 0) return;
        StartCoroutine(PlaylistRoutine(playlist, fadeDuration));
    }

    // Updated Playlist Routine for seamless transitions
    private IEnumerator PlaylistRoutine(AudioClip[] playlist, float fadeDuration)
    {
        int i = 0;
        while (true)
        {
            AudioClip nextClip = playlist[i % playlist.Length];
            
            // Only start the fade when the current song is nearing its end
            // We subtract the fadeDuration so the songs overlap perfectly
            float waitTime = nextClip.length - fadeDuration;
            
            // Trigger the fade
            PlayMusic(nextClip, fadeDuration);
            
            // Wait for the duration of the song minus the overlap
            yield return new WaitForSeconds(waitTime > 0 ? waitTime : 1f);
            
            i++;
        }
    }
}