using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova;
using Sirenix.OdinInspector;
using DG.Tweening;
using System.Linq;

public class CardHandManager : MonoBehaviour
{
    private static float endRotation = 7.5F;
    private static float endRestSize = 0.85F;

    private static float middleRotation = 3F;
    private static float middleRestSize = 0.85F;

    private static float selectedScalePercentage = 1.35F;

    [SerializeField] GameObject cardPrefab;
    
    [SerializeField] private List<GameObject> cardHand;
    
    [Header("Selection Range")]
    [SerializeField] private int visibleCardCount = 5;
    [SerializeField] private int currentStartIndex = 0;
    
    [Header("Scroll Settings")]
    [SerializeField] private float scrollSensitivity = 100f;
    [SerializeField] private float cardIntroAnimationDuration = 0.4f;
    [SerializeField] private float cardOutroAnimationDuration = 0.3f;
    [SerializeField] private float scrollInputBuffer = 0.1f; // Extra buffer time after animations
    
    [Header("Drag Settings")]
    [SerializeField] private float dragReturnDuration = 0.3f;
    [SerializeField] private float dragScaleFactor = 1.1f; // Scale when dragging
    
    private bool isScrolling = false;
    private float lastScrollTime = 0f;
    private bool isPointerOver = false; // Track mouse hover
    
    // Drag state tracking
    private GameObject draggedCard = null;
    private Vector3 draggedCardOriginalPosition;
    private Vector3 draggedCardOriginalScale;
    private int draggedCardOriginalIndex;
    private Transform draggedCardOriginalParent;
    private GameObject temporaryReplacementCard = null;


    void Start()
    {
        GenerateHand();
        SetupNovaHoverGesture();
    }
    
    void Update()
    {
        HandleInput();
    }
    
    private void HandleInput()
    {
        // Only allow scrolling if pointer is over this object
        if (!isPointerOver) return;

        // Don't allow scrolling during drag operations
        if (draggedCard != null) return;

        // Check if we're still in scroll cooldown period
        if (isScrolling || Time.time < lastScrollTime + GetTotalAnimationDuration() + scrollInputBuffer) 
            return;
        
        // Always allow scrolling if we have cards
        if (cardHand.Count == 0) return;
        
        // Use keyboard input for testing (A/D keys or Left/Right arrows)
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ScrollLeft();
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            ScrollRight();
        }
        
