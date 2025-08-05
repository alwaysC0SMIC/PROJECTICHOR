using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Nova;

public class TweenTester : MonoBehaviour
{
    [BoxGroup("Tween Settings"), Required]
    public TweenPreset tweenPreset;

    [BoxGroup("Tween Settings"), ShowIf("@tweenPreset != null && tweenPreset.tweenType != TweenPreset.TweenType.Fade && tweenPreset.tweenType != TweenPreset.TweenType.NovaScale && tweenPreset.tweenType != TweenPreset.TweenType.NovaPosition")]
    public Transform targetTransform;

    [BoxGroup("Tween Settings"), ShowIf("@tweenPreset != null && tweenPreset.tweenType == TweenPreset.TweenType.Fade")]
    public CanvasGroup targetCanvasGroup;

    [BoxGroup("Tween Settings"), ShowIf("@tweenPreset != null && (tweenPreset.tweenType == TweenPreset.TweenType.NovaScale || tweenPreset.tweenType == TweenPreset.TweenType.NovaPosition)")]
    public UIBlock targetUIBlock;

    [BoxGroup("Runtime"), ReadOnly]
    public Vector3 originalPosition, originalScale, originalRotation;

    [BoxGroup("Runtime"), ReadOnly]
    public float originalAlpha;

    [BoxGroup("Runtime"), ReadOnly]
    public Vector3 originalNovaScale;

    [BoxGroup("Runtime"), ReadOnly]
    public Vector3 originalNovaPosition;

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
        else if ((tweenPreset.tweenType == TweenPreset.TweenType.NovaScale || tweenPreset.tweenType == TweenPreset.TweenType.NovaPosition) && targetUIBlock != null)
        {
            currentTween = tweenPreset.ApplyTween(targetUIBlock);
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

        if (targetUIBlock != null)
        {
            // Reset Nova Scale if this was a scale tween
            if (tweenPreset != null && tweenPreset.tweenType == TweenPreset.TweenType.NovaScale)
            {
                targetUIBlock.Size = new Nova.Length3(
                    Nova.Length.Percentage(originalNovaScale.x),
                    Nova.Length.Percentage(originalNovaScale.y),
                    Nova.Length.Percentage(originalNovaScale.z)
                );
            }
            
            // Reset Nova Position if this was a position tween
            if (tweenPreset != null && tweenPreset.tweenType == TweenPreset.TweenType.NovaPosition)
            {
                targetUIBlock.Layout.Position = new Nova.Length3(
                    Nova.Length.Percentage(originalNovaPosition.x),
                    Nova.Length.Percentage(originalNovaPosition.y),
                    Nova.Length.Percentage(originalNovaPosition.z)
                );
            }
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

        if (targetUIBlock != null)
        {
            // Store Nova Scale values
            var currentSize = targetUIBlock.Size;
            originalNovaScale = new Vector3(
                currentSize.X.Percent,
                currentSize.Y.Percent,
                currentSize.Z.Percent
            );
            
            // Store Nova Position values
            var currentPosition = targetUIBlock.Layout.Position;
            originalNovaPosition = new Vector3(
                currentPosition.X.Percent,
                currentPosition.Y.Percent,
                currentPosition.Z.Percent
            );
        }
    }

    private void OnDisable()
    {
        StopTween();
    }
}
