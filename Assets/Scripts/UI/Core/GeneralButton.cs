using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class GeneralButton : BounceInteractable
{
    [TitleGroup("Events")]
    [SerializeField] private UnityEvent onClickEvent;
    [SerializeField] private UnityEvent onHoverEvent;
    [SerializeField] private UnityEvent unHoverEvent;

    [TitleGroup("Click Behaviour")]
    [SerializeField] private bool triggerInteractOFFafterClick = true;

    public override void HoverAnimation()
    {
        base.HoverAnimation();
        onHoverEvent?.Invoke();
    }

    public override void ExitHoverAnimation()
    {
        base.ExitHoverAnimation();
        unHoverEvent?.Invoke();
    }

    public override void OnClick()
    {
        if (triggerInteractOFFafterClick && interactable != null)
        {
            interactable.enabled = false;
        }

        onClickEvent?.Invoke();
     
    }
    
     
}
