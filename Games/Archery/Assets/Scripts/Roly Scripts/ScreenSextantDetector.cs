using UnityEngine;
using UnityEngine.Events;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ScreenSextantDetector : MonoBehaviour
{
    [Header("Debug Options")]
    // Enables or disables the grid drawing in the Scene view
    public bool showGrid = true;

    [Header("Events per Sextant (1–6)")]
    // An array of UnityEvents, one for each sextant (1-based indexing)
    public UnityEvent[] sextantEvents = new UnityEvent[6];

    // General-purpose event triggered on any sextant touch
    public event Action<int> OnSextantTouched;

    void Update()
    {
        Vector2 inputPosition = Vector2.zero;
        bool inputDetected = false;

        // Handle touch input
        if (Input.touchCount > 0)
        {
            inputPosition = Input.GetTouch(0).position;
            inputDetected = true;
        }
        // Handle mouse click input
        else if (Input.GetMouseButtonDown(0))
        {
            inputPosition = Input.mousePosition;
            inputDetected = true;
        }

        if (inputDetected)
        {
            int sextant = GetSextant(inputPosition);
            Debug.Log($"Touched sextant: {sextant}");

            // Trigger general event
            OnSextantTouched?.Invoke(sextant);

            // Trigger specific UnityEvent assigned to that sextant
            if (sextant >= 1 && sextant <= 6 && sextantEvents.Length >= sextant)
            {
                sextantEvents[sextant - 1]?.Invoke();
            }
        }
    }

    /// <summary>
    /// Converts a screen position to a sextant number (1 to 6)
    /// </summary>
    int GetSextant(Vector2 position)
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        float cellWidth = screenWidth / 2f;
        float cellHeight = screenHeight / 3f;

        // Calculate column and row index based on screen position
        int column = Mathf.FloorToInt(position.x / cellWidth);
        int row = Mathf.FloorToInt((screenHeight - position.y) / cellHeight); // Y is inverted

        column = Mathf.Clamp(column, 0, 1);
        row = Mathf.Clamp(row, 0, 2);

        // Return sextant index from 1 to 6
        return row * 2 + column + 1;
    }

    /// <summary>
    /// Draws the grid in the Scene view using Gizmos (editor only)
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showGrid || Camera.main == null) return;

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        float cellWidth = screenWidth / 2f;
        float cellHeight = screenHeight / 3f;

        Gizmos.color = Color.green;

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 2; col++)
            {
                int sextant = row * 2 + col + 1;

                // Calculate world space corners
                Vector3 topLeft = Camera.main.ScreenToWorldPoint(new Vector3(col * cellWidth, screenHeight - row * cellHeight, Camera.main.nearClipPlane + 1));
                Vector3 bottomRight = Camera.main.ScreenToWorldPoint(new Vector3((col + 1) * cellWidth, screenHeight - (row + 1) * cellHeight, Camera.main.nearClipPlane + 1));

                Vector3 size = bottomRight - topLeft;
                Vector3 center = topLeft + size / 2f;

                // Draw grid cell
                Gizmos.DrawWireCube(center, new Vector3(size.x, size.y, 0.01f));

#if UNITY_EDITOR
                // Display sextant number in the Scene view
                Handles.Label(center, $"Sextant {sextant}");
#endif
            }
        }
    }

    /// <summary>
    /// Ensures the UnityEvent array always contains exactly 6 elements
    /// </summary>
    void OnValidate()
    {
        if (sextantEvents == null || sextantEvents.Length != 6)
        {
            sextantEvents = new UnityEvent[6];
            for (int i = 0; i < 6; i++)
            {
                if (sextantEvents[i] == null)
                    sextantEvents[i] = new UnityEvent();
            }
        }
    }
}
