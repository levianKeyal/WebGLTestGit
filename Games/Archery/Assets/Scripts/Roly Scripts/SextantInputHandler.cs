using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(ScreenSextantDetector))]
public class SextantInputHandler : MonoBehaviour
{
    private const int ALLOWED_SEXTANT = 2;

    [Header("UI Anchors for Image")]
    [SerializeField] 
    private Canvas _anchorCanvas;

    [SerializeField] 
    private RectTransform _anchorContainer;

    [SerializeField] 
    private Image _anchorImage;

    [Header("Line Renderers")]
    [SerializeField] 
    private LineRenderer _lineRenderer;

    [SerializeField] 
    private LineRenderer _extensionLineRenderer;

    [Header("Debug Values")]
    [SerializeField, ReadOnly] 
    private float _lastDragLengthSerialized;

    [SerializeField, ReadOnly] 
    private float _lastExtensionLengthSerialized;

    public float LastDragLength { get; private set; }
    public float LastExtensionLength { get; private set; }

    private ScreenSextantDetector _sextantDetector;

    private bool _isDragging, _isFrozen;
    private Vector3 _dragStartWorldPos, _frozenEndPoint;

    private float _imageTopScreenY;
    private Vector2 _imageScreenMin, _imageScreenMax;

    private Camera _mainCamera;

    private void Awake()
    {
        _sextantDetector = GetComponent<ScreenSextantDetector>();
        _mainCamera = Camera.main;

        InitializeLineRenderer(_lineRenderer);
        InitializeLineRenderer(_extensionLineRenderer);
    }

    private void Start()
    {
        if (_sextantDetector && _anchorCanvas && _anchorContainer)
            _sextantDetector.AlignToSextant(6, _anchorCanvas, _anchorContainer);

        CalculateImageScreenArea();
    }

    private void OnEnable()
    {
        _sextantDetector.OnSextantTouched += HandleTouchDetected;
        _sextantDetector.OnSextantDragged += HandleDragDetected;
    }

    private void OnDisable()
    {
        _sextantDetector.OnSextantTouched -= HandleTouchDetected;
        _sextantDetector.OnSextantDragged -= HandleDragDetected;
        StopDrag();
    }

    private void Update()
    {
        if (!_isDragging) return;

        Vector2 currentScreenPos = Input.touchCount > 0 ? (Vector2)Input.GetTouch(0).position : (Vector2)Input.mousePosition;

        float clampedX = Mathf.Clamp(currentScreenPos.x, _imageScreenMin.x, _imageScreenMax.x);
        float clampedY = Mathf.Clamp(currentScreenPos.y, _imageTopScreenY, Screen.height);
        Vector3 clampedWorldPos = ScreenToWorld(new Vector3(clampedX, clampedY, 1));

        if (!_isFrozen)
        {
            if (currentScreenPos.y >= _imageTopScreenY)
            {
                UpdateMainLine(clampedWorldPos);
            }
            else
            {
                FreezeLine(clampedWorldPos);
            }
        }
        else
        {
            if (currentScreenPos.y >= _imageTopScreenY)
            {
                UnfreezeLine(clampedWorldPos);
            }
            else
            {
                UpdateExtensionLine(currentScreenPos, clampedWorldPos);
            }
        }

        if (Input.touchCount == 0 && !Input.GetMouseButton(0))
            StopDrag();
    }

    private void InitializeLineRenderer(LineRenderer lr)
    {
        lr.positionCount = 2;
        lr.enabled = false;
    }

    private void CalculateImageScreenArea()
    {
        if (!_anchorImage) return;

        Vector3[] corners = new Vector3[4];
        _anchorImage.rectTransform.GetWorldCorners(corners);

        Vector3 bottomLeftScreen = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        Vector3 topRightScreen = RectTransformUtility.WorldToScreenPoint(null, corners[2]);

        _imageScreenMin = new Vector2(bottomLeftScreen.x, bottomLeftScreen.y);
        _imageScreenMax = new Vector2(topRightScreen.x, topRightScreen.y);
        _imageTopScreenY = topRightScreen.y;
    }

