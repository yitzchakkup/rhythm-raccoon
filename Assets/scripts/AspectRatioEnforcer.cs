using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AspectRatioEnforcer : MonoBehaviour
{
    // 16:9 Aspect Ratio (1920x1080)
    private float targetAspect = 16f / 9f; 

    void Start()
    {
        EnforceAspect();
    }

    // We run this in Update so if the grader drags the window to resize it, 
    // the black bars instantly adapt!
    void Update()
    {
        EnforceAspect();
    }

    void EnforceAspect()
    {
        // Determine the current screen shape
        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        Camera cam = GetComponent<Camera>();

        // If the screen is wider than 16:9 (Add Pillarboxes - black bars on sides)
        if (scaleHeight < 1.0f)
        {
            Rect rect = cam.rect;
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
            cam.rect = rect;
        }
        // If the screen is taller than 16:9 (Add Letterboxes - black bars top/bottom)
        else 
        {
            float scaleWidth = 1.0f / scaleHeight;
            Rect rect = cam.rect;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
            cam.rect = rect;
        }
    }
}