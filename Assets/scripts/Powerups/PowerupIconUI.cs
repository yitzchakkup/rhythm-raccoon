using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PowerupIconUI : MonoBehaviour
{
    [Tooltip("The exact string name of the powerup (e.g., 'SlowMo', 'HalveScore')")]
    public string powerupName;
    
    [SerializeField] private Image activeFillIcon;

    private Coroutine wipeCoroutine;

    private void Awake()
    {
        if (activeFillIcon != null) 
        {
            activeFillIcon.fillAmount = 0f;
        }
    }

    public void TriggerClockWipe(float duration)
    {
        if (wipeCoroutine != null) StopCoroutine(wipeCoroutine);
        wipeCoroutine = StartCoroutine(WipeRoutine(duration));
    }

    private IEnumerator WipeRoutine(float duration)
    {
        float timer = duration;

        while (timer > 0)
        {
            timer -= Time.deltaTime;
            
            if (activeFillIcon != null)
            {
                // Calculates a percentage from 1.0 down to 0.0
                activeFillIcon.fillAmount = timer / duration;
            }
            yield return null; // Wait for the next frame
        }

        if (activeFillIcon != null) 
        {
            activeFillIcon.fillAmount = 0f;
        }
    }
}