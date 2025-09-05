using System;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using Sirenix.OdinInspector;
using Nova;

[CreateAssetMenu(fileName = "NewTweenPreset", menuName = "Tween/Tween Preset")]
public class TweenPreset : ScriptableObject
{
    public enum TweenType { Move, Scale, Rotate, Fade, NovaScale, NovaPosition }

    #region BASICS

    [TitleGroup("Tween/Setup")]
    [EnumToggleButtons, LabelText("Type")]
    public TweenType tweenType = TweenType.Move;

    [TitleGroup("Tween/Setup"), Min(0f)]
    public float duration = 1f;

    [TitleGroup("Tween/Setup")]
    public Ease ease = Ease.OutQuad;

    [TitleGroup("Tween/Setup"), Min(0f)]
    public float delay = 0f;

    [TitleGroup("Tween/Looping")]
    public LoopType loopType = LoopType.Restart;

    [TitleGroup("Tween/Looping"), Min(0)]
    public int loops = 0;

    #endregion

    #region TARGET VALUES

    [TitleGroup("Targets")]
    [ShowIf("@tweenType == TweenType.Move || tweenType == TweenType.Scale || tweenType == TweenType.Rotate")]
    [LabelText("Target Vector (Move/Scale/Rotate)")]
    public Vector3 targetVector;

    [TitleGroup("Targets")]
    [ShowIf("@tweenType == TweenType.NovaScale")]
    [LabelText("Target Scale (Nova UI) - Percent Values")]
    [Tooltip("X, Y, Z values represent percentage scaling (e.g., 1.0 = 100%, 0.5 = 50%, 1.5 = 150%)")]
    [InfoBox("Nova UI Scale uses percentage values. 1.0 = 100% size, 0.5 = 50% size, 2.0 = 200% size", InfoMessageType.Info, "@tweenType == TweenType.NovaScale")]
    public Vector3 targetNovaScale = Vector3.one;

    [TitleGroup("Targets")]
    [ShowIf("@tweenType == TweenType.NovaPosition")]
    [LabelText("Target Position (Nova UI) - Percent Values")]
    [Tooltip("X, Y, Z values represent percentage positioning (e.g., 0.5 = 50% from left/top, 1.0 = 100% = right/bottom edge)")]
    [InfoBox("Nova UI Position uses percentage values. 0.0 = left/top edge, 0.5 = center, 1.0 = right/bottom edge", InfoMessageType.Info, "@tweenType == TweenType.NovaPosition")]
    public Vector3 targetNovaPosition = new Vector3(0.5f, 0.5f, 0f);

    [TitleGroup("Targets")]
    [ShowIf("@tweenType == TweenType.Fade")]
    [LabelText("Target Alpha (Fade)"), Range(0f, 1f)]
    public float targetFloat = 1f;

    #endregion

    #region EVENTS

    [TitleGroup("Events")]
    public UnityEvent onCompleteUnityEvent;

    /// <summary>
    /// Subscribable event for code listeners.
    /// </summary>
    public event Action OnComplete;

    #endregion

    #region APPLY (TRANSFORM)

    /// <summary>
    /// Apply Move/Scale/Rotate tweens to a Transform.
    /// </summary>
    public Tween ApplyTween(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning($"[{name}] ApplyTween(Transform): target is null.");
            return null;
        }

        Tween tween = null;

        switch (tweenType)
        {
            case TweenType.Move:
                tween = target.DOMove(targetVector, duration);
                break;

            case TweenType.Scale:
                tween = target.DOScale(targetVector, duration);
                break;

            case TweenType.Rotate:
                tween = target.DORotate(targetVector, duration, RotateMode.FastBeyond360);
                break;

            case TweenType.Fade:
                // Not applicable to Transform directly; use CanvasGroup overload instead.
                Debug.LogWarning($"[{name}] Fade tween requires a CanvasGroup target. Use ApplyTween(CanvasGroup).");
                return null;

            case TweenType.NovaScale:
                // Not applicable to Transform directly; use UIBlock overload instead.
                Debug.LogWarning($"[{name}] NovaScale tween requires a UIBlock target. Use ApplyTween(UIBlock).");
                return null;

            case TweenType.NovaPosition:
                // Not applicable to Transform directly; use UIBlock overload instead.
                Debug.LogWarning($"[{name}] NovaPosition tween requires a UIBlock target. Use ApplyTween(UIBlock).");
                return null;
        }

