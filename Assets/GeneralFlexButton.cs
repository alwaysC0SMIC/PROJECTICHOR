using System;
using Flexalon;
using UnityEngine;

public class GeneralFlexButton : MonoBehaviour
{
    [SerializeField] private FlexalonInteractable interactable;
    
    void Start()
    {
        interactable.Clicked.AddListener(ClickFunction);
        interactable.HoverStart.AddListener(HoverAnimation);
        interactable.HoverEnd.AddListener(UnHoverAnimation);
    }

    private void ClickFunction(FlexalonInteractable arg0)
    {
       EventBus<UpdateGameStateEvent>.Raise(new UpdateGameStateEvent { gameState = GameState.Playing });
    }

    private void UnHoverAnimation(FlexalonInteractable arg0)
    {
        
    }

    private void HoverAnimation(FlexalonInteractable arg0)
    {
        
    }

}
