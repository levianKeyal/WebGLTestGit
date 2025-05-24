using UnityEngine;

public class AnchorZone : MonoBehaviour
{
    [SerializeField]
    private ScreenSextantDetector _screenSextanDetector;

    [SerializeField]
    private Canvas _anchorCanvas;                // Reference to the _anchorCanvas

    [SerializeField]
    private RectTransform _anchorContainer;  // Assign the container holding the image

    [SerializeField]
    private bool _centerOnSextant;

    [SerializeField]
    private int _sextantPosition;

    void Start()
    {
        _screenSextanDetector = GetComponent<ScreenSextantDetector>();

        if(_centerOnSextant)
            _screenSextanDetector.AlignToSextant(_sextantPosition,_anchorCanvas, _anchorContainer);
    }
}
