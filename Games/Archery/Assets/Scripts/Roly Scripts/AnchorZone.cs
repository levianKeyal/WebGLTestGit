using UnityEngine;

public class AnchorZone : MonoBehaviour
{
    public Canvas anchorCanvas;                // Reference to the anchorCanvas
    public RectTransform anchorContainer;  // Assign the container holding the image    

    /// <summary>
    /// Align the container to the center of the specified sextant (1–6).
    /// </summary>
    /// <param name="sextantNumber">Sextant number: 1 (top-left), 2 (top-right), 3 (middle-left), 4 (middle-right), 5 (bottom-left), 6 (bottom-right)</param>
    public void AlignToSextant(int sextantNumber)
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

        // Convert screen point to local position in the anchorCanvas
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

    void Start()
    {
        // Example: center the image in sextant 6 at start
        AlignToSextant(6);
    }
}
