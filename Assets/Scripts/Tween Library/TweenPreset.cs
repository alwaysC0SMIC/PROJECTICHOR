using System;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NewTweenPreset", menuName = "Tween/Tween Preset")]
public class TweenPreset : ScriptableObject
{
    public enum TweenType { Move, Scale, Rotate, Fade }

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
