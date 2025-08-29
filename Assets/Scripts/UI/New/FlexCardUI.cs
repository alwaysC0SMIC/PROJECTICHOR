using System;
using AllIn1SpringsToolkit;
using Coffee.UIEffects;
using DG.Tweening;
using Flexalon;
using Nova;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static Flexalon.FlexalonInteractable;

public class FlexCardUI : MonoBehaviour
{
    //VARIABLES
    [SerializeField] private SO_Defender defenderData;
    [SerializeField] Image backgroundImage;
    [SerializeField] private FlexalonObject flexalonObject;
    [SerializeField] private FlexalonLerpAnimator flexalonAnimator;
    [SerializeField] private FlexalonInteractable flexalonInteractable;
    [SerializeField] private float disableObjectTimer = 0.5F;

    [SerializeField] private float defaultHeight = 0.8F;
    [SerializeField] private float hoverHeight = 1F;
    [SerializeField] private TransformSpringComponent transformSpringComponent;
    [SerializeField] private float rotationForce = 5F;

    [SerializeField] private Image cardImage;
    [SerializeField] private UIEffect uIEffect;
    //UI
    // [SerializeField] private TMP_Text cardNameText;
    // //[SerializeField] private TMP_Text cardDescriptionText;
    // [SerializeField] private TMP_Text cardCostText;


    void Start()
    {
        flexalonInteractable.Clicked.AddListener(ClickCheck);
        flexalonInteractable.HoverStart.AddListener(HoverAnimation);
        flexalonInteractable.HoverEnd.AddListener(UnHoverAnimation);
        flexalonInteractable.DragStart.AddListener(OnDragStart);
        flexalonInteractable.DragEnd.AddListener(OnDragEnd);
    }

    private void ClickCheck(FlexalonInteractable arg0)
    {
        if (GameManager.Instance.currentIchorAmount < defenderData.cost)
        {
            AnimateCostTextColor();
        }

    }
    private void AnimateCostTextColor()
    {
        // if (cardCostText != null)
        // {
        //     Color startColor = Color.white;
        //     Color endColor = Color.red;
        //     float halfDuration = 0.25f;
        //     DOTween.To(
        //         () => cardCostText.color,
        //         x => cardCostText.color = x,
        //         endColor,
        //         halfDuration
        //     ).OnComplete(() => {
        //         DOTween.To(
        //             () => cardCostText.color,
        //             x => cardCostText.color = x,
        //             startColor,
        //             halfDuration
        //         );
        //     });
        // }
    }


    public void InitializeCard(SO_Defender defender)
    {
        defenderData = defender;
        cardImage.sprite = defenderData.defenderArt;
        uIEffect.color = defenderData.hdrColorForCard;
        // cardNameText.text = defenderData.defenderName;
        // //cardDescriptionText.text = defenderData.defenderDescription;
        // cardCostText.text = "Cost: " + defenderData.cost.ToString();
    }

    private void OnDragEnd(FlexalonInteractable arg0)
    {
        backgroundImage.color = new Color(1, 1, 1, 1f);
        EventBus<ToggleGameplayCamEvent>.Raise(new ToggleGameplayCamEvent { allowCam = true });
        EventBus<BuildingEvent>.Raise(new BuildingEvent { isBuilding = false, defenderToBuild = defenderData });

        //flexalonObject.Rotation = Quaternion.Euler(0, 0, 0);

    }

    private void OnDragStart(FlexalonInteractable arg0)
    {
        EventBus<ToggleGameplayCamEvent>.Raise(new ToggleGameplayCamEvent { allowCam = false });
        EventBus<BuildingEvent>.Raise(new BuildingEvent { isBuilding = true, defenderToBuild = defenderData });

        //flexalonObject.Rotation = Quaternion.Euler(0, 0, 90F);
        backgroundImage.color = new Color(1, 1, 1, 0.1f); 
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
        // Get mouse position in screen space
        Vector2 mousePos = Input.mousePosition;

        // Get the background image's RectTransform
        RectTransform imageRect = backgroundImage.rectTransform;

        // Convert mouse position to local position relative to the image
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            imageRect,
            mousePos,
            Camera.main,
            out localMousePos
        );

        // Check which half of the image the mouse is over (left half = negative X, right half = positive X)
        bool mouseOnLeftHalf = localMousePos.x < 0;

        // Apply rotation based on which half is being hovered
        Vector3 rotationDirection;
        if (mouseOnLeftHalf)
        {
            // Mouse is on left half - rotate clockwise (positive Z)
            rotationDirection = Vector3.forward * rotationForce;
            Debug.Log($"[FlexCardUI] Mouse on LEFT half of image - rotating clockwise");
        }
        else
        {
            // Mouse is on right half - rotate counter-clockwise (negative Z)
            rotationDirection = Vector3.forward * -rotationForce;
            Debug.Log($"[FlexCardUI] Mouse on RIGHT half of image - rotating counter-clockwise");
        }

        transformSpringComponent.AddVelocityRotation(rotationDirection);
        flexalonObject.HeightOfParent = hoverHeight;

        if (GameManager.Instance.currentIchorAmount < defenderData.cost)
        {
            flexalonInteractable.Draggable = false;
        }
        else
        { 
            flexalonInteractable.Draggable = true;
        }
            
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
    }

    void OnDestroy()
    {
        flexalonInteractable.HoverStart.RemoveAllListeners();
    }



}