        return ConfigureTween(tween)?.SetTarget(target);
    }

    #endregion

    #region APPLY (CANVASGROUP FADE VIA DOTWEEN.TO)

    /// <summary>
    /// Apply Fade to a CanvasGroup using DOTween.To (not DOFade).
    /// </summary>
    public Tween ApplyTween(CanvasGroup target)
    {
        if (tweenType != TweenType.Fade)
        {
            Debug.LogWarning($"[{name}] ApplyTween(CanvasGroup) called but tweenType is {tweenType}. Expected Fade.");
            return null;
        }

        if (target == null)
        {
            Debug.LogWarning($"[{name}] ApplyTween(CanvasGroup): target is null.");
            return null;
        }

        // Replace DOFade with a raw value tween for alpha:
        float start = target.alpha;
        Tween tween = DOTween.To(
            () => target.alpha,
            a => target.alpha = a,
            targetFloat,
            duration
        );

        return ConfigureTween(tween)?.SetTarget(target);
    }

    #endregion

    #region APPLY (NOVA UIBLOCK SCALE)

    /// <summary>
    /// Apply NovaScale or NovaPosition to a UIBlock using percentage-based values with Canvas updates.
    /// </summary>
    public Tween ApplyTween(UIBlock target)
    {
        if (tweenType != TweenType.NovaScale && tweenType != TweenType.NovaPosition)
        {
            Debug.LogWarning($"[{name}] ApplyTween(UIBlock) called but tweenType is {tweenType}. Expected NovaScale or NovaPosition.");
            return null;
        }

        if (target == null)
        {
            Debug.LogWarning($"[{name}] ApplyTween(UIBlock): target is null.");
            return null;
        }

        Tween tween = null;

        if (tweenType == TweenType.NovaScale)
        {
            tween = CreateNovaScaleTween(target);
        }
        else if (tweenType == TweenType.NovaPosition)
        {
            tween = CreateNovaPositionTween(target);
        }

        return ConfigureTween(tween)?.SetTarget(target);
    }

    private Tween CreateNovaScaleTween(UIBlock target)
    {
        // Get the current size as starting point
        Length3 currentSize = target.Size;
        
        // Extract current percentage values
        Vector3 startValues = new Vector3(
            currentSize.X.Percent,
            currentSize.Y.Percent,
            currentSize.Z.Percent
        );

        // Create a tween using DOTween.To for smooth interpolation
        return DOTween.To(
            () => startValues,
            value =>
            {
                // Update the UIBlock size with new percentage values
                target.Size = new Length3(
                    Length.Percentage(value.x),
                    Length.Percentage(value.y),
                    Length.Percentage(value.z)
                );
                
                // Force Nova Canvas to update layout immediately
                Canvas.ForceUpdateCanvases();
            },
            targetNovaScale,
            duration
        );
    }

    private Tween CreateNovaPositionTween(UIBlock target)
    {
        // Get the current position as starting point
        Length3 currentPosition = target.Layout.Position;
        
        // Extract current percentage values
        Vector3 startValues = new Vector3(
            currentPosition.X.Percent,
            currentPosition.Y.Percent,
            currentPosition.Z.Percent
        );

        // Create a tween using DOTween.To for smooth interpolation
        return DOTween.To(
            () => startValues,
            value =>
            {
                // Update the UIBlock position with new percentage values
                target.Layout.Position = new Length3(
                    Length.Percentage(value.x),
                    Length.Percentage(value.y),
                    Length.Percentage(value.z)
                );
                
                // Force Nova Canvas to update layout immediately
                Canvas.ForceUpdateCanvases();
            },
            targetNovaPosition,
            duration
        );
    }

    #endregion

    #region HELPER METHODS

    /// <summary>
    /// Quick helper to create a Nova UI scale tween with common percentage values.
    /// </summary>
    /// <param name="target">The UIBlock to animate</param>
    /// <param name="scalePercent">Target scale as percentage (1.0 = 100%, 0.5 = 50%)</param>
    /// <returns>The configured tween</returns>
    public Tween ApplyNovaScale(UIBlock target, float scalePercent)
    {
        if (tweenType != TweenType.NovaScale)
        {
            Debug.LogWarning($"[{name}] ApplyNovaScale called but tweenType is {tweenType}. Expected NovaScale.");
            return null;
        }

        // Temporarily set target scale and apply
        Vector3 originalTarget = targetNovaScale;
        targetNovaScale = Vector3.one * scalePercent;
        
        Tween result = ApplyTween(target);
        
        // Restore original target
        targetNovaScale = originalTarget;
        
        return result;
    }

    /// <summary>
    /// Check if this tween preset requires a specific target type.
    /// </summary>
    /// <returns>The required target type as a string, or null if any Transform works</returns>
    public string GetRequiredTargetType()
    {
        switch (tweenType)
        {
            case TweenType.Fade:
                return "CanvasGroup";
            case TweenType.NovaScale:
            case TweenType.NovaPosition:
                return "UIBlock";
            default:
                return "Transform";
        }
    }

    #endregion

    #region INTERNAL

    private Tween ConfigureTween(Tween tween)
    {
        if (tween == null) return null;

        tween.SetEase(ease)
             .SetDelay(delay)
             .SetLoops(loops, loopType)
             .OnComplete(() =>
             {
                 onCompleteUnityEvent?.Invoke();
                 OnComplete?.Invoke();
             });

        return tween;
    }

    #endregion
}
