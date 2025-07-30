using AllIn1SpringsToolkit;
using DG.Tweening;
using Nova;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[InfoBox(
    "BounceInteractable wires Nova Interactable + UIBlock to either:\n" +
    "• Spring-driven hover feedback (TransformSpringComponent)\n" +
    "• Tween-driven hover feedback (TweenPreset list)\n\n" +
    "HOW IT WORKS\n" +
    "• On Hover: Plays HoverAnimation (spring OR tween based on Interaction Animation).\n" +
    "• On Unhover: Plays ExitHoverAnimation to return to rest.\n" +
    "• On Click: Raises UI click sound, calls OnClick() for your custom behavior.\n\n" +
    "REQUIREMENTS\n" +
    "• Same GameObject must have: Interactable, TransformSpringComponent (for spring mode), and UIBlock.\n" +
    "• Tween mode requires TweenPreset assets.\n\n" +
    "TIPS\n" +
    "• Use 'Spring Settings' to configure the hover pop scale.\n" +
    "• Use 'Tween Interaction Settings' lists to define custom hover/exit animations.",
    InfoMessageType = InfoMessageType.None, VisibleIf = "@true")]

[RequireComponent(typeof(Interactable), typeof(TransformSpringComponent))]
[DisallowMultipleComponent]
public abstract class BounceInteractable : MonoBehaviour
{
    public enum InteractionAnimationType
    {
        [LabelText("Use Spring (TransformSpringComponent)")]
        UseSpring,

        [LabelText("Use Tween (DOTween via TweenPreset list)")]
        UseTween
    }

    public enum PlayMode
    {
        [LabelText("Parallel (Join)")]
        Parallel,

        [LabelText("Sequential (Append)")]
        Sequential
    }

    #region UI & COMPONENTS

    [TitleGroup("Interaction Setup")]
    [ShowInInspector, ReadOnly]
    [PropertyTooltip("The Nova UIBlock on this GameObject. Autocached at edit-time and runtime.")]
    public UIBlock Root { get; private set; }

    [TitleGroup("Interaction Setup")]
    [ShowInInspector, ReadOnly]
    [PropertyTooltip("The Nova Interactable on this GameObject. Used to receive hover/click gestures.")]
    public Interactable interactable { get; private set; }

    [TitleGroup("Interaction Setup")]
    [ShowIf("@interactionAnimation == InteractionAnimationType.UseSpring")]
    [SerializeField, Tooltip("TransformSpringComponent used when Interaction Animation = Use Spring.\n" +
                             "Automatically fetched on this GameObject if not assigned.")]
    public TransformSpringComponent springComponent;

    [TitleGroup("Interaction Setup")]
    [SerializeField, Tooltip("Select which animation system to use for hover/exit:\n" +
                             "• Use Spring: drives TransformSpringComponent (pop/settle).\n" +
                             "• Use Tween: plays DOTween tweens from the provided TweenPreset lists.")]
    private InteractionAnimationType interactionAnimation = InteractionAnimationType.UseSpring;

    #endregion

    #region Spring Settings

    [TitleGroup("Spring Settings"), ShowIf("@interactionAnimation == InteractionAnimationType.UseSpring")]
    [SerializeField, Tooltip("Target scale value applied to the spring when hovering.\n" +
                             "Example: 1.1 = 10% pop. On exit, scale returns to 1.0.\n" +
                             "Tune spring settings on the TransformSpringComponent itself for feel.")]
    public float scaleSpringStrength = 1.1f;

    // If you re-enable rotation, add a tooltip like below:
    // [TitleGroup("Spring Settings"), ShowIf("@interactionAnimation == InteractionAnimationType.UseSpring")]
    // [SerializeField, Tooltip("Z-rotation target (degrees) while hovering. Negative values tilt clockwise.\n" +
    //                          "Set back to 0 on exit to return to identity.")]
    // public float rotationSpringStrength = -3f;

    #endregion

    #region Tween Interaction Settings (Lists)

    [TitleGroup("Tween Interaction Settings")]
    [ShowIf("@interactionAnimation == InteractionAnimationType.UseTween")]
    [LabelText("Hover Play Mode")]
    [SerializeField, Tooltip("How to play the 'Hover Tween Presets' list:\n" +
                             "• Parallel: All tweens Join into one sequence.\n" +
                             "• Sequential: Tweens Append one after another.")]
    private PlayMode hoverPlayMode = PlayMode.Parallel;

    [TitleGroup("Tween Interaction Settings")]
    [ShowIf("@interactionAnimation == InteractionAnimationType.UseTween")]
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
    [LabelText("Hover Tween Presets")]
    [SerializeField, Tooltip("Tweens to play when the pointer enters (hover).\n" +
                             "Each TweenPreset usually targets this.transform (unless overridden in the preset).")]
    private List<TweenPreset> hoverTweenPresets = new();

    [TitleGroup("Tween Interaction Settings")]
    [ShowIf("@interactionAnimation == InteractionAnimationType.UseTween")]
    [LabelText("Exit Play Mode")]
    [SerializeField, Tooltip("How to play the 'Exit Tween Presets' list:\n" +
                             "• Parallel: All tweens Join into one sequence.\n" +
                             "• Sequential: Tweens Append one after another.")]
    private PlayMode exitPlayMode = PlayMode.Parallel;

    [TitleGroup("Tween Interaction Settings")]
    [ShowIf("@interactionAnimation == InteractionAnimationType.UseTween")]
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
    [LabelText("Exit Tween Presets")]
    [SerializeField, Tooltip("Tweens to play when the pointer exits (unhover).\n" +
                             "Use this to return the element to a resting state.")]
    private List<TweenPreset> exitTweenPresets = new();

    #endregion

    #region Audio

    [TitleGroup("Audio")]
    [SerializeField, Tooltip("Sound triggered on click. Raised via EventBus<AudioEvent>.")]
    public AudioTrigger clickSound = AudioTrigger.UI_Click;

    [TitleGroup("Audio")]
    [SerializeField, Tooltip("Sound triggered when hover begins. Raised via EventBus<AudioEvent>.")]
    public AudioTrigger hoverSound = AudioTrigger.UI_Hover;

    [TitleGroup("Audio")]
    [SerializeField, Tooltip("Sound triggered when hover ends. Raised via EventBus<AudioEvent>.")]
    public AudioTrigger exitHoverSound = AudioTrigger.UI_ExitHover;

    #endregion

    #region EDITOR AUTOCACHE

#if UNITY_EDITOR
    private void OnValidate()
    {
        // KEEP REFERENCES FRESH IN EDIT MODE
        AutoCacheComponents();
        // No functional change here; tooltips only
    }
#endif

    private void Reset()
    {
        // RUNS WHEN THE COMPONENT IS ADDED OR RESET
        AutoCacheComponents();
    }

    [Tooltip("Autocaches Root (UIBlock), Interactable, and Spring if not assigned.\n" +
             "Called in editor (OnValidate) and at runtime (Start).")]
    private void AutoCacheComponents()
    {
        if (Root == null) Root = GetComponent<UIBlock>();
        if (interactable == null) interactable = GetComponent<Interactable>();
        if (springComponent == null) springComponent = GetComponent<TransformSpringComponent>();
    }

    #endregion

    #region Unity

    private void Start()
    {
        // SAFETY: FINALIZE AUTOCACHE AT RUNTIME
        AutoCacheComponents();

        // Enable spring component only if we're in spring mode (keeps it off for tween mode)
        if (springComponent != null)
            springComponent.enabled = interactionAnimation == InteractionAnimationType.UseSpring;

        // Raise DOTween internal capacities (optional headroom to avoid allocations)
        DOTween.SetTweensCapacity(1000, 4000);

        // GESTURE HOOKS
        if (Root != null)
        {
            Root.AddGestureHandler<Gesture.OnClick>(Click);
            Root.AddGestureHandler<Gesture.OnHover>(BeginHover);
            Root.AddGestureHandler<Gesture.OnUnhover>(ExitHover);
        }
        else
        {
            Debug.LogWarning($"{nameof(BounceInteractable)} on {name}: UIBlock (Root) is missing.");
        }

        OnStart();
    }

    private void OnDestroy()
    {
        // Kill tweens bound to this transform target when destroyed
        DOTween.Kill(transform);
    }

    #endregion

    #region Interaction Hooks

    [Tooltip("Optional: override to run logic after Start() has finished wiring up handlers.")]
    public virtual void OnStart() { }

    [Tooltip("Nova OnClick handler. Plays click SFX and calls OnClick().")]
    public virtual void Click(Gesture.OnClick evt)
    {
        EventBus<AudioEvent>.Raise(new AudioEvent(clickSound));
        OnClick();
    }

    [Tooltip("Nova OnRelease handler (alias for Click). Plays click SFX and calls OnClick().")]
    public virtual void Press(Gesture.OnRelease evt)
    {
        EventBus<AudioEvent>.Raise(new AudioEvent(clickSound));
        OnClick();
    }

    [Tooltip("Nova OnHover handler. Starts hover animation (spring OR tween based on Interaction Animation).")]
    public virtual void BeginHover(Gesture.OnHover evt)
    {
        EventBus<AudioEvent>.Raise(new AudioEvent(hoverSound));
        HoverAnimation();
    }

    [Tooltip("Nova OnUnhover handler. Starts exit animation (spring OR tween based on Interaction Animation).")]
    public virtual void ExitHover(Gesture.OnUnhover evt)
    {
        EventBus<AudioEvent>.Raise(new AudioEvent(exitHoverSound));
        ExitHoverAnimation();
    }

    #endregion

    #region Animations

    [Tooltip("Executed when hover begins. Uses spring or tweens depending on Interaction Animation.")]
    public virtual void HoverAnimation()
    {
        if (interactionAnimation == InteractionAnimationType.UseSpring && springComponent != null)
        {
            // Spring path
            springComponent.SetTargetScale(scaleSpringStrength);
            // If you use rotation spring as well, set it here (see commented field above).
        }
        else if (interactionAnimation == InteractionAnimationType.UseTween)
        {
            // Tween path
            PlayPresets(hoverTweenPresets, hoverPlayMode);
        }
    }

    [Tooltip("Executed when hover ends. Returns to rest using spring or tweens depending on Interaction Animation.")]
    public virtual void ExitHoverAnimation()
    {
        if (interactionAnimation == InteractionAnimationType.UseSpring && springComponent != null)
        {
            // Spring path: return to neutral
            springComponent.SetTargetScale(1f);
            // springComponent.SetTargetRotation(Quaternion.identity);
        }
        else if (interactionAnimation == InteractionAnimationType.UseTween)
        {
            // Tween path
            PlayPresets(exitTweenPresets, exitPlayMode);
        }
    }

    /// <summary>
    /// Plays a list of TweenPresets either in Parallel or Sequentially against this.transform.
    /// Any preset that returns null (e.g., requires a different target) is skipped.
    /// </summary>
    [Tooltip("Utility: Builds a DOTween Sequence from a list of TweenPresets and plays it.\n" +
             "Parallel → Join; Sequential → Append. Null/invalid presets are skipped.")]
    private void PlayPresets(List<TweenPreset> presets, PlayMode mode)
    {
        if (presets == null || presets.Count == 0) return;

        // Kill currently running tweens on this transform to avoid overlap/fighting
        DOTween.Kill(transform);

        Sequence seq = DOTween.Sequence().SetTarget(transform);
        bool addedAny = false;

        foreach (var preset in presets)
        {
            if (preset == null) continue;

            // By default, apply to this.transform
            Tween t = preset.ApplyTween(transform);
            if (t == null) continue; // skip unsupported target/preset combos

            addedAny = true;

            if (mode == PlayMode.Sequential) seq.Append(t);
            else seq.Join(t);
        }

        // If nothing valid was added (all null), don't play an empty sequence
        if (!addedAny)
        {
            seq.Kill();
            return;
        }

        // Sequences auto-play by default, but ensuring won't hurt:
        seq.Play();
    }

    #endregion

    [Tooltip("Override in derived classes to define click behavior for this interactable.\n" +
             "Base implementation returns the spring to scale 1.")]
    public virtual void OnClick()
    {
        if (springComponent != null)
        {
            springComponent.SetTargetScale(1f);
            // springComponent.SetVelocityRotation(new Vector3(0f, 0f, rotationSpringStrength));
        }
    }
    //TEST
}
