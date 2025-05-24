using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SextantInputHandler : MonoBehaviour
{
    [Header("Dependencies")]
    public ScreenSextantDetector sextantDetector;

    [Header("UI Anchors for Image")]
    [SerializeField] private Canvas _anchorCanvas;
    [SerializeField] private RectTransform _anchorContainer;
    [SerializeField] private Image _anchorImage;

    [Header("Drag State")]
    public float LastDragLength { get; private set; } = 0f;

    [SerializeField]
    private LineRenderer lineRenderer;
    private bool isDragging = false;
    private Vector3 dragStartWorldPos;

    private const int AllowedSextant = 2;

    [SerializeField]
    private LineRenderer extensionLineRenderer;
    private Vector3 frozenEndPoint;
    private bool isFrozen = false;
    private int activeSextant = -1;

    private bool freezeLength = false;

    // Screen Y coordinate of the image's top border
    private float _imageTopScreenY;

    // To track if we've crossed the image top line
    private bool _isLengthFrozen = false;

    // Side of start drag relative to image top line: true if start below line, false if above
    private bool _startedBelowLine;

    private void Start()
    {
        if (sextantDetector != null && _anchorCanvas != null && _anchorContainer != null)
        {
            sextantDetector.AlignToSextant(6, _anchorCanvas, _anchorContainer);
        }

        CalculateImageTopScreenY();

        // Configura el extensionLineRenderer para que tenga 2 posiciones y esté oculto al inicio
        extensionLineRenderer.positionCount = 2;
        extensionLineRenderer.enabled = false;
    }

    private void CalculateImageTopScreenY()
    {
        if (_anchorImage == null) return;

        // Get world corners of the Image's RectTransform (4 corners: BL, TL, TR, BR)
        Vector3[] corners = new Vector3[4];
        _anchorImage.rectTransform.GetWorldCorners(corners);

        // Top left corner (index 1) world position
        Vector3 topLeftWorld = corners[1];

        // Convert to screen position
        Vector3 topLeftScreen = RectTransformUtility.WorldToScreenPoint(null, topLeftWorld);

        _imageTopScreenY = topLeftScreen.y;

        Debug.Log($"Image top border screen Y position: {_imageTopScreenY}");
    }

    void Awake()
    {
        sextantDetector = GetComponent<ScreenSextantDetector>();
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
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

            // Obtener la Y de input en pantalla
            float inputScreenY = Input.touchCount > 0 ? Input.GetTouch(0).position.y : Input.mousePosition.y;

            if (!isFrozen)
            {
                if (inputScreenY >= _imageTopScreenY)
                {
                    // Arriba del límite, actualización normal
                    UpdateLine(dragStartWorldPos, currentWorldPos);
                    LastDragLength = Vector3.Distance(dragStartWorldPos, currentWorldPos);
                    extensionLineRenderer.enabled = false;
                }
                else
                {
                    // Cruzó hacia abajo, congelar línea principal, activar extensión
                    isFrozen = true;

                    frozenEndPoint = CalculateIntersectionPoint(dragStartWorldPos, currentWorldPos);

                    UpdateLine(dragStartWorldPos, frozenEndPoint);

                    extensionLineRenderer.enabled = true;
                    extensionLineRenderer.SetPosition(0, frozenEndPoint);
                    extensionLineRenderer.SetPosition(1, currentWorldPos);
                }
            }
            else
            {
                if (inputScreenY >= _imageTopScreenY)
                {
                    // Regresó arriba, desactivar extensión, reactivar principal
                    isFrozen = false;
                    extensionLineRenderer.enabled = false;
                    UpdateLine(dragStartWorldPos, currentWorldPos);
                    LastDragLength = Vector3.Distance(dragStartWorldPos, currentWorldPos);
                }
                else
                {
                    // Sigue abajo, actualizar extensión
                    extensionLineRenderer.SetPosition(0, frozenEndPoint);
                    extensionLineRenderer.SetPosition(1, currentWorldPos);
                }
            }

            if (Input.touchCount == 0 && !Input.GetMouseButton(0))
            {
                StopDrag();
            }
        }
    }

    private Vector3 CalculateIntersectionPoint(Vector3 start, Vector3 end)
    {
        // Convertir el Y de la línea límite a world Y en el plano de la cámara (nearClipPlane + 1)
        float yWorld = Camera.main.ScreenToWorldPoint(new Vector3(0, _imageTopScreenY, Camera.main.nearClipPlane + 1)).y;

        // La línea es entre start y end, vamos a calcular la intersección en Y = yWorld
        Vector3 direction = end - start;

        if (Mathf.Approximately(direction.y, 0f))
        {
            // Línea horizontal, retorna start o end (no cruza)
            return start;
        }

        float t = (yWorld - start.y) / direction.y;
        t = Mathf.Clamp01(t); // para estar seguros que sea entre start y end

        return start + direction * t;
    }

    void HandleTouchDetected(int sextant)
    {
        Debug.Log($"Touch detected in sextant {sextant}");

        if (sextant == AllowedSextant)
        {
            dragStartWorldPos = GetInputWorldPosition();

            // Determine if drag started below or above the image top line
            Vector3 startScreenPos = Input.touchCount > 0
                ? (Vector3)Input.GetTouch(0).position
                : Input.mousePosition;
            _startedBelowLine = startScreenPos.y < _imageTopScreenY;

            _isLengthFrozen = false;

            StartDrag();
        }
        else
        {
            Debug.Log($"Drag is DISABLED in Sextant {sextant}");
        }
    }

    void HandleDragDetected(int sextant)
    {
        // Placeholder for future drag logic if needed
    }

    void StartDrag()
    {
        isDragging = true;
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, dragStartWorldPos);
        lineRenderer.SetPosition(1, dragStartWorldPos);
        LastDragLength = 0f;

        Debug.Log("Drag started in Sextant 2");
    }

    void StopDrag()
    {
        isDragging = false;
        freezeLength = false;
        isFrozen = false;
        lineRenderer.enabled = false;
        extensionLineRenderer.enabled = false;
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

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || _anchorImage == null || _anchorContainer == null || Camera.main == null)
            return;

        // Solo dibujamos la línea límite si ya calculamos la posición de la imagen
        if (_imageTopScreenY <= 0f)
            return;

        // Convertir el screen Y de la línea a world points en X min y X max (bordes de pantalla)
        Vector3 leftScreenPoint = new Vector3(0, _imageTopScreenY, Camera.main.nearClipPlane + 1);
        Vector3 rightScreenPoint = new Vector3(Screen.width, _imageTopScreenY, Camera.main.nearClipPlane + 1);

        Vector3 leftWorldPoint = Camera.main.ScreenToWorldPoint(leftScreenPoint);
        Vector3 rightWorldPoint = Camera.main.ScreenToWorldPoint(rightScreenPoint);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(leftWorldPoint, rightWorldPoint);

        // Opcional: también dibujar la línea del drag y etiqueta si está activo
        if (isDragging && lineRenderer.enabled && lineRenderer.positionCount >= 2)
        {
            Vector3 startPos = lineRenderer.GetPosition(0);
            Vector3 endPos = lineRenderer.GetPosition(1);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(startPos, endPos);

            Vector3 midPoint = (startPos + endPos) * 0.5f;
            Handles.Label(midPoint + Vector3.up * 0.1f, $"Length: {LastDragLength:F2}");
        }
    }
#endif
}