        // Handle mouse scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            ScrollLeft();
        }
        else if (scroll < 0f)
        {
            ScrollRight();
        }
    }
    
    private float GetTotalAnimationDuration()
    {
        return Mathf.Max(cardIntroAnimationDuration, cardOutroAnimationDuration);
    }
    
    private void ScrollLeft()
    {
        if (cardHand.Count <= visibleCardCount) return;
        
        // Cancel any active drag operation before scrolling
        CancelCurrentDrag();
        
        isScrolling = true;
        lastScrollTime = Time.time;
        
        // Don't move the card yet - let CoordinatedScrollAnimation handle it
        Debug.Log($"[CardHandManager] Scroll Left: Will move card {cardHand[cardHand.Count - 1].name} to front after animation");
        
        // Start coordinated animation - new card scales up, old card scales down
        StartCoroutine(CoordinatedScrollAnimationLeft());
    }
    
    private void ScrollRight()
    {
        if (cardHand.Count <= visibleCardCount) return;
        
        // Cancel any active drag operation before scrolling
        CancelCurrentDrag();
        
        isScrolling = true;
        lastScrollTime = Time.time;
        
        // Move the first card to the back
        GameObject cardToMove = cardHand[0];
        cardHand.RemoveAt(0);
        cardHand.Add(cardToMove);
        
        Debug.Log($"[CardHandManager] Scroll Right: Moved card {cardToMove.name} to back");
        
        // Start coordinated animation - new card scales up, old card scales down
        StartCoroutine(CoordinatedScrollAnimation());
    }
    
    private IEnumerator CoordinatedScrollAnimationLeft()
    {
        // For ScrollLeft, we need to handle the sequence differently
        // Find the card that should scale down (rightmost visible card)
        GameObject cardToScaleDown = null;
        for (int i = visibleCardCount - 1; i >= 0; i--)
        {
            if (i < cardHand.Count && cardHand[i].activeInHierarchy)
            {
                cardToScaleDown = cardHand[i];
                break;
            }
        }
        
        // Find the card that will scale up (the last card in the list that will move to front)
        GameObject cardToScaleUp = cardHand[cardHand.Count - 1];
        
        // Start the scale down animation first
        bool scaleDownFinished = false;
        if (cardToScaleDown != null)
        {
            StartCoroutine(AnimateCardToInvisibleAndDisable(cardToScaleDown, () => scaleDownFinished = true));
        }
        else
        {
            scaleDownFinished = true;
        }
        
        // Wait for scale down to finish before moving cards and updating hierarchy
        while (!scaleDownFinished)
        {
            yield return null;
        }
        
        // NOW move the last card to the front
        GameObject cardToMove = cardHand[cardHand.Count - 1];
        cardHand.RemoveAt(cardHand.Count - 1);
        cardHand.Insert(0, cardToMove);
        
        Debug.Log($"[CardHandManager] Scroll Left: Moved card {cardToMove.name} to front after scale down");
        
        // Update hierarchy positions
        for (int i = 0; i < cardHand.Count; i++)
        {
            cardHand[i].transform.SetSiblingIndex(i);
        }
        
        Canvas.ForceUpdateCanvases();
        
        // Now start the scale up animation for the newly positioned card
        if (cardToScaleUp != null)
        {
            cardToScaleUp.SetActive(true);
            cardToScaleUp.transform.localScale = Vector3.zero;
            UIBlock2D cardBlock = cardToScaleUp.GetComponent<UIBlock2D>();
            cardBlock.Size = new Length3(
                cardBlock.Size.X,
                Length.Percentage(0f),
                cardBlock.Size.Z
            );
            StartCoroutine(AnimateCardToVisible(cardToScaleUp, 0f));
        }
        
        // Wait for scale up animation to complete
        yield return new WaitForSeconds(cardIntroAnimationDuration);
        
        isScrolling = false;
    }
    
    private IEnumerator CoordinatedScrollAnimation()
    {
        // Identify which cards need to change
        GameObject cardToScaleUp = null;
        GameObject cardToScaleDown = null;
        
        // Find the card that should scale up (newly visible)
        for (int i = 0; i < visibleCardCount && i < cardHand.Count; i++)
        {
            if (!cardHand[i].activeInHierarchy)
            {
                cardToScaleUp = cardHand[i];
                break;
            }
        }
        
        // Find the card that should scale down (no longer visible)
        for (int i = visibleCardCount; i < cardHand.Count; i++)
        {
            if (cardHand[i].activeInHierarchy)
            {
                cardToScaleDown = cardHand[i];
                break;
            }
        }
        
        // Start the scale down animation first and wait for it to complete
        bool scaleDownFinished = false;
        if (cardToScaleDown != null)
        {
            StartCoroutine(AnimateCardToInvisibleAndDisable(cardToScaleDown, () => scaleDownFinished = true));
        }
        else
        {
            scaleDownFinished = true; // No scale down needed
        }
        
        // Wait for scale down to finish before updating hierarchy
        while (!scaleDownFinished)
        {
            yield return null;
        }
        
        // Now update hierarchy positions after scale down is complete
        for (int i = 0; i < cardHand.Count; i++)
        {
            cardHand[i].transform.SetSiblingIndex(i);
        }
        
        Canvas.ForceUpdateCanvases();
        
        // Now start the scale up animation for the newly visible card
        if (cardToScaleUp != null)
        {
            cardToScaleUp.SetActive(true);
            cardToScaleUp.transform.localScale = Vector3.zero;
            UIBlock2D cardBlock = cardToScaleUp.GetComponent<UIBlock2D>();
            cardBlock.Size = new Length3(
                cardBlock.Size.X,
                Length.Percentage(0f),
                cardBlock.Size.Z
            );
            StartCoroutine(AnimateCardToVisible(cardToScaleUp, 0f));
        }
        
        // Wait for scale up animation to complete
        yield return new WaitForSeconds(cardIntroAnimationDuration);
        
        isScrolling = false;
    }

    public void GenerateHand()
    {
        cardHand.Clear();

        // Example of generating a hand with 8 cards (more than visible range)
        for (int i = 0; i < 8; i++)
        {
            GameObject card = Instantiate(cardPrefab, transform);
            UIBlock2D cardBlock = card.GetComponent<UIBlock2D>();
            
            // Set initial size to 0 and scale to 0
            cardBlock.Size = new Length3(
                cardBlock.Size.X,
                Length.Percentage(0F),
                cardBlock.Size.Z
            );
            card.transform.localScale = Vector3.zero;
            
            // Assign random color to differentiate cards
            cardBlock.Color = new Color(
                Random.Range(0.3f, 1f),
                Random.Range(0.3f, 1f),
                Random.Range(0.3f, 1f),
                1f
            );
            
            // Name the card for easy identification
            card.name = $"Card_{i}";
            
            // Initially disable all cards except the first visibleCardCount
            card.SetActive(i < visibleCardCount);
            
            // Setup drag gestures for each card
            SetupCardDragGestures(card);
            
            cardHand.Add(card);
        }
        
        // Initialize visible range
        currentStartIndex = 0;
        StartCoroutine(InitializeVisibleCards());
    }
    
    private IEnumerator InitializeVisibleCards()
    {
        // Wait for Nova UI to settle
        Canvas.ForceUpdateCanvases();
        yield return null;
        
        // Reset to simple start state - no complex start index
        currentStartIndex = 0;
        
        // For initial setup, manually animate the first 5 cards to visible
        // since ReorderCardsInHierarchy() now only animates changes
        for (int i = 0; i < Mathf.Min(visibleCardCount, cardHand.Count); i++)
        {
            GameObject card = cardHand[i];
            card.transform.SetSiblingIndex(i);
            Debug.Log($"[CardHandManager] Initial animation for card {card.name}");
            StartCoroutine(AnimateCardToVisible(card, i * 0.1f)); // Stagger the initial animations
        }
        
        Debug.Log($"[CardHandManager] Initialized with cards: {string.Join(", ", cardHand.Take(visibleCardCount).Select(c => c.name))}");
    }
    
    private void UpdateVisibleCards(int newStartIndex)
    {
        int oldStartIndex = currentStartIndex;
        currentStartIndex = newStartIndex;
        
        Debug.Log($"[CardHandManager] Scrolling from index {oldStartIndex} to {newStartIndex}");
        
        // Reorder cards in hierarchy for Nova's auto layout
        ReorderCardsInHierarchy();
        
        // Animate cards that should become invisible
        for (int i = 0; i < cardHand.Count; i++)
        {
            bool wasVisible = i >= oldStartIndex && i < oldStartIndex + visibleCardCount;
            bool shouldBeVisible = i >= newStartIndex && i < newStartIndex + visibleCardCount;
            
            if (wasVisible && !shouldBeVisible)
            {
                // Card should disappear
                StartCoroutine(AnimateCardToInvisible(cardHand[i]));
            }
            else if (!wasVisible && shouldBeVisible)
            {
                // Card should appear
                StartCoroutine(AnimateCardToVisible(cardHand[i], 0f));
            }
        }
        
        // Reset scrolling flag after animation
        StartCoroutine(ResetScrollingFlag());
    }
    
    private void ReorderCardsInHierarchy()
    {
        // Track which cards were visible before reordering
        HashSet<GameObject> previouslyVisible = new HashSet<GameObject>();
        for (int i = 0; i < Mathf.Min(visibleCardCount, cardHand.Count); i++)
        {
            if (cardHand[i].activeInHierarchy)
                previouslyVisible.Add(cardHand[i]);
        }
        
        // Set hierarchy positions for ALL cards
        for (int i = 0; i < cardHand.Count; i++)
        {
            GameObject card = cardHand[i];
            card.transform.SetSiblingIndex(i);
        }
        
        // Force layout update after hierarchy changes
        Canvas.ForceUpdateCanvases();
        
        // Handle visibility and animations
        for (int i = 0; i < cardHand.Count; i++)
        {
            GameObject card = cardHand[i];
            bool shouldBeVisible = i < visibleCardCount;
            bool wasVisible = previouslyVisible.Contains(card);
            
            if (shouldBeVisible && !wasVisible)
            {
                // Card should become visible - enable it and animate in
                if (!card.activeInHierarchy)
                {
                    card.SetActive(true);
                    // Ensure it starts from zero scale for the animation
                    card.transform.localScale = Vector3.zero;
                    UIBlock2D cardBlock = card.GetComponent<UIBlock2D>();
                    cardBlock.Size = new Length3(
                        cardBlock.Size.X,
                        Length.Percentage(0f),
                        cardBlock.Size.Z
                    );
                    Debug.Log($"[CardHandManager] Enabled card {card.name} at position {i}");
                }
                Debug.Log($"[CardHandManager] Animating NEW card {card.name} to visible");
                StartCoroutine(AnimateCardToVisible(card, 0f));
            }
            else if (!shouldBeVisible && wasVisible)
            {
                // Card should become invisible - animate out then disable
                Debug.Log($"[CardHandManager] Animating OLD card {card.name} to invisible");
                StartCoroutine(AnimateCardToInvisibleAndDisable(card));
            }
        }
        
        Debug.Log($"[CardHandManager] Reordered hierarchy. Visible cards: {string.Join(", ", cardHand.Take(visibleCardCount).Select(c => c.name))}");
        Debug.Log($"[CardHandManager] Active cards: {string.Join(", ", cardHand.Where(c => c.activeInHierarchy).Select(c => c.name))}");
    }
    
    private IEnumerator ResetScrollingFlag()
    {
        yield return new WaitForSeconds(GetTotalAnimationDuration());
        isScrolling = false;
    }

    private IEnumerator AnimateCardToVisible(GameObject card, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        UIBlock2D cardBlock = card.GetComponent<UIBlock2D>();
        Canvas.ForceUpdateCanvases();
        yield return null;

        // Ensure we start from 0 scale and size
        card.transform.localScale = Vector3.zero;
        cardBlock.Size = new Length3(
            cardBlock.Size.X,
            Length.Percentage(0f),
            cardBlock.Size.Z
        );

        // Animate both scale and size to visible simultaneously
        float currentValue = 0f;
        
        DOTween.To(
            () => currentValue,
            value =>
            {
                // Animate size (height)
                cardBlock.Size = new Length3(
                    cardBlock.Size.X,
                    Length.Percentage(value),
                    cardBlock.Size.Z
                );
                
                // Animate scale simultaneously
                card.transform.localScale = Vector3.one * value;
            },
            1f,
            cardIntroAnimationDuration
        ).SetEase(Ease.OutCubic);

        Debug.Log($"[CardHandManager] Started simultaneous animation for {card.name} from 0 to 1");
    }
    
    private IEnumerator AnimateCardToInvisible(GameObject card)
    {
        UIBlock2D cardBlock = card.GetComponent<UIBlock2D>();
        Canvas.ForceUpdateCanvases();
        yield return null;
        
        // Start from current state (should be 1)
        float currentValue = 1f;
        
        DOTween.To(
            () => currentValue,
            value => {
                // Animate size (height)
                cardBlock.Size = new Length3(
                    cardBlock.Size.X,
                    Length.Percentage(value),
                    cardBlock.Size.Z
                );
                
                // Animate scale
                card.transform.localScale = Vector3.one * value;
            },
            0f,
            cardOutroAnimationDuration
        ).SetEase(Ease.InCubic);
        
        Debug.Log($"[CardHandManager] Started disappear animation for {card.name} from 100% to 0% with scale");
    }
    
    private IEnumerator AnimateCardToInvisibleAndDisable(GameObject card, System.Action onComplete = null)
    {
        UIBlock2D cardBlock = card.GetComponent<UIBlock2D>();
        Canvas.ForceUpdateCanvases();
        yield return null;
        
        // Start from current state (should be 1)
        float currentValue = 1f;
        
        bool finished = false;
        DOTween.To(
            () => currentValue,
            value => {
                // Animate size (height)
                cardBlock.Size = new Length3(
                    cardBlock.Size.X,
                    Length.Percentage(value),
                    cardBlock.Size.Z
                );
                
                // Animate scale simultaneously
                card.transform.localScale = Vector3.one * value;
            },
            0f,
            cardOutroAnimationDuration
        ).SetEase(Ease.InCubic).OnComplete(() => {
            // Disable the card after animation completes
            card.SetActive(false);
            Debug.Log($"[CardHandManager] Disabled card {card.name} after animation");
            finished = true;
            onComplete?.Invoke(); // Call the callback if provided
        });
        
        Debug.Log($"[CardHandManager] Started simultaneous disappear animation for {card.name} from 1 to 0");

        // Wait until animation is finished
        while (!finished)
            yield return null;
    }

    private IEnumerator AnimateCardHeight(UIBlock2D cardBlock, float delay)
    {
        // Wait for delay (stagger animations)
        yield return new WaitForSeconds(delay);
        
        // Force Nova UI layout calculation
        Canvas.ForceUpdateCanvases();
        yield return null; // Wait one frame for Nova to process
        
        // Animate Y percentage from 0 to 1 over 0.5 seconds
        float currentPercentage = 0f;
        DOTween.To(
            () => currentPercentage,
            percentage => {
                currentPercentage = percentage;
                cardBlock.Size = new Length3(
                    cardBlock.Size.X,
                    Length.Percentage(percentage),
                    cardBlock.Size.Z
                );
            },
            1f,
            0.5f
        ).SetEase(Ease.OutCubic);
    }
    
    #region PUBLIC METHODS
    
    public void ScrollToIndex(int index)
    {
        if (cardHand.Count <= visibleCardCount) return;
        if (isScrolling) return;
        
        // Handle looping for programmatic scrolling
        if (index < 0)
            index = cardHand.Count - visibleCardCount;
        else if (index > cardHand.Count - visibleCardCount)
            index = 0;
        
        isScrolling = true;
        UpdateVisibleCards(index);
    }
    
    public int GetCurrentStartIndex() => currentStartIndex;
    public int GetVisibleCardCount() => visibleCardCount;
    public List<GameObject> GetVisibleCards()
    {
        // Always return the first 'visibleCardCount' cards in the list
        List<GameObject> visibleCards = new List<GameObject>();
        for (int i = 0; i < Mathf.Min(visibleCardCount, cardHand.Count); i++)
        {
            visibleCards.Add(cardHand[i]);
        }
        return visibleCards;
    }
    
    [Button("🎨 Randomize Card Colors")]
    private void RandomizeCardColors()
    {
        foreach (GameObject card in cardHand)
        {
            UIBlock2D cardBlock = card.GetComponent<UIBlock2D>();
            if (cardBlock != null)
            {
                cardBlock.Color = new Color(
                    Random.Range(0.3f, 1f),
                    Random.Range(0.3f, 1f),
                    Random.Range(0.3f, 1f),
                    1f
                );
            }
        }
        Debug.Log("[CardHandManager] Randomized all card colors");
    }
    
    #endregion

    // ===============================
    // DRAG AND DROP FUNCTIONALITY
    // ===============================
    // Handles click-and-drag operations on individual cards:
    // - When a card is pressed, it scales up and can be dragged
    // - During drag, the next available card appears in its place
    // - When released, the dragged card returns to original position
    // - The replacement card scales back down and disappears
    
    private void SetupCardDragGestures(GameObject card)
    {
        UIBlock2D cardBlock = card.GetComponent<UIBlock2D>();
        if (cardBlock != null)
        {
            cardBlock.AddGestureHandler<Gesture.OnPress>(data => HandleCardPress(card, data));
            cardBlock.AddGestureHandler<Gesture.OnDrag>(data => HandleCardDrag(card, data));
            cardBlock.AddGestureHandler<Gesture.OnRelease>(data => HandleCardRelease(card, data));
        }
    }
    
    private void HandleCardPress(GameObject card, Gesture.OnPress pressData)
    {
        // Don't allow dragging during scroll animations or if not visible
        if (isScrolling || !card.activeInHierarchy) return;
        
        // Only allow dragging visible cards
        int cardIndex = cardHand.IndexOf(card);
        if (cardIndex >= visibleCardCount) return;
        
        draggedCard = card;
        draggedCardOriginalIndex = cardIndex;
        draggedCardOriginalPosition = card.transform.position;
        draggedCardOriginalScale = card.transform.localScale;
        draggedCardOriginalParent = card.transform.parent;
        
        // Scale up the dragged card slightly
        card.transform.localScale = draggedCardOriginalScale * dragScaleFactor;
        
        // Move card out of layout constraints to parent's parent (grandparent)
        Transform grandParent = draggedCardOriginalParent.parent;
        if (grandParent != null)
        {
            card.transform.SetParent(grandParent, true); // worldPositionStays = true
        }
        else
        {
            // Fallback: move to root canvas or scene
            Canvas rootCanvas = FindFirstObjectByType<Canvas>();
            if (rootCanvas != null)
            {
                card.transform.SetParent(rootCanvas.transform, true);
            }
        }
        
        // Bring to front for dragging (after reparenting)
        card.transform.SetAsLastSibling();
        
        // Show replacement card if available
        ShowReplacementCard();
        
        Debug.Log($"[CardHandManager] Started dragging {card.name}, moved to {card.transform.parent.name}");
    }
    
    private void HandleCardDrag(GameObject card, Gesture.OnDrag dragData)
    {
        if (draggedCard != card) return;
        
        // Update card position to follow mouse/touch
        // Nova UI provides world space position directly
        Vector3 worldPosition = dragData.PointerPositions.Current;
        
        // Since we moved the card out of the layout, we may need to adjust the position
        // depending on the coordinate space differences between the original parent and new parent
        card.transform.position = worldPosition;
    }
    
    private void HandleCardRelease(GameObject card, Gesture.OnRelease releaseData)
    {
        if (draggedCard != card) return;
        
        Debug.Log($"[CardHandManager] Released drag on {card.name}");
        
        // Create temporary spacer card to prepare space for returning card
        StartCoroutine(CreateTemporarySpacerAndReturn(card));
        
        // Hide replacement card
        HideReplacementCard();
        
        // Reset drag state
        draggedCard = null;
    }
    
    private void ShowReplacementCard()
    {
        // Find the next card that could fill the gap
        GameObject nextCard = FindNextAvailableCard();
        if (nextCard != null)
        {
            temporaryReplacementCard = nextCard;
            
            // Position it at the end of the visible cards (bottom of the hierarchy)
            // This means it should be at index (visibleCardCount - 1) to appear at the rightmost/bottom position
            int insertIndex = visibleCardCount - 1;
            nextCard.transform.SetSiblingIndex(insertIndex);
            
            // Prepare and animate in
            nextCard.SetActive(true);
            nextCard.transform.localScale = Vector3.zero;
            UIBlock2D cardBlock = nextCard.GetComponent<UIBlock2D>();
            cardBlock.Size = new Length3(
                cardBlock.Size.X,
                Length.Percentage(0f),
                cardBlock.Size.Z
            );
            StartCoroutine(AnimateCardToVisible(nextCard, 0f));
            
            Debug.Log($"[CardHandManager] Showing replacement card {nextCard.name} at the end of visible cards (index {insertIndex})");
        }
    }
    
    private void HideReplacementCard()
    {
        if (temporaryReplacementCard != null)
        {
            Debug.Log($"[CardHandManager] Hiding replacement card {temporaryReplacementCard.name}");
            
            // Animate out and disable the replacement card
            StartCoroutine(AnimateCardToInvisibleAndDisable(temporaryReplacementCard, () => {
                // Reset its hierarchy position to the end
                temporaryReplacementCard.transform.SetSiblingIndex(cardHand.IndexOf(temporaryReplacementCard));
                temporaryReplacementCard = null;
            }));
        }
    }
    
    private GameObject FindNextAvailableCard()
    {
        // Find the first inactive card that could be shown
        for (int i = visibleCardCount; i < cardHand.Count; i++)
        {
            if (!cardHand[i].activeInHierarchy)
            {
                return cardHand[i];
            }
        }
        return null;
    }
    
    private IEnumerator ReturnCardToPosition(GameObject card)
    {
        // Animate back to original position and scale first
        float duration = dragReturnDuration;
        Vector3 startPosition = card.transform.position;
        Vector3 startScale = card.transform.localScale;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth interpolation
            
            // Interpolate position and scale
            card.transform.position = Vector3.Lerp(startPosition, draggedCardOriginalPosition, t);
            card.transform.localScale = Vector3.Lerp(startScale, draggedCardOriginalScale, t);
            
            yield return null;
        }
        
        // Ensure exact final values
        card.transform.position = draggedCardOriginalPosition;
        card.transform.localScale = draggedCardOriginalScale;
        
        // Return card to original parent and hierarchy position
        card.transform.SetParent(draggedCardOriginalParent, false); // worldPositionStays = false to maintain layout
        card.transform.SetSiblingIndex(draggedCardOriginalIndex);
        
        Canvas.ForceUpdateCanvases();
        
        Debug.Log($"[CardHandManager] Returned {card.name} to original parent {draggedCardOriginalParent.name} at index {draggedCardOriginalIndex}");
    }
    
    private IEnumerator CreateTemporarySpacerAndReturn(GameObject card)
    {
        // Create invisible temporary spacer card at the original position
        GameObject spacerCard = Instantiate(cardPrefab, draggedCardOriginalParent);
        spacerCard.name = "TempSpacer_" + card.name;
        spacerCard.transform.SetSiblingIndex(draggedCardOriginalIndex);
        
        // Make it invisible but still take up layout space
        UIBlock2D spacerBlock = spacerCard.GetComponent<UIBlock2D>();
        spacerBlock.Color = new Color(0, 0, 0, 0); // Fully transparent
        
        // Start with zero width
        spacerBlock.Size = new Length3(
            Length.Percentage(0f), // Start with 0 width
            spacerBlock.Size.Y,    // Keep original height
            spacerBlock.Size.Z
        );
        
        spacerCard.SetActive(true);
        
        // Force layout update
        Canvas.ForceUpdateCanvases();
        yield return null;
        
        // Animate spacer width from 0 to 1 to create space
        float spacerDuration = dragReturnDuration * 0.3f; // Make it quick
        DOTween.To(
            () => 0f,
            value => {
                // Animate Nova UI size for layout spacing
                spacerBlock.Size = new Length3(
                    Length.Percentage(value), // Animate the layout width
                    spacerBlock.Size.Y,
                    spacerBlock.Size.Z
                );
            },
            1f,
            spacerDuration
        ).SetEase(Ease.OutCubic);
        
        Debug.Log($"[CardHandManager] Created temporary spacer for {card.name}");
        
        // Wait for spacer to expand, then start returning the original card
        yield return new WaitForSeconds(spacerDuration);
        
        // Now return the original card to position
        StartCoroutine(ReturnCardToPosition(card));
        
        // Wait for the return animation to complete
        yield return new WaitForSeconds(dragReturnDuration);
        
        // Remove the spacer card after the original card is back
        if (spacerCard != null)
        {
            Debug.Log($"[CardHandManager] Removing temporary spacer for {card.name}");
            Destroy(spacerCard);
        }
        
        // Force final layout update
        Canvas.ForceUpdateCanvases();
    }
    
    private void CancelCurrentDrag()
    {
        if (draggedCard != null)
        {
            // Return card to original parent first
            draggedCard.transform.SetParent(draggedCardOriginalParent, false); // worldPositionStays = false
            
            // Force return the card immediately without animation
            draggedCard.transform.position = draggedCardOriginalPosition;
            draggedCard.transform.localScale = draggedCardOriginalScale;
            draggedCard.transform.SetSiblingIndex(draggedCardOriginalIndex);
            
            // Hide replacement card
            HideReplacementCard();
            
            // Reset drag state
            draggedCard = null;
            
            Canvas.ForceUpdateCanvases();
            
            Debug.Log($"[CardHandManager] Cancelled drag and returned card to original parent");
        }
    }

    private void SetupNovaHoverGesture()
    {
        UIBlock2D uiBlock = GetComponent<UIBlock2D>();
        if (uiBlock != null)
        {
            // Add hover gesture handlers
            uiBlock.AddGestureHandler<Gesture.OnHover>(HandleHoverEnter);
            uiBlock.AddGestureHandler<Gesture.OnUnhover>(HandleHoverExit);
            Debug.Log("[CardHandManager] Nova UI hover gestures enabled");
        }
        else
        {
            Debug.LogWarning("[CardHandManager] No UIBlock2D found for gesture handling");
        }
    }
    
    private void HandleHoverEnter(Gesture.OnHover hoverData)
    {
        isPointerOver = true;
    }
    
    private void HandleHoverExit(Gesture.OnUnhover unhoverData)
    {
        isPointerOver = false;
    }

    private void OnDestroy()
    {
        // Clean up gesture handlers
        UIBlock2D uiBlock = GetComponent<UIBlock2D>();
        if (uiBlock != null)
        {
            uiBlock.RemoveGestureHandler<Gesture.OnHover>(HandleHoverEnter);
            uiBlock.RemoveGestureHandler<Gesture.OnUnhover>(HandleHoverExit);
        }
        
        // Clean up card drag gestures
        foreach (GameObject card in cardHand)
        {
            if (card != null)
            {
                UIBlock2D cardBlock = card.GetComponent<UIBlock2D>();
                if (cardBlock != null)
                {
                    // Note: Nova UI gesture handlers are automatically cleaned up when the object is destroyed
                    // but we could explicitly remove them here if needed
                }
            }
        }
    }
}

