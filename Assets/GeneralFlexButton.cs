using System;
using Flexalon;
using UnityEngine;
using UnityEngine.Events;

public class GeneralFlexButton : MonoBehaviour
{
    [Header("INTERACTABLE")]
    [SerializeField] private FlexalonInteractable interactable;

    [Header("EVENTS")]
    [SerializeField] private UnityEvent OnClickEvent;
    [SerializeField] private UnityEvent OnHoverEvent;
    [SerializeField] private UnityEvent OnUnHoverEvent;
    
    void Start()
    {
        interactable.Clicked.AddListener(ClickFunction);
        interactable.HoverStart.AddListener(HoverAnimation);
        interactable.HoverEnd.AddListener(UnHoverAnimation);
    }

    private void ClickFunction(FlexalonInteractable arg0)
    {
        OnClickEvent?.Invoke();
    }

    private void UnHoverAnimation(FlexalonInteractable arg0)
    {
        OnUnHoverEvent?.Invoke();
    }

    private void HoverAnimation(FlexalonInteractable arg0)
    {
        OnHoverEvent?.Invoke();
    }

}
