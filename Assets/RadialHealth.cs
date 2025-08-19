using UnityEngine;
using UnityEngine.UI;

public class RadialHealth : MonoBehaviour
{
    //VARIABLES
    [SerializeField] private Image radialFillImage;
    private float maxHealth = 100f;
    private float currentHealth = 100f;
    private float fillAnimDuration = 0.3f;

    public void Initialize(float maxHealthValue)
    {
        maxHealth = maxHealthValue;
        currentHealth = maxHealthValue;
        SetFillInstant(1f);
    }

    public void ChangeHealth(float delta)
    {
        currentHealth = Mathf.Clamp(currentHealth + delta, 0, maxHealth);
        float targetFill = maxHealth > 0 ? currentHealth / maxHealth : 0f;
        AnimateFill(targetFill);
    }

    private void SetFillInstant(float value)
    {
        if (radialFillImage != null)
            radialFillImage.fillAmount = value;
    }

    private void AnimateFill(float targetValue)
    {
        if (radialFillImage != null)
        {
            DG.Tweening.DOTween.To(() => radialFillImage.fillAmount, x => radialFillImage.fillAmount = x, targetValue, fillAnimDuration);
        }
    }
}