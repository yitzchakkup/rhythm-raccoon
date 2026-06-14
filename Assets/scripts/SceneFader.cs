using UnityEngine;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public float fadeSpeed = 0.5f;
    private CanvasGroup canvasGroup;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            // The scene just loaded! Start fading from black to clear instantly.
            StartCoroutine(FadeInRoutine());
        }
    }

    private IEnumerator FadeInRoutine()
    {
        float timer = 0f;
        while (timer < fadeSpeed)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeSpeed);
            timer += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false; // Let the player play the game!
    }
}