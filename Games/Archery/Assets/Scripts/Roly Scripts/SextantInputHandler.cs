using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SextantInputHandler : MonoBehaviour
{
    #region Constantes

    private const int ALLOWED_SEXTANT = 2;

    #endregion

    #region Referencias SerializedField Privadas

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

    #endregion

    #region Variables Privadas

    private ScreenSextantDetector _sextantDetector;

    private bool _isDragging = false;
    private bool _isFrozen = false;

    private Vector3 _dragStartWorldPos;
    private Vector3 _frozenEndPoint;

    private float _imageTopScreenY;

    private Vector2 _imageScreenMin;
    private Vector2 _imageScreenMax;

    #endregion

    #region Propiedades Públicas

    public float LastDragLength { get; private set; } = 0f;
    public float LastExtensionLength { get; private set; } = 0f;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        _sextantDetector = GetComponent<ScreenSextantDetector>();

        _lineRenderer.positionCount = 2;
        _lineRenderer.enabled = false;

        _extensionLineRenderer.positionCount = 2;
        _extensionLineRenderer.enabled = false;
    }

    private void Start()
    {
        if (_sextantDetector != null && _anchorCanvas != null && _anchorContainer != null)
        {
            _sextantDetector.AlignToSextant(6, _anchorCanvas, _anchorContainer);
        }

        CalculateImageScreenArea();
    }

    private void OnEnable()
    {
        if (_sextantDetector != null)
        {
            _sextantDetector.OnSextantTouched += HandleTouchDetected;
            _sextantDetector.OnSextantDragged += HandleDragDetected;
        }
    }

    private void OnDisable()
    {
        if (_sextantDetector != null)
        {
            _sextantDetector.OnSextantTouched -= HandleTouchDetected;
            _sextantDetector.OnSextantDragged -= HandleDragDetected;
        }

        StopDrag();
    }

    private void Update()
    {
        if (_isDragging)
        {
            Vector3 currentWorldPos = GetInputWorldPosition();

            // Clamp screen position of the input inside the vertical area
            Vector2 currentScreenPos = Input.touchCount > 0 ? (Vector2)Input.GetTouch(0).position : (Vector2)Input.mousePosition;

            // Clamp X between image left and right edges
            float clampedX = Mathf.Clamp(currentScreenPos.x, _imageScreenMin.x, _imageScreenMax.x);
            // Clamp Y between image top and screen top
            float clampedY = Mathf.Clamp(currentScreenPos.y, _imageTopScreenY, Screen.height);

            Vector3 clampedScreenPos = new Vector3(clampedX, clampedY, Camera.main.nearClipPlane + 1);
            Vector3 clampedWorldPos = Camera.main.ScreenToWorldPoint(clampedScreenPos);

            float inputScreenY = currentScreenPos.y;

            if (!_isFrozen)
            {
                if (inputScreenY >= _imageTopScreenY)
                {
                    UpdateLine(_dragStartWorldPos, clampedWorldPos);
                    LastDragLength = Vector3.Distance(_dragStartWorldPos, clampedWorldPos);
                    _lastDragLengthSerialized = LastDragLength;
                    _extensionLineRenderer.enabled = false;
                }
                else
                {
                    _isFrozen = true;

                    _frozenEndPoint = CalculateIntersectionPoint(_dragStartWorldPos, clampedWorldPos);

                    UpdateLine(_dragStartWorldPos, _frozenEndPoint);

                    _extensionLineRenderer.enabled = true;
                    _extensionLineRenderer.SetPosition(0, _frozenEndPoint);
                    _extensionLineRenderer.SetPosition(1, clampedWorldPos);
                }
            }
            else
            {
                if (inputScreenY >= _imageTopScreenY)
                {
                    _isFrozen = false;
                    _extensionLineRenderer.enabled = false;
                    UpdateLine(_dragStartWorldPos, clampedWorldPos);
                    LastDragLength = Vector3.Distance(_dragStartWorldPos, clampedWorldPos);
                }
                else
                {
                    // Clamp again for extension line inside the image rect
                    Vector2 clampedExtPos = new Vector2(
                        Mathf.Clamp(currentScreenPos.x, _imageScreenMin.x, _imageScreenMax.x),
                        Mathf.Clamp(currentScreenPos.y, _imageScreenMin.y, _imageScreenMax.y)
                    );
                    Vector3 clampedExtWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(clampedExtPos.x, clampedExtPos.y, Camera.main.nearClipPlane + 1));

                    _extensionLineRenderer.SetPosition(0, _frozenEndPoint);
                    _extensionLineRenderer.SetPosition(1, clampedExtWorldPos);

                    LastExtensionLength = Vector3.Distance(_frozenEndPoint, clampedWorldPos);
                    _lastExtensionLengthSerialized = LastExtensionLength;
                }
            }

            if (Input.touchCount == 0 && !Input.GetMouseButton(0))
            {
                StopDrag();
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || _anchorImage == null || _anchorContainer == null || Camera.main == null)
            return;

        if (_imageTopScreenY <= 0f)
            return;

        Vector3 leftScreenPoint = new Vector3(0, _imageTopScreenY, Camera.main.nearClipPlane + 1);
        Vector3 rightScreenPoint = new Vector3(Screen.width, _imageTopScreenY, Camera.main.nearClipPlane + 1);

        Vector3 leftWorldPoint = Camera.main.ScreenToWorldPoint(leftScreenPoint);
        Vector3 rightWorldPoint = Camera.main.ScreenToWorldPoint(rightScreenPoint);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(leftWorldPoint, rightWorldPoint);

        Vector3 bl = Camera.main.ScreenToWorldPoint(new Vector3(_imageScreenMin.x, _imageScreenMin.y, Camera.main.nearClipPlane + 1));
        Vector3 br = Camera.main.ScreenToWorldPoint(new Vector3(_imageScreenMax.x, _imageScreenMin.y, Camera.main.nearClipPlane + 1));
        Vector3 tr = Camera.main.ScreenToWorldPoint(new Vector3(_imageScreenMax.x, _imageScreenMax.y, Camera.main.nearClipPlane + 1));
        Vector3 tl = Camera.main.ScreenToWorldPoint(new Vector3(_imageScreenMin.x, _imageScreenMax.y, Camera.main.nearClipPlane + 1));

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);

        float screenTopY = Screen.height;
        Vector3 tlTopWorld = Camera.main.ScreenToWorldPoint(new Vector3(_imageScreenMin.x, screenTopY, Camera.main.nearClipPlane + 1));
        Vector3 trTopWorld = Camera.main.ScreenToWorldPoint(new Vector3(_imageScreenMax.x, screenTopY, Camera.main.nearClipPlane + 1));

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(tl, tlTopWorld);
        Gizmos.DrawLine(tr, trTopWorld);

        if (_isDragging && _lineRenderer.enabled && _lineRenderer.positionCount >= 2)
        {
            Vector3 startPos = _lineRenderer.GetPosition(0);
            Vector3 endPos = _lineRenderer.GetPosition(1);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(startPos, endPos);

            Vector3 midPoint = (startPos + endPos) * 0.5f;
            Handles.Label(midPoint + Vector3.up * 0.1f, $"Length: {LastDragLength:F2}");
        }

        if (_extensionLineRenderer.enabled && _extensionLineRenderer.positionCount >= 2)
        {
            Vector3 extStart = _extensionLineRenderer.GetPosition(0);
            Vector3 extEnd = _extensionLineRenderer.GetPosition(1);
            Vector3 extMidPoint = (extStart + extEnd) * 0.5f;
            Handles.Label(extMidPoint + Vector3.up * 0.1f, $"Ext Length: {LastExtensionLength:F2}");
        }
    }
