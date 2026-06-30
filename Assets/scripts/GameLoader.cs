using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameLoader : MonoBehaviour
{
    private void Start()
    {
        // Start the loading process as soon as the game boots
        StartCoroutine(BootSequence());
    }

    private IEnumerator BootSequence()
    {
        // 1. Give Unity a tiny fraction of a second to initialize all Awake() methods on your global managers
        yield return new WaitForSeconds(0.2f);

        // 2. Load the Main Menu safely
        // We use the string name here so we don't accidentally load the wrong index later
        SceneManager.LoadScene("LobbyScene"); 
    }
}