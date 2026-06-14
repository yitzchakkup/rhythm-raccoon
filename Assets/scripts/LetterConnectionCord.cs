using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LetterConnectionCord : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private List<FallingLetter> connectedLetters;

    public void Setup(List<FallingLetter> letters)
    {
        lineRenderer = GetComponent<LineRenderer>();
        connectedLetters = letters;

        // Sort letters by X position so the line draws cleanly left-to-right
        // This prevents the line from crossing over itself in a knot!
        connectedLetters.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        // Tell the LineRenderer exactly how many points it needs to connect
        lineRenderer.positionCount = connectedLetters.Count;
    }

    void Update()
    {
        if (connectedLetters == null || connectedLetters.Count == 0) return;

        for (int i = 0; i < connectedLetters.Count; i++)
        {
            // If any letter in the cluster was popped or destroyed, the cord breaks!
            if (connectedLetters[i] == null)
            {
                Destroy(gameObject);
                return;
            }

            // Update the line's position to match the letter's current position
            Vector3 pointPosition = connectedLetters[i].transform.position;
            
            // Push the line slightly back on the Z-axis so it renders behind the letters
            pointPosition.z = 1f; 
            
            lineRenderer.SetPosition(i, pointPosition);
        }
    }
}