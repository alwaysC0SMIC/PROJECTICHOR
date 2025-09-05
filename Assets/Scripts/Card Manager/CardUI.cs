using UnityEngine;
using Nova;
using TMPro;
using Sirenix.OdinInspector;
using DG.Tweening;
using System.Collections;


public class CardUI : MonoBehaviour
{
    //VARIABLES
    [SerializeField] private UIBlock Root;
    [SerializeField] private Interactable interactable;
    //[SerializeField] private NovaHoverGuard hoverGuard;

    [SerializeField] private float hiddenScale = 0f;
    [SerializeField] private float normalScale = 0.8f;
    [SerializeField] private float hoverScale = 1f;
    [SerializeField] private float animationDuration = 0.25f;

    #region TESTING

    [Title("Scale Testing", "", TitleAlignments.Centered)]

    [HorizontalGroup("Scale Tests")]
    [Button("🔽 Scale Down (Hidden)", ButtonSizes.Medium)]
    [PropertyTooltip("Animate card to hidden scale (0%)")]
    private void TestScaleDown()
    {
        ScaleToTarget(hiddenScale);
    }

    [HorizontalGroup("Scale Tests")]
    [Button("📐 Scale Normal", ButtonSizes.Medium)]
    [PropertyTooltip("Animate card to normal scale (80%)")]
    private void TestScaleNormal()
    {
        ScaleToTarget(normalScale);
    }

    [HorizontalGroup("Scale Tests")]
    [Button("🔼 Scale Hover", ButtonSizes.Medium)]
    [PropertyTooltip("Animate card to hover scale (100%)")]
    private void TestScaleHover()
    {
        ScaleToTarget(hoverScale);
    }

    #endregion


    void Start()
    {
        // // Add NovaHoverGuard if not already present
        // if (hoverGuard == null)
        // {
        //     hoverGuard = Root.gameObject.GetComponent<NovaHoverGuard>();
        //     if (hoverGuard == null)
        //     {
        //         hoverGuard = Root.gameObject.AddComponent<NovaHoverGuard>();
        //     }
        // }

        Root.AddGestureHandler<Gesture.OnHover>(OnHover);
        Root.AddGestureHandler<Gesture.OnUnhover>(OnUnHover);
        // Add click handler to consume click events and prevent camera panning
        Root.AddGestureHandler<Gesture.OnClick>(OnClick);
    }

    private void OnHover(Gesture.OnHover e)
    {
        // Scale up on hover
        ScaleToTarget(hoverScale, true);
    }

    private void OnUnHover(Gesture.OnUnhover e)
    {
        // Scale down on unhover
        ScaleToTarget(normalScale, true);
    }

    private void OnClick(Gesture.OnClick e)
    {
        // Handle card click here
        Debug.Log($"Card clicked: {gameObject.name}");
        
        // You can add card-specific click logic here
        // The click event is now consumed and won't reach the camera system
    }

    public void ScaleToTarget(float targetScale, bool enableInteraction = false, bool disableWhenInactive = false, System.Action onComplete = null)
    {
        if (Root != null)
        {
            DOTween.Kill(Root.transform);

            // Get the current size
            Length3 currentSize = Root.Size;

            // Animate the Y size to target scale
            DOTween.To(
                () => currentSize.Y.Percent, // Get current Y percentage
                value =>
                {
                    // Update the Y size with new percentage value
                    Root.Size = new Length3(
                        currentSize.X,
                        Length.Percentage(value),
                        currentSize.Z
                    );

                    // Force Nova UI to update layout
                    Canvas.ForceUpdateCanvases();
                },
                targetScale, // Target value
                animationDuration
            ).SetEase(Ease.OutCubic).OnComplete(() =>
            {

                if (enableInteraction)
                {
                    interactable.enabled = true;
                }
                else
                {
                    interactable.enabled = false;
                    if (disableWhenInactive)
                    {
                        gameObject.SetActive(false);
                    }
                }

                // Call the completion callback if provided
                onComplete?.Invoke();
            });
        }
    }

    public void ScaleDown()
    {
        ScaleToTarget(hiddenScale, false, true);
    }

    public void ScaleUp()
    {
        ScaleToTarget(normalScale, true);
    }
    
    // Coroutine versions with completion callbacks
    public System.Collections.IEnumerator ScaleDownCoroutine(System.Action onComplete = null)
    {
        bool animationFinished = false;
        
        ScaleToTarget(hiddenScale, false, true, () => {
            animationFinished = true;
            onComplete?.Invoke();
        });
        
        // Wait for animation to complete
        while (!animationFinished)
        {
            yield return null;
        }
    }

    public System.Collections.IEnumerator ScaleUpCoroutine(System.Action onComplete = null)
    {
        bool animationFinished = false;
        
        ScaleToTarget(normalScale, true, false, () => {
            animationFinished = true;
            onComplete?.Invoke();
        });
        
        // Wait for animation to complete
        while (!animationFinished)
        {
            yield return null;
        }
    }
    
    // Alternative: Callback versions for non-coroutine usage
    public void ScaleDownWithCallback(System.Action onComplete)
    {
        ScaleToTarget(hiddenScale, false, true, onComplete);
    }

    public void ScaleUpWithCallback(System.Action onComplete)
    {
        ScaleToTarget(normalScale, true, false, onComplete);
    }

    public void ForceScaleDown()
    { 
        if (Root != null)
        {
            // Kill any existing tweens on this Root
            DOTween.Kill(Root.transform);
            
            // Get the current size
            Length3 currentSize = Root.Size;
            
            // Immediately set Y size to hidden scale (0%)
            Root.Size = new Length3(
                currentSize.X,
                Length.Percentage(hiddenScale),
                currentSize.Z
            );
            
            // Disable interaction
            if (interactable != null)
            {
                interactable.enabled = false;
            }
            
            // Force Nova UI to update layout
            Canvas.ForceUpdateCanvases();
        }
    }
    
    public void ForceScaleUp()
    {
        if (Root != null)
        {
            // Kill any existing tweens on this Root
            DOTween.Kill(Root.transform);
            
            // Get the current size
            Length3 currentSize = Root.Size;
            
            // Immediately set Y size to normal scale
            Root.Size = new Length3(
                currentSize.X,
                Length.Percentage(normalScale),
                currentSize.Z
            );
            
            // Enable interaction
            if (interactable != null)
            {
                interactable.enabled = true;
            }
            
            // Force Nova UI to update layout
            Canvas.ForceUpdateCanvases();
        }
    }

    public void StopAllAnimations()
    {
        if (Root != null)
        {
            // Kill all DOTween animations on this Root's transform
            DOTween.Kill(Root.transform);
            
            // Also kill any tweens that might be targeting this gameObject
            DOTween.Kill(gameObject);
            DOTween.Kill(Root);
        }
    }

}