#endif

    #endregion

    #region Métodos Privados

    private void CalculateImageScreenArea()
    {
        if (_anchorImage == null) return;

        Vector3[] corners = new Vector3[4];
        _anchorImage.rectTransform.GetWorldCorners(corners);

        Vector3 bottomLeftScreen = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        Vector3 topRightScreen = RectTransformUtility.WorldToScreenPoint(null, corners[2]);

        _imageScreenMin = new Vector2(bottomLeftScreen.x, bottomLeftScreen.y);
        _imageScreenMax = new Vector2(topRightScreen.x, topRightScreen.y);

        _imageTopScreenY = topRightScreen.y;
    }

    private Vector3 GetInputWorldPosition()
    {
        Vector3 inputPos = Input.touchCount > 0 ? (Vector3)Input.GetTouch(0).position : Input.mousePosition;

        Camera cam = Camera.main;
        return cam != null ? cam.ScreenToWorldPoint(new Vector3(inputPos.x, inputPos.y, cam.nearClipPlane + 1)) : Vector3.zero;
    }

    private void UpdateLine(Vector3 start, Vector3 end)
    {
        _lineRenderer.SetPosition(0, start);
        _lineRenderer.SetPosition(1, end);
    }

    private float CalculateLineLength(LineRenderer lr)
    {
        if (lr.positionCount >= 2)
            return Vector3.Distance(lr.GetPosition(0), lr.GetPosition(1));
        return 0f;
    }

    private Vector3 CalculateIntersectionPoint(Vector3 start, Vector3 end)
    {
        float yWorld = Camera.main.ScreenToWorldPoint(new Vector3(0, _imageTopScreenY, Camera.main.nearClipPlane + 1)).y;

        Vector3 direction = end - start;

        if (Mathf.Approximately(direction.y, 0f))
            return start;

        float t = (yWorld - start.y) / direction.y;
        t = Mathf.Clamp01(t);

        return start + direction * t;
    }

    private void HandleTouchDetected(int sextant)
    {
        if (sextant == ALLOWED_SEXTANT)
        {
            _dragStartWorldPos = GetInputWorldPosition();

            _isFrozen = false;
            _isDragging = true;
            _lineRenderer.enabled = true;
            _lineRenderer.SetPosition(0, _dragStartWorldPos);
            _lineRenderer.SetPosition(1, _dragStartWorldPos);
            LastDragLength = 0f;
            LastExtensionLength = 0f;
            _lastExtensionLengthSerialized = 0f;
        }
    }

    private void HandleDragDetected(int sextant)
    {
        // Placeholder for extra logic
    }

    private void StopDrag()
    {
        _isDragging = false;
        _isFrozen = false;
        _lineRenderer.enabled = false;
        _extensionLineRenderer.enabled = false;
    }

    #endregion
}