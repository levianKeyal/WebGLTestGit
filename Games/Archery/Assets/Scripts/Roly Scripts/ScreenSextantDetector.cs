using UnityEngine;
using UnityEngine.Events;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ScreenSextantDetector : MonoBehaviour
{
    [Header("Debug Options")]
    // Enable or disable drawing the grid lines in the Scene view
    public bool showGrid = true;

    [Header("Events: Touch Down per Sextant (1–6)")]
    // Array of UnityEvents invoked when a touch/click begins in each sextant
    public UnityEvent[] onTouchDownEvents = new UnityEvent[6];

    [Header("Events: Drag per Sextant (1–6)")]
    // Array of UnityEvents invoked when a drag is detected within the sextant where touch started
    public UnityEvent[] onDragEvents = new UnityEvent[6];

    // General-purpose C# event fired on touch down with the sextant number (1–6)
    public event Action<int> OnSextantTouched;

    // General-purpose C# event fired on drag with the sextant number (1–6)
    public event Action<int> OnSextantDragged;

    // Tracks whether a drag is currently active
    private bool isDragging = false;

    // Stores the sextant number where the drag originally started; drag events only fire if still inside this sextant
    private int activeDragSextant = -1;

    void Update()
    {
        Vector2 inputPosition = Vector2.zero;

        // Handle touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            inputPosition = touch.position;

            int sextant = GetSextant(inputPosition);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    // Touch started: trigger touch down event and mark drag as active in this sextant
                    HandleTouchDown(sextant);
                    isDragging = true;
                    activeDragSextant = sextant;
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    // Touch moved or held: trigger drag event only if still in original sextant
                    HandleDrag(sextant);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    // Touch ended or canceled: reset drag tracking
                    isDragging = false;
                    activeDragSextant = -1;
                    break;
            }
        }
        // Handle mouse input (for testing in editor or standalone)
        else if (Input.GetMouseButtonDown(0))
        {
            inputPosition = Input.mousePosition;
            int sextant = GetSextant(inputPosition);
            // Mouse button pressed: trigger touch down and start drag tracking
            HandleTouchDown(sextant);
            isDragging = true;
            activeDragSextant = sextant;
        }
        else if (Input.GetMouseButton(0))
        {
            inputPosition = Input.mousePosition;
            int sextant = GetSextant(inputPosition);
            // Mouse held down: trigger drag event only if still in original sextant
            HandleDrag(sextant);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            // Mouse button released: reset drag tracking
            isDragging = false;
            activeDragSextant = -1;
        }
    }

    // Called when a touch or click begins on a sextant
    void HandleTouchDown(int sextant)
    {
        Debug.Log($"[TouchDown] Sextant {sextant}");

        // Invoke the general C# event for touch down
        OnSextantTouched?.Invoke(sextant);

        // Invoke the specific UnityEvent assigned for this sextant's touch down
        if (sextant >= 1 && sextant <= 6 && onTouchDownEvents.Length >= sextant)
            onTouchDownEvents[sextant - 1]?.Invoke();
    }

    // Called during dragging within the sextant where drag started
    void HandleDrag(int sextant)
    {
        // Only trigger drag events if dragging and still inside the sextant where drag began
        if (!isDragging || sextant != activeDragSextant) return;

        //Debug.Log($"[Drag] Sextant {sextant}");

        // Invoke the general C# event for dragging
        OnSextantDragged?.Invoke(sextant);

        // Invoke the specific UnityEvent assigned for this sextant's drag
        if (sextant >= 1 && sextant <= 6 && onDragEvents.Length >= sextant)
            onDragEvents[sextant - 1]?.Invoke();
    }

    // Determines the sextant (1 to 6) given a screen position
    int GetSextant(Vector2 position)
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // Each sextant is half the screen width and one-third of the screen height
        float cellWidth = screenWidth / 2f;
        float cellHeight = screenHeight / 3f;

        // Calculate which column (0 or 1) and row (0 to 2) the position falls into
        int column = Mathf.FloorToInt(position.x / cellWidth);
        int row = Mathf.FloorToInt((screenHeight - position.y) / cellHeight); // Y axis inverted in screen space

        column = Mathf.Clamp(column, 0, 1);
        row = Mathf.Clamp(row, 0, 2);

        // Return sextant index from 1 to 6 (row major order, left to right)
        return row * 2 + column + 1;
    }

    // Draws the sextant grid in the Scene view using Gizmos for debugging
    void OnDrawGizmos()
    {
        if (!showGrid || Camera.main == null) return;

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        float cellWidth = screenWidth / 2f;
        float cellHeight = screenHeight / 3f;

        Gizmos.color = Color.green;

        // Iterate over rows and columns to draw each sextant's outline
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 2; col++)
            {
                int sextant = row * 2 + col + 1;

                // Convert screen coordinates to world points at near clipping plane + 1 unit
                Vector3 topLeft = Camera.main.ScreenToWorldPoint(new Vector3(col * cellWidth, screenHeight - row * cellHeight, Camera.main.nearClipPlane + 1));
                Vector3 bottomRight = Camera.main.ScreenToWorldPoint(new Vector3((col + 1) * cellWidth, screenHeight - (row + 1) * cellHeight, Camera.main.nearClipPlane + 1));

                Vector3 size = bottomRight - topLeft;
                Vector3 center = topLeft + size / 2f;

                // Draw wireframe cube representing the sextant boundaries
                Gizmos.DrawWireCube(center, new Vector3(size.x, size.y, 0.01f));

#if UNITY_EDITOR
                // Display the sextant number label in the Scene view
                Handles.Label(center, $"Sextant {sextant}");
#endif
            }
        }
    }

    // Ensure that UnityEvent arrays always contain exactly 6 elements for editor safety
    void OnValidate()
    {
        EnsureArrayLength(ref onTouchDownEvents);
        EnsureArrayLength(ref onDragEvents);
    }

    // Helper to ensure UnityEvent arrays have 6 initialized elements
    void EnsureArrayLength(ref UnityEvent[] array)
    {
        if (array == null || array.Length != 6)
        {
            UnityEvent[] newArray = new UnityEvent[6];
            for (int i = 0; i < 6; i++)
            {
                newArray[i] = (array != null && i < array.Length && array[i] != null) ? array[i] : new UnityEvent();
            }
            array = newArray;
        }
    }
    public int GetSextantAtPosition(Vector2 screenPosition)
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // Calculate horizontal divisions
        float halfWidth = screenWidth / 2f;
        float sectionHeight = screenHeight / 3f;

        int column = screenPosition.x < halfWidth ? 0 : 1;  // 0 = left, 1 = right

        int row;
        if (screenPosition.y >= sectionHeight * 2)
            row = 0;  // top third
        else if (screenPosition.y >= sectionHeight)
            row = 1;  // middle third
        else
            row = 2;  // bottom third

        // Map to sextant number
        int sextantIndex = row * 2 + column + 1;

        return sextantIndex;  // Returns 1–6
    }
    /// <summary>
    /// Align the container to the center of the specified sextant (1–6).
    /// </summary>
    /// <param name="sextantNumber">Sextant number: 1 (top-left), 2 (top-right), 3 (middle-left), 4 (middle-right), 5 (bottom-left), 6 (bottom-right)</param>
    public void AlignToSextant(int sextantNumber, Canvas anchorCanvas, RectTransform anchorContainer)
    {
        if (sextantNumber < 1 || sextantNumber > 6)
        {
            Debug.LogWarning("Invalid sextant number. Must be between 1 and 6.");
            return;
        }

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // Determine row (top, middle, bottom)
        int row = (sextantNumber - 1) / 2;  // 0: top, 1: middle, 2: bottom
        // Determine column (left or right)
        int col = (sextantNumber - 1) % 2;  // 0: left, 1: right

        // Calculate X position (center of left or right half)
        float centerX = (col == 0) ? screenWidth / 4f : 3f * screenWidth / 4f;

        // Calculate Y position (center of top, middle, or bottom third)
        float centerY;
        if (row == 0)         // top
            centerY = 5f * screenHeight / 6f;
        else if (row == 1)    // middle
            centerY = 3f * screenHeight / 6f;
        else                  // bottom
            centerY = screenHeight / 6f;

        Vector2 sextantScreenPos = new Vector2(centerX, centerY);

        // Convert screen point to local position in the _anchorCanvas
        RectTransform canvasRect = anchorCanvas.GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, sextantScreenPos,
            anchorCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : anchorCanvas.worldCamera,
            out localPoint
        );

        // Set the container's anchoredPosition
        anchorContainer.anchoredPosition = localPoint;
    }
}
