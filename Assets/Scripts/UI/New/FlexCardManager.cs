using System;
using System.Collections.Generic;
using Flexalon;
using Microsoft.Unity.VisualStudio.Editor;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class FlexCardManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    //VARIABLES
    [SerializeField] private List<SO_Defender> defendersInHand;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private int handSize = 5;
    private int maxHandSize;

    [SerializeField] public List<GameObject> hand = new List<GameObject>();
    private bool canScroll = false;

    // Scroll buffer variables
    [SerializeField, Range(0.05f, 0.5f), Tooltip("Cooldown buffer after scroll operations")]
    private float scrollInputBuffer = 0.1f;
    private bool scrollBuffer = true;
    
    // Visible window tracking
    private int currentStartIndex = 0;

    [SerializeField] public FlexalonObject flexalonObject;

    // Event binding for BuildingEvent
    private EventBinding<BuildingEvent> buildingEventBinding;
    private bool canInteract = true;

    void Start()
    {
        //TEMP
        //maxHandSize = handSize + 2;
        maxHandSize = handSize;

        // Initialize in unhovered state
        UnHoverAnimation();

        // Register for building events
        buildingEventBinding = new EventBinding<BuildingEvent>(OnBuildingEvent);
        EventBus<BuildingEvent>.Register(buildingEventBinding);

        GenerateHand();
        UpdateVisibleCards();
    }

    void OnDestroy()
    {
        // Unregister from building events
        if (buildingEventBinding != null)
        {
            EventBus<BuildingEvent>.Deregister(buildingEventBinding);
            buildingEventBinding = null;
        }
    }

    private void OnBuildingEvent(BuildingEvent buildingEvent)
    {
        // If building mode is activated, hide the cards
        if (buildingEvent.isBuilding)
        {
            canInteract = false;
            UnHoverAnimation();
            Debug.Log("[FlexCardManager] Build mode activated - hiding cards");
        }
        else
        { 
            canInteract = true;
        }
        // Note: We don't auto-show cards when build mode ends, 
        // let the user hover to show them again
    }

    private void UnHoverAnimation()
    {
        flexalonObject.Offset = new Vector3(0, -250F, 0);
    }

    private void HoverAnimation()
    {
        flexalonObject.Offset = Vector3.zero;
    }

    public void GenerateHand()
    {
        hand.Clear();

        // // Define color palette with names
        // Color[] cardColors = {
        //     Color.red,
        //     Color.blue,
        //     Color.green,
        //     Color.yellow,
        //     Color.magenta,
        //     Color.cyan,
        //     new Color(1f, 0.5f, 0f), // Orange
        //     new Color(0.5f, 0f, 0.5f), // Purple
        //     new Color(0.5f, 0.25f, 0f), // Brown
        //     new Color(1f, 0.75f, 0.8f) // Pink
        // };
        
        // string[] colorNames = {
        //     "Red",
        //     "Blue", 
        //     "Green",
        //     "Yellow",
        //     "Magenta",
        //     "Cyan",
        //     "Orange",
        //     "Purple",
        //     "Brown",
        //     "Pink"
        // };

        for (int i = 0; i < maxHandSize; i++)
        {
            GameObject card = Instantiate(cardPrefab, transform);
            
            card.GetComponent<FlexCardUI>().InitializeCard(defendersInHand[0]);
            
            // Assign unique color and name
            //Color cardColor = cardColors[i % cardColors.Length];
            //string colorName = colorNames[i % colorNames.Length];

            card.name = $"Card {i + 1}";
            
            //card.GetComponent<UnityEngine.UI.Image>().color = cardColor;
            
            hand.Add(card);
            
           // Debug.Log($"[FlexCardManager] Created {card.name} with color {colorName}");
        }
    }
    
    private void UpdateVisibleCards()
    {
        for (int i = 0; i < hand.Count; i++)
        {
            bool shouldBeVisible = i >= currentStartIndex && i < currentStartIndex + handSize;
            
            if (shouldBeVisible)
            {
                hand[i].GetComponent<FlexCardUI>().EnableCard();
            }
            else
            {
                hand[i].GetComponent<FlexCardUI>().DisableCard();
            }
        }
        
        //Debug.Log($"[FlexCardManager] Visible window: {currentStartIndex} to {currentStartIndex + handSize - 1}");
    }

    void Update()
    {
        HandleScrollInput();
    }

    private void HandleScrollInput()
    {
        // Only process scroll input if hovering over this object
        if (!canScroll || !scrollBuffer) return;

        // Get scroll wheel input
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput > 0f)
        {
            // Scroll up - shift visible window left
            ScrollLeft();
        }
        else if (scrollInput < 0f)
        {
            // Scroll down - shift visible window right
            ScrollRight();
        }
    }

    public void ScrollLeft()
    {
        if (hand.Count <= handSize) return; // No scrolling needed if all cards fit
        
        scrollBuffer = false;
        
        // Wrap-around: if at the beginning, move last card to front
        if (currentStartIndex <= 0)
        {
            // Move the last card to the front of the list
            GameObject cardToMove = hand[hand.Count - 1];
            hand.RemoveAt(hand.Count - 1);
            hand.Insert(0, cardToMove);
            
            // Update hierarchy positions
            for (int i = 0; i < hand.Count; i++)
            {
                hand[i].transform.SetSiblingIndex(i);
            }
            
            // Start index stays at 0 since we moved the card to the front
            currentStartIndex = 0;
            
            Debug.Log($"[FlexCardManager] Wrapped around to end - moved card {cardToMove.name} to front");
        }
        else
        {
            // Normal left scroll
            currentStartIndex--;
            Debug.Log($"[FlexCardManager] Scrolled left - new start index: {currentStartIndex}");
        }
        
        UpdateVisibleCards();
        Invoke("ResetScrollBuffer", scrollInputBuffer);
    }

    public void ScrollRight()
    {
        if (hand.Count <= handSize) return; // No scrolling needed if all cards fit
        
        scrollBuffer = false;
        
        // Wrap-around: if at the end, move first card to back
        if (currentStartIndex >= hand.Count - handSize)
        {
            // Move the first card to the back of the list
            GameObject cardToMove = hand[0];
            hand.RemoveAt(0);
            hand.Add(cardToMove);
            
            // Update hierarchy positions
            for (int i = 0; i < hand.Count; i++)
            {
                hand[i].transform.SetSiblingIndex(i);
            }
            
            // Adjust start index to maintain the same visible window
            currentStartIndex = hand.Count - handSize;
            
            Debug.Log($"[FlexCardManager] Wrapped around to beginning - moved card {cardToMove.name} to back");
        }
        else
        {
            // Normal right scroll
            currentStartIndex++;
            Debug.Log($"[FlexCardManager] Scrolled right - new start index: {currentStartIndex}");
        }
        
        UpdateVisibleCards();
        Invoke("ResetScrollBuffer", scrollInputBuffer);
    }

    public void ResetScrollBuffer()
    {
        scrollBuffer = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(!canInteract) return;
        canScroll = true;
        HoverAnimation();
        Debug.Log("[FlexCardManager] Pointer entered - scrolling enabled and hover animation triggered");
    }

    // IPointerExitHandler implementation
    public void OnPointerExit(PointerEventData eventData)
    {
        if(!canInteract) return;
        canScroll = false;
        UnHoverAnimation();
        Debug.Log("[FlexCardManager] Pointer exited - scrolling disabled and unhover animation triggered");
    }
}
