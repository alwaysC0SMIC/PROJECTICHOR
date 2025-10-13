using Flexalon;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MenuButton : MonoBehaviour
{
    //VARIABLES
    [SerializeField] Image image;
    [SerializeField] TMP_Text text;
    [SerializeField] FlexalonInteractable interactable;

    [Header("EVENTS")]
    [SerializeField] private UnityEvent OnClickEvent;
    [SerializeField] private UnityEvent OnHoverEvent;
    [SerializeField] private UnityEvent OnUnHoverEvent;
    
    void Start()
    {
        interactable.Clicked.AddListener(ClickFunction);
        interactable.HoverStart.AddListener(HoverAnimation);
        interactable.HoverEnd.AddListener(UnHoverAnimation);

        image.color = Color.clear;
        text.color = Color.white;
    }

    private void ClickFunction(FlexalonInteractable arg0)
    {
        OnClickEvent?.Invoke();
    }

    private void UnHoverAnimation(FlexalonInteractable arg0)
    {
        OnUnHoverEvent?.Invoke();
        image.color = Color.clear;
        text.color = Color.white;
    }

    private void HoverAnimation(FlexalonInteractable arg0)
    {
        OnHoverEvent?.Invoke();
        image.color = Color.black;
        text.color = Color.red;
    }

}
