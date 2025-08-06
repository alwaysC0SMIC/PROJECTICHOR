using System;
using Flexalon;
using Nova;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static Flexalon.FlexalonInteractable;

public class FlexCardUI : MonoBehaviour
{
    //VARIABLES
    [SerializeField] private FlexalonObject flexalonObject;
    [SerializeField] private FlexalonLerpAnimator flexalonAnimator;
    [SerializeField] private FlexalonInteractable flexalonInteractable;
    [SerializeField] private float disableObjectTimer = 0.5F;

    [SerializeField] private float defaultHeight = 0.8F;
    [SerializeField] private float hoverHeight = 1F;

    void Start()
    {
        //flexalonInteractable.Clicked += 
        flexalonInteractable.HoverStart.AddListener(HoverAnimation);
        flexalonInteractable.HoverEnd.AddListener(UnHoverAnimation);
        flexalonInteractable.DragStart.AddListener(OnDragStart);
        flexalonInteractable.DragEnd.AddListener(OnDragEnd);
        //DisableCard();
    }

    private void OnDragEnd(FlexalonInteractable arg0)
    {
        EventBus<ToggleGameplayCamEvent>.Raise(new ToggleGameplayCamEvent { allowCam = true });
        EventBus<BuildingEvent>.Raise(new BuildingEvent { isBuilding = false });

        //Debug.Log("Drag Ended");
        flexalonObject.Rotation = Quaternion.Euler(0, 0, 0);
        //flexalonObject.HeightOfParent = defaultHeight;
        flexalonObject.Scale =  Vector3.one;
    }

    private void OnDragStart(FlexalonInteractable arg0)
    {
        EventBus<ToggleGameplayCamEvent>.Raise(new ToggleGameplayCamEvent { allowCam = false });
        EventBus<BuildingEvent>.Raise(new BuildingEvent { isBuilding = true });
        

        //Debug.Log("Drag Started");
        flexalonObject.Rotation = Quaternion.Euler(0, 0, 90F);
        flexalonObject.Scale =  Vector3.one*0.1F;
    }

    public void OnEnable()
    {
        EnableCard();
    }

    public void OnDisable()
    {
        DisableCard();
    }



    private void UnHoverAnimation(FlexalonInteractable arg0)
    {
        flexalonObject.HeightOfParent = defaultHeight;
    }

    private void HoverAnimation(FlexalonInteractable arg0)
    {
        flexalonObject.HeightOfParent = hoverHeight;
    }


    public void EnableCard()
    {
        flexalonInteractable.Clickable = true;
        //REPLACE WITH ACTUAL ANIMATION SYSTEM IN FUTURE
        flexalonObject.HeightOfParent = defaultHeight;
    }

    public void DisableCard()
    {
        flexalonInteractable.Clickable = false;
        flexalonObject.HeightOfParent = 0F;
        //Invoke("DisableObject", disableObjectTimer);
    }

    void OnDestroy()
    {
        flexalonInteractable.HoverStart.RemoveAllListeners();
    }



}