    private Vector3 ScreenToWorld(Vector3 screenPos)
    {
        return _mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, _mainCamera.nearClipPlane + 1));
    }

    private void UpdateMainLine(Vector3 endPoint)
    {
        UpdateLine(_lineRenderer, _dragStartWorldPos, endPoint);
        LastDragLength = Vector3.Distance(_dragStartWorldPos, endPoint);
        _lastDragLengthSerialized = LastDragLength;
        _extensionLineRenderer.enabled = false;
    }

    private void FreezeLine(Vector3 currentWorldPos)
    {
        _isFrozen = true;
        _frozenEndPoint = CalculateIntersectionPoint(_dragStartWorldPos, currentWorldPos);
        UpdateLine(_lineRenderer, _dragStartWorldPos, _frozenEndPoint);
        _extensionLineRenderer.enabled = true;
        _extensionLineRenderer.SetPosition(0, _frozenEndPoint);
        _extensionLineRenderer.SetPosition(1, currentWorldPos);
    }

    private void UnfreezeLine(Vector3 currentWorldPos)
    {
        _isFrozen = false;
        _extensionLineRenderer.enabled = false;
        UpdateMainLine(currentWorldPos);
    }

    private void UpdateExtensionLine(Vector2 currentScreenPos, Vector3 clampedWorldPos)
    {
        Vector2 clampedExtPos = new Vector2(
            Mathf.Clamp(currentScreenPos.x, _imageScreenMin.x, _imageScreenMax.x),
            Mathf.Clamp(currentScreenPos.y, _imageScreenMin.y, _imageScreenMax.y)
        );
        Vector3 clampedExtWorldPos = ScreenToWorld(new Vector3(clampedExtPos.x, clampedExtPos.y, 1));

        _extensionLineRenderer.SetPosition(0, _frozenEndPoint);
        _extensionLineRenderer.SetPosition(1, clampedExtWorldPos);

        LastExtensionLength = Vector3.Distance(_frozenEndPoint, clampedWorldPos);
        _lastExtensionLengthSerialized = LastExtensionLength;
    }

    private Vector3 CalculateIntersectionPoint(Vector3 start, Vector3 end)
    {
        float yWorld = ScreenToWorld(new Vector3(0, _imageTopScreenY, 1)).y;
        Vector3 direction = end - start;

        if (Mathf.Approximately(direction.y, 0f))
            return start;

        float t = Mathf.Clamp01((yWorld - start.y) / direction.y);
        return start + direction * t;
    }

    private void UpdateLine(LineRenderer lr, Vector3 start, Vector3 end)
    {
        lr.enabled = true;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }

    private void HandleTouchDetected(int sextant)
    {
        if (sextant != ALLOWED_SEXTANT) return;

        _dragStartWorldPos = ScreenToWorld(Input.touchCount > 0 ? (Vector3)Input.GetTouch(0).position : Input.mousePosition);
        _isDragging = true;
        _isFrozen = false;

        UpdateLine(_lineRenderer, _dragStartWorldPos, _dragStartWorldPos);
        LastDragLength = LastExtensionLength = _lastExtensionLengthSerialized = 0f;
    }

    private void HandleDragDetected(int sextant)
    {
        // Placeholder for extra drag logic if needed
    }

    private void StopDrag()
    {
        _isDragging = _isFrozen = false;
        _lineRenderer.enabled = false;
        _extensionLineRenderer.enabled = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !_anchorImage || !_mainCamera || _imageTopScreenY <= 0f)
            return;

        DrawHorizontalLine(_imageTopScreenY, Color.red);
        DrawRectOutline(_imageScreenMin, _imageScreenMax, Color.yellow);
        DrawVerticalBoundaries(Color.cyan);

        if (_isDragging && _lineRenderer.enabled)
            DrawLabelForLine(_lineRenderer, $"Length: {LastDragLength:F2}", Color.green);

        if (_extensionLineRenderer.enabled)
            DrawLabelForLine(_extensionLineRenderer, $"Ext Length: {LastExtensionLength:F2}", Color.green);
    }

    private void DrawHorizontalLine(float y, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(ScreenToWorld(new Vector3(0, y, 1)), ScreenToWorld(new Vector3(Screen.width, y, 1)));
    }

    private void DrawRectOutline(Vector2 min, Vector2 max, Color color)
    {
        Vector3 bl = ScreenToWorld(new Vector3(min.x, min.y, 1));
        Vector3 br = ScreenToWorld(new Vector3(max.x, min.y, 1));
        Vector3 tr = ScreenToWorld(new Vector3(max.x, max.y, 1));
        Vector3 tl = ScreenToWorld(new Vector3(min.x, max.y, 1));

        Gizmos.color = color;
        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
    }

    private void DrawVerticalBoundaries(Color color)
    {
        Vector3 tl = ScreenToWorld(new Vector3(_imageScreenMin.x, _imageScreenMax.y, 1));
        Vector3 tr = ScreenToWorld(new Vector3(_imageScreenMax.x, _imageScreenMax.y, 1));
        Vector3 tlTop = ScreenToWorld(new Vector3(_imageScreenMin.x, Screen.height, 1));
        Vector3 trTop = ScreenToWorld(new Vector3(_imageScreenMax.x, Screen.height, 1));

        Gizmos.color = color;
        Gizmos.DrawLine(tl, tlTop);
        Gizmos.DrawLine(tr, trTop);
    }

    private void DrawLabelForLine(LineRenderer lr, string label, Color color)
    {
        if (lr.positionCount < 2) return;

        Vector3 midPoint = (lr.GetPosition(0) + lr.GetPosition(1)) * 0.5f;
        Gizmos.color = color;
        Gizmos.DrawLine(lr.GetPosition(0), lr.GetPosition(1));
        Handles.Label(midPoint + Vector3.up * 0.1f, label);
    }
#endif
}
