using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class TweenTester : MonoBehaviour
{
    [BoxGroup("Tween Settings"), Required]
    public TweenPreset tweenPreset;

    [BoxGroup("Tween Settings"), ShowIf("@tweenPreset != null && tweenPreset.tweenType != TweenPreset.TweenType.Fade")]
    public Transform targetTransform;

    [BoxGroup("Tween Settings"), ShowIf("@tweenPreset != null && tweenPreset.tweenType == TweenPreset.TweenType.Fade")]
    public CanvasGroup targetCanvasGroup;

    [BoxGroup("Runtime"), ReadOnly]
    public Vector3 originalPosition, originalScale, originalRotation;

    [BoxGroup("Runtime"), ReadOnly]
    public float originalAlpha;

    private Tween currentTween;

    void Start()
    {
        StoreOriginalState();
    }

    [BoxGroup("Controls")]
    [Button("▶ Play Tween")]
    private void PlayTween()
    {
        if (tweenPreset == null)
        {
            Debug.LogWarning("TweenPreset missing.");
            return;
        }

        //StoreOriginalState();

        if (tweenPreset.tweenType == TweenPreset.TweenType.Fade && targetCanvasGroup != null)
        {
            currentTween = tweenPreset.ApplyTween(targetCanvasGroup);
        }
        else if (targetTransform != null)
        {
            currentTween = tweenPreset.ApplyTween(targetTransform);
        }
        else
        {
            Debug.LogWarning("No valid target assigned.");
        }
    }

    [BoxGroup("Controls")]
    [Button("⏹ Stop Tween")]
    private void StopTween()
    {
        currentTween?.Kill();
        currentTween = null;
    }

    [BoxGroup("Controls")]
    [Button("⏪ Reset Target")]
    private void ResetTarget()
    {
        if (targetTransform != null)
        {
            targetTransform.position = originalPosition;
            targetTransform.localScale = originalScale;
            targetTransform.eulerAngles = originalRotation;
        }

        if (targetCanvasGroup != null)
        {
            targetCanvasGroup.alpha = originalAlpha;
        }
    }

    private void StoreOriginalState()
    {
        if (targetTransform != null)
        {
            originalPosition = targetTransform.position;
            originalScale = targetTransform.localScale;
            originalRotation = targetTransform.eulerAngles;
        }

        if (targetCanvasGroup != null)
        {
            originalAlpha = targetCanvasGroup.alpha;
        }
    }

    private void OnDisable()
    {
        StopTween();
    }
}
