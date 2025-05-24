using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(LineRenderer))]
public class SextantInputHandler : MonoBehaviour
{
    public ScreenSextantDetector sextantDetector;

    public List<SextantDragRule> dragRules = new List<SextantDragRule>();
    private Dictionary<int, List<int>> allowedDragPaths = new Dictionary<int, List<int>>();

    [Header("Enable Drag per Sextant")]
    public bool allowDragInSextant1 = true;
    public bool allowDragInSextant2 = true;
    public bool allowDragInSextant3 = true;
    public bool allowDragInSextant4 = true;
    public bool allowDragInSextant5 = true;
    public bool allowDragInSextant6 = true;

    private bool freezeLength = false;

    // Last recorded drag length (in world units)
    public float LastDragLength { get; private set; } = 0f;

    private Vector3 dragStartWorldPos;
    private bool isDragging = false;
    private int activeSextant = -1;

    private LineRenderer lineRenderer;

    public int currentSextant;

    void Awake()
    {
        sextantDetector = GetComponent<ScreenSextantDetector>();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;

        allowedDragPaths.Clear();
        foreach (var rule in dragRules)
        {
            if (!allowedDragPaths.ContainsKey(rule.sextant))
            {
                allowedDragPaths[rule.sextant] = new List<int>(rule.allowedSextants);
            }
        }
    }

    void OnEnable()
    {
        if (sextantDetector != null)
        {
            sextantDetector.OnSextantTouched += HandleTouchDetected;
            sextantDetector.OnSextantDragged += HandleDragDetected;
        }
    }

    void OnDisable()
    {
        if (sextantDetector != null)
        {
            sextantDetector.OnSextantTouched -= HandleTouchDetected;
            sextantDetector.OnSextantDragged -= HandleDragDetected;
        }

        StopDrag();
    }

    void Update()
    {
        if (isDragging)
        {
            Vector3 currentWorldPos = GetInputWorldPosition();
            UpdateLine(dragStartWorldPos, currentWorldPos);

            int currentSextant = sextantDetector.GetSextantAtPosition(Input.mousePosition);

            // Check if we're inside sextant 5 or 6
            if (currentSextant == 5 || currentSextant == 6)
            {
                if (!freezeLength)
                {
                    // First time entering 5 or 6, freeze
                    freezeLength = true;
                    Debug.Log($"Entered Sextant {currentSextant}, freezing length at: {LastDragLength:F2}");
                }
            }
            else
            {
                if (freezeLength)
                {
                    // Just exited 5 or 6, resume
                    freezeLength = false;
                    Debug.Log($"Exited Sextant 5/6, resuming length measurement.");
                }
            }

            // Only update length if not frozen
            if (!freezeLength)
            {
                LastDragLength = Vector3.Distance(dragStartWorldPos, currentWorldPos);
                Debug.Log($"Current Drag Length: {LastDragLength:F2}");
            }

            // Stop drag when input ends
            if (Input.touchCount == 0 && !Input.GetMouseButton(0))
            {
                StopDrag();
            }
        }
    }

    void HandleTouchDetected(int sextant)
    {
        Debug.Log("Touch detected");

        activeSextant = sextant;
        dragStartWorldPos = GetInputWorldPosition();

        if (IsDragAllowed(sextant))
        {
            StartDrag();
        }
        else
        {
            Debug.Log($"Drag is DISABLED in Sextant {sextant}");
        }
    }

    void HandleDragDetected(int sextant)
    {
        // No extra logic needed here for now
    }

    void StartDrag()
    {
        isDragging = true;
        freezeLength = false;  // Reset freeze flag at start of drag
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, dragStartWorldPos);
        lineRenderer.SetPosition(1, dragStartWorldPos);
        LastDragLength = 0f;

        switch (activeSextant)
        {
            case 1: 
                HandleSextant1DragStart(); 
                break;

            case 2: 
                HandleSextant2DragStart(); 
                break;

            case 3: 
                HandleSextant3DragStart(); 
                break;

            case 4: 
                HandleSextant4DragStart(); 
                break;
            case 5: 
                HandleSextant5DragStart(); 
                break;

            case 6: 
                HandleSextant6DragStart(); 
                break;
        }
    }

    void StopDrag()
    {
        isDragging = false;
        freezeLength = false;  // Reset freeze for next drag
        lineRenderer.enabled = false;
        activeSextant = -1;
    }

    void UpdateLine(Vector3 start, Vector3 end)
    {
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    Vector3 GetInputWorldPosition()
    {
        Vector3 inputPos = Input.touchCount > 0
            ? (Vector3)Input.GetTouch(0).position
            : Input.mousePosition;

        Camera cam = Camera.main;
        return cam != null
            ? cam.ScreenToWorldPoint(new Vector3(inputPos.x, inputPos.y, cam.nearClipPlane + 1))
            : Vector3.zero;
    }

    bool IsDragAllowed(int sextant)
    {
        return sextant switch
        {
            1 => allowDragInSextant1,
            2 => allowDragInSextant2,
            3 => allowDragInSextant3,
            4 => allowDragInSextant4,
            5 => allowDragInSextant5,
            6 => allowDragInSextant6,
            _ => false
        };
    }

    // Example methods for per-sextant logic
    void HandleSextant1DragStart() => Debug.Log("Custom logic for Sextant 1");
    void HandleSextant2DragStart() => Debug.Log("Custom logic for Sextant 2");
    void HandleSextant3DragStart() => Debug.Log("Custom logic for Sextant 3");
    void HandleSextant4DragStart() => Debug.Log("Custom logic for Sextant 4");
    void HandleSextant5DragStart() => Debug.Log("Custom logic for Sextant 5");
    void HandleSextant6DragStart() => Debug.Log("Custom logic for Sextant 6");

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (isDragging && lineRenderer.enabled && lineRenderer.positionCount >= 2)
        {
            // Get the world positions of the line
            Vector3 startPos = lineRenderer.GetPosition(0);
            Vector3 endPos = lineRenderer.GetPosition(1);

            // Compute the midpoint
            Vector3 midPoint = (startPos + endPos) * 0.5f;

            // Optional: lift slightly for better readability
            Vector3 labelPos = midPoint + Vector3.up * 0.1f;

            // Draw label using Handles (only works in editor)
            UnityEditor.Handles.Label(labelPos, $"Length: {LastDragLength:F2}");
        }
    }
#endif
}
