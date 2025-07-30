using DG.Tweening;
using UnityEngine;

public class TweenPlayer : MonoBehaviour
{
    //VARIABLES
    public TweenPreset preset;
    public Transform targetTransform;
    public CanvasGroup targetCanvasGroup;

    private Tween activeTween;

    public void PlayTween()
    {
        if (preset.tweenType == TweenPreset.TweenType.Fade && targetCanvasGroup != null)
            activeTween = preset.ApplyTween(targetCanvasGroup);
        else if (targetTransform != null)
            activeTween = preset.ApplyTween(targetTransform);
    }

    public void KillTween()
    {
        activeTween?.Kill();
    }
}
