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

    #endregion

    #region Variables Privadas

    private ScreenSextantDetector _sextantDetector;

    private bool _isDragging = false;
    private bool _isFrozen = false;

    private Vector3 _dragStartWorldPos;
    private Vector3 _frozenEndPoint;

    private float _imageTopScreenY;

    #endregion

    #region Propiedades Públicas

    public float LastDragLength { get; private set; } = 0f;

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

        CalculateImageTopScreenY();
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

            float inputScreenY = Input.touchCount > 0 ? Input.GetTouch(0).position.y : Input.mousePosition.y;

            if (!_isFrozen)
            {
                if (inputScreenY >= _imageTopScreenY)
                {
                    // Arriba del límite, actualización normal
                    UpdateLine(_dragStartWorldPos, currentWorldPos);
                    LastDragLength = Vector3.Distance(_dragStartWorldPos, currentWorldPos);
                    _extensionLineRenderer.enabled = false;
                }
                else
                {
                    // Cruzó hacia abajo, congelar línea principal, activar extensión
                    _isFrozen = true;

                    _frozenEndPoint = CalculateIntersectionPoint(_dragStartWorldPos, currentWorldPos);

                    UpdateLine(_dragStartWorldPos, _frozenEndPoint);

                    _extensionLineRenderer.enabled = true;
                    _extensionLineRenderer.SetPosition(0, _frozenEndPoint);
                    _extensionLineRenderer.SetPosition(1, currentWorldPos);
                }
            }
            else
            {
                if (inputScreenY >= _imageTopScreenY)
                {
                    // Regresó arriba, desactivar extensión, reactivar principal
                    _isFrozen = false;
                    _extensionLineRenderer.enabled = false;
                    UpdateLine(_dragStartWorldPos, currentWorldPos);
                    LastDragLength = Vector3.Distance(_dragStartWorldPos, currentWorldPos);
                }
                else
                {
                    // Sigue abajo, actualizar extensión
                    _extensionLineRenderer.SetPosition(0, _frozenEndPoint);
                    _extensionLineRenderer.SetPosition(1, currentWorldPos);
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

        if (_isDragging && _lineRenderer.enabled && _lineRenderer.positionCount >= 2)
        {
            Vector3 startPos = _lineRenderer.GetPosition(0);
            Vector3 endPos = _lineRenderer.GetPosition(1);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(startPos, endPos);

            Vector3 midPoint = (startPos + endPos) * 0.5f;
            Handles.Label(midPoint + Vector3.up * 0.1f, $"Length: {LastDragLength:F2}");
        }
    }
#endif

    #endregion

    #region Métodos Privados

    private void CalculateImageTopScreenY()
    {
        if (_anchorImage == null) return;

        Vector3[] corners = new Vector3[4];
        _anchorImage.rectTransform.GetWorldCorners(corners);

        Vector3 topLeftWorld = corners[1];
        Vector3 topLeftScreen = RectTransformUtility.WorldToScreenPoint(null, topLeftWorld);

        _imageTopScreenY = topLeftScreen.y;
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

            Vector3 startScreenPos = Input.touchCount > 0 ? (Vector3)Input.GetTouch(0).position : Input.mousePosition;
            _isFrozen = false;

            _isDragging = true;
            _lineRenderer.enabled = true;
            _lineRenderer.SetPosition(0, _dragStartWorldPos);
            _lineRenderer.SetPosition(1, _dragStartWorldPos);
            LastDragLength = 0f;
        }
    }

    private void HandleDragDetected(int sextant)
    {
        // Puede agregarse lógica adicional si se requiere
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
