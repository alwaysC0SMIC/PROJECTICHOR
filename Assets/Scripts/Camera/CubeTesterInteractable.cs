using UnityEngine;
using DG.Tweening;

public class CubeTesterInteractable : MonoBehaviour, IInteractable
{
    #region CONSTANTS
    private const float BASE_SCALE = 1f;
    private const float HOVER_SCALE = 1.1f;
    #endregion

    #region CONFIG
    [Header("SCALE")]
    [SerializeField, Tooltip("Duration (seconds) for hover scale in/out")]
    private float scaleDuration = 0.12f;

    [SerializeField, Tooltip("Ease for hover scale in/out")]
    private Ease scaleEase = Ease.OutQuad;

    [Header("ROTATION")]
    [SerializeField, Tooltip("Duration (seconds) for a full 360° rotation")]
    private float rotateDuration = 0.35f;

    [SerializeField, Tooltip("Ease for rotation")]
    private Ease rotateEase = Ease.OutCubic;

    [SerializeField, Tooltip("Local axis used for rotation")]
    private Vector3 rotationAxis = Vector3.up;

    [Header("Advanced")]
    [SerializeField, Tooltip("Use unscaled time (true) or scaled (false) for tweens")]
    private bool useUnscaledTime = true;
    #endregion

    #region STATE
    private Tween _scaleTween;
    private Tween _rotateTween;
    #endregion

    #region UNITY
    private void Awake()
    {
        // Ensure a clean baseline
        transform.localScale = Vector3.one * BASE_SCALE;
    }

    private void OnDisable()
    {
        // Kill any active tweens when this is disabled (scene change, deactivate, etc.)
        _scaleTween?.Kill();
        _rotateTween?.Kill();
        DOTween.Kill(transform); // safety: kill any other tweens targeting this transform
    }

    private void OnDestroy()
    {
        DOTween.Kill(transform);
    }
    #endregion

    #region IInteractable
    public void OnHover()
    {
        //Debug.Log($"{nameof(CubeTesterInteractable)}: Hover started on {name}");

        // Scale up to 1.1f
        _scaleTween?.Kill();
        _scaleTween = transform
            .DOScale(Vector3.one * HOVER_SCALE, scaleDuration)
            .SetEase(scaleEase)
            .SetUpdate(useUnscaledTime)
            .SetTarget(transform); // so DOTween.Kill(transform) also clears it
    }

    public void OnHoverExit()
    {
        //Debug.Log($"{nameof(CubeTesterInteractable)}: Hover ended on {name}");

        // Scale back to 1.0f
        _scaleTween?.Kill();
        _scaleTween = transform
            .DOScale(Vector3.one * BASE_SCALE, scaleDuration)
            .SetEase(scaleEase)
            .SetUpdate(useUnscaledTime)
            .SetTarget(transform);
    }

    public void OnClick()
    {
        // Add +360° around the chosen axis (local)
        _rotateTween?.Kill();
        _rotateTween = transform
            .DORotate(rotationAxis.normalized * 360f, rotateDuration, RotateMode.LocalAxisAdd)
            .SetEase(rotateEase)
            .SetUpdate(useUnscaledTime)
            .SetTarget(transform);
    }
    #endregion
}
