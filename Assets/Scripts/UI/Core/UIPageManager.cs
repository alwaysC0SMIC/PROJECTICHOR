using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Nova;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class UIPageManager : MonoBehaviour
{
    #region ENUMS
    public enum PageAnimationMode { None, TweenAnimation, FadeWithClipMask, Both }
    #endregion

    #region PAGE IDENTITY

    [TitleGroup("Page Info")]
    [SerializeField] private Enum_UIMenuPage pageType = Enum_UIMenuPage.Gameplay;

    [TitleGroup("Page Root")]
    [SerializeField] private UIBlock2D pageRoot;

    [TitleGroup("Page Root")]
    [ShowIf("@useFullPageAnimation && (pageAnimationMode == PageAnimationMode.FadeWithClipMask || pageAnimationMode == PageAnimationMode.Both)")]
    [Required, SerializeField] private ClipMask clipMask;
    [SerializeField] private float clipFadeDuration = 0.25f;

    #endregion

    #region ANIMATION SETTINGS

    [TitleGroup("Page Animation Settings")]
    [SerializeField] private bool useFullPageAnimation = true;

    [TitleGroup("Page Animation Settings"), ShowIf(nameof(useFullPageAnimation))]
    [SerializeField] private PageAnimationMode pageAnimationMode = PageAnimationMode.TweenAnimation;

    // NEW: stagger toggle + duration
    [TitleGroup("Page Animation Settings")]
    [LabelText("Use Element Stagger")]
    [Tooltip("When enabled, a short delay is applied between enabling/disabling each UIElement.")]
    [SerializeField] private bool useElementStagger = true;

    [TitleGroup("Page Animation Settings")]
    [ShowIf(nameof(useElementStagger))]
    [LabelText("Element Stagger Duration (s)")]
    [MinValue(0f)]
    [SerializeField] private float elementStaggerDuration = 0.08f;

    [TitleGroup("Page Animation Settings")]
    [ShowIf("@useFullPageAnimation && (pageAnimationMode == PageAnimationMode.TweenAnimation || pageAnimationMode == PageAnimationMode.Both)")]
    [LabelText("Tween In")]
    [SerializeField] private TweenPreset tweenIn;

    [TitleGroup("Page Animation Settings")]
    [ShowIf("@useFullPageAnimation && (pageAnimationMode == PageAnimationMode.TweenAnimation || pageAnimationMode == PageAnimationMode.Both)")]
    [LabelText("Tween In Target (optional)")]
    [Tooltip("Optional Nova UIBlock target for Tween In. If null, will use pageRoot.")]
    [SerializeField] private UIBlock tweenInTarget;

    [TitleGroup("Page Animation Settings")]
    [ShowIf("@useFullPageAnimation && (pageAnimationMode == PageAnimationMode.TweenAnimation || pageAnimationMode == PageAnimationMode.Both)")]
    [LabelText("Tween Out")]
    [SerializeField] private TweenPreset tweenOut;

    [TitleGroup("Page Animation Settings")]
    [ShowIf("@useFullPageAnimation && (pageAnimationMode == PageAnimationMode.TweenAnimation || pageAnimationMode == PageAnimationMode.Both)")]
    [LabelText("Tween Out Target (optional)")]
    [Tooltip("Optional Nova UIBlock target for Tween Out. If null, will use pageRoot.")]
    [SerializeField] private UIBlock tweenOutTarget;

    [TitleGroup("Page Animation Settings/Sequence Control")]
    [ShowIf(nameof(useFullPageAnimation))]
    [LabelText("Run Intro Sequentially")]
    [SerializeField] private bool runIntroSequentially = true;

    [TitleGroup("Page Animation Settings/Sequence Control")]
    [ShowIf(nameof(useFullPageAnimation))]
    [LabelText("Run Outro Sequentially")]
    [SerializeField] private bool runOutroSequentially = false;

    #endregion

    #region PAGE ELEMENTS

    [TitleGroup("Page Elements")]
    [SerializeField] private List<UIElement> UIPageElements = new();

    // -------- INTRO (instant) --------
    [TitleGroup("Page Elements/Intro (Instant)")]
    [LabelText("Make Elements Instant (Intro)")]
    [Tooltip("If enabled, all UIElements will enable immediately on intro (no element animations).")]
    [SerializeField] private bool elementsInstantOnIntro = false;

    [TitleGroup("Page Elements/Intro (Instant)")]
    [ShowIf(nameof(elementsInstantOnIntro))]
    [LabelText("Toggle Interactable (Intro)")]
    [SerializeField] private bool instantToggleInteractableIntro = true;

    [TitleGroup("Page Elements/Intro (Instant)")]
    [ShowIf(nameof(elementsInstantOnIntro))]
    [LabelText("Trigger Events (Intro)")]
    [SerializeField] private bool instantTriggerEventsIntro = true;

    [TitleGroup("Page Elements/Intro (Instant)")]
    [ShowIf(nameof(elementsInstantOnIntro))]
    [LabelText("Play Audio (Intro)")]
    [SerializeField] private bool instantPlayAudioIntro = false;

    // -------- OUTRO (instant) --------
    [TitleGroup("Page Elements/Outro (Instant)")]
    [LabelText("Make Elements Instant (Outro)")]
    [Tooltip("If enabled, all UIElements will disable immediately on outro (no element animations).")]
    [SerializeField] private bool elementsInstantOnOutro = false;

    [TitleGroup("Page Elements/Outro (Instant)")]
    [ShowIf(nameof(elementsInstantOnOutro))]
    [LabelText("Toggle Interactable (Outro)")]
    [SerializeField] private bool instantToggleInteractableOutro = true;

    [TitleGroup("Page Elements/Outro (Instant)")]
    [ShowIf(nameof(elementsInstantOnOutro))]
    [LabelText("Trigger Events (Outro)")]
    [SerializeField] private bool instantTriggerEventsOutro = true;

    [TitleGroup("Page Elements/Outro (Instant)")]
    [ShowIf(nameof(elementsInstantOnOutro))]
    [LabelText("Play Audio (Outro)")]
    [SerializeField] private bool instantPlayAudioOutro = false;

    #endregion

    #region EVENTS

    [TitleGroup("Page Events")]
    [SerializeField] private UnityEvent onPageOpen;

    [TitleGroup("Page Events")]
    [SerializeField] private UnityEvent onPageClose;

    #endregion

    #region EVENT BINDING

    private EventBinding<UpdateUIPageEvent> updateUIPage;

    private void OnEnable()
    {
        updateUIPage = new EventBinding<UpdateUIPageEvent>(UpdateUIPage);
        EventBus<UpdateUIPageEvent>.Register(updateUIPage);
    }

    private void OnDisable()
    {
        EventBus<UpdateUIPageEvent>.Deregister(updateUIPage);
    }

    #endregion

    #region PAGE FLOW

    private void Start()
    {
        SetupPage();

        if (pageType != Enum_UIMenuPage.Gameplay)
        {
            StartCoroutine(HandlePageTransition(false));
        }
    }

    private void UpdateUIPage(UpdateUIPageEvent target)
    {
        bool enable = pageType == target.uiPage;

        if (enable) onPageOpen?.Invoke();
        else onPageClose?.Invoke();

        StartCoroutine(HandlePageTransition(enable));
    }

    private IEnumerator HandlePageTransition(bool enable)
    {
        if (pageRoot != null && enable)
            pageRoot.Visible = true;

        if (enable)
        {
            // INTRO: page-level first, then elements
            if (useFullPageAnimation && runIntroSequentially)
            {
                if (ShouldDoFade())  yield return HandleClipFade(true);
                if (ShouldDoTween()) yield return HandleTween(true);
                yield return HandleElements(true);
            }
            else
            {
                List<IEnumerator> coroutines = new();
                if (useFullPageAnimation && ShouldDoFade())  coroutines.Add(HandleClipFade(true));
                if (useFullPageAnimation && ShouldDoTween()) coroutines.Add(HandleTween(true));
                coroutines.Add(HandleElements(true));
                yield return WaitForAll(coroutines.ToArray());
            }
        }
        else
        {
            // OUTRO: disable elements FIRST, then page outro animations.
            yield return HandleElements(false);

            if (useFullPageAnimation)
            {
                if (runOutroSequentially)
                {
                    if (ShouldDoTween()) yield return HandleTween(false);
                    if (ShouldDoFade())  yield return HandleClipFade(false);
                }
                else
                {
                    List<IEnumerator> coroutines = new();
                    if (ShouldDoFade())  coroutines.Add(HandleClipFade(false));
                    if (ShouldDoTween()) coroutines.Add(HandleTween(false));
                    yield return WaitForAll(coroutines.ToArray());
                }
            }

            if (pageRoot != null)
                pageRoot.Visible = false;
        }
    }

    private bool ShouldDoFade() =>
        pageAnimationMode == PageAnimationMode.FadeWithClipMask || pageAnimationMode == PageAnimationMode.Both;

    private bool ShouldDoTween() =>
        pageAnimationMode == PageAnimationMode.TweenAnimation || pageAnimationMode == PageAnimationMode.Both;

    private IEnumerator HandleElements(bool enable)
    {
        // Choose instant config based on direction (intro/outro)
        bool useInstant =  enable ? elementsInstantOnIntro : elementsInstantOnOutro;
        bool toggleInt  =  enable ? instantToggleInteractableIntro : instantToggleInteractableOutro;
        bool trigEvents =  enable ? instantTriggerEventsIntro      : instantTriggerEventsOutro;
        bool playAudio  =  enable ? instantPlayAudioIntro          : instantPlayAudioOutro;

        // Instant path: immediate enable/disable; no staggering
        if (useInstant)
        {
            foreach (var element in UIPageElements)
            {
                if (element == null) continue;

                if (enable && !element.isEnabled)
                    element.EnableElementImmediate(toggleInt, trigEvents, playAudio);
                else if (!enable && element.isEnabled)
                    element.DisableElementImmediate(toggleInt, trigEvents, playAudio);
            }
            yield break;
        }

        // Animated path; stagger only if enabled
        foreach (var element in UIPageElements)
        {
            if (element != null && element.isEnabled != enable)
            {
                if (enable) element.EnableElement();
                else        element.DisableElement();

                if (useElementStagger && elementStaggerDuration > 0f)
                    yield return new WaitForSeconds(elementStaggerDuration);
            }
        }
    }

    private IEnumerator HandleTween(bool enable)
    {
        if (pageRoot == null) yield break;

        var targetTween = enable ? tweenIn : tweenOut;
        var targetUIBlock = enable ? tweenInTarget : tweenOutTarget;
        
        if (targetTween != null)
        {
            Tween tween = null;
            
            // Determine the target to use - custom target or fallback to pageRoot
            UIBlock uiBlockTarget = targetUIBlock != null ? targetUIBlock : pageRoot;
            
            // Use Transform target for tween types (Move, Scale, Rotate, Fade)
            Transform transformTarget = uiBlockTarget != null ? uiBlockTarget.transform : pageRoot.transform;

            if (targetTween.tweenType == TweenPreset.TweenType.NovaScale || targetTween.tweenType == TweenPreset.TweenType.NovaPosition)
            {
                tween = targetTween.ApplyTween(uiBlockTarget);
            }
            else
            { 
                tween = targetTween.ApplyTween(transformTarget);
            }

            if (tween != null)
                yield return tween.WaitForCompletion();
        }
    }

    private IEnumerator HandleClipFade(bool enable)
    {
        if (clipMask == null) yield break;

        float startAlpha = enable ? 0f : 1f;
        float endAlpha   = enable ? 1f : 0f;

        Color tint = new Color(1, 1, 1, startAlpha);
        DOTween.To(() => tint.a, x =>
        {
            tint.a = x;
            clipMask.Tint = tint;
        }, endAlpha, clipFadeDuration).SetTarget(this);

        yield return new WaitForSeconds(clipFadeDuration);
    }

    private IEnumerator WaitForAll(params IEnumerator[] coroutines)
    {
        // Filter out nulls
        List<IEnumerator> list = new List<IEnumerator>();
        foreach (var c in coroutines)
            if (c != null) list.Add(c);

        if (list.Count == 0) yield break;

        int completed = 0;
        foreach (var routine in list)
        {
            StartCoroutine(WrapCoroutine(routine, () => completed++));
        }
        yield return new WaitUntil(() => completed == list.Count);
    }

    private IEnumerator WrapCoroutine(IEnumerator coroutine, Action onComplete)
    {
        yield return coroutine;
        onComplete?.Invoke();
    }

    #endregion

    #region HELPERS

    [Button("▶ Open Page")]
    private void SimulateOpen() 
    {
        //Debug.Log($"[UIPageManager] Raising UpdateUIPageEvent for: {pageType}");
        EventBus<UpdateUIPageEvent>.Raise(new UpdateUIPageEvent { uiPage = pageType });
    }

    [Button("⏹ Close Page")]
    private void SimulateClose() 
    {
        //Debug.Log($"[UIPageManager] Raising UpdateUIPageEvent for: None");
        EventBus<UpdateUIPageEvent>.Raise(new UpdateUIPageEvent { uiPage = Enum_UIMenuPage.None });
    }

#if UNITY_EDITOR
    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("📄 Page Type")]
    private Enum_UIMenuPage Debug_PageType => pageType;

    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("📊 Event Bus Status")]
    [DisplayAsString]
    private string Debug_EventBusStatus => 
        $"Registered: {(updateUIPage != null ? "Yes" : "No")} | " +
        $"Page: {pageType} | " +
        $"Time: {System.DateTime.Now:HH:mm:ss}";
#endif

    private void SetupPage()
    {
        // Custom per-page setup here
    }

    #endregion
}

[Serializable]
public enum Enum_UIMenuPage
{
    None,
    Gameplay,
    PlayerProfile,
    Settings,
    Build,
    Dialogue,
    Quests,
    ShopPage,
    Inventory
}
