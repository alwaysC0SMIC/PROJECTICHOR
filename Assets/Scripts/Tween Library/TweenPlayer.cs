using DG.Tweening;
using UnityEngine;
using Nova;

public class TweenPlayer : MonoBehaviour
{
    //VARIABLES
    public TweenPreset preset;
    public Transform targetTransform;
    public CanvasGroup targetCanvasGroup;
    public UIBlock targetUIBlock;

    private Tween activeTween;

    public void PlayTween()
    {
        if (preset.tweenType == TweenPreset.TweenType.Fade && targetCanvasGroup != null)
            activeTween = preset.ApplyTween(targetCanvasGroup);
        else if ((preset.tweenType == TweenPreset.TweenType.NovaScale || preset.tweenType == TweenPreset.TweenType.NovaPosition) && targetUIBlock != null)
            activeTween = preset.ApplyTween(targetUIBlock);
        else if (targetTransform != null)
            activeTween = preset.ApplyTween(targetTransform);
    }

    public void KillTween()
    {
        activeTween?.Kill();
    }
}
