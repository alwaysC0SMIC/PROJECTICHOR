using System;
using System.Collections.Generic;
using AllIn1SpringsToolkit;
using DG.Tweening;
using Nova;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(UIBlock))]
[DisallowMultipleComponent]
public class UIElement : MonoBehaviour
{
    public enum PlayMode { Parallel, Sequential }

    [Serializable]
    public struct TweenItem
    {
        [LabelText("🎬 Preset")]
        public TweenPreset preset;

        [LabelText("🎯 Target Override (optional)")]
        [Tooltip(
            "Move/Scale/Rotate → a Transform (or any Component to use its transform).\n" +
            "Fade → a CanvasGroup (or any Component that has one).")]
        public UnityEngine.Object targetOverride;
    }

    #region STATE & COMPONENTS

    [TitleGroup("State"), ReadOnly, LabelText("✅ Enabled?")]
    public bool isEnabled = true;

    [TitleGroup("State"), LabelText("🎭 Play Animations")]
    [Tooltip("When enabled, show/hide will play animations. When disabled, show/hide will be instant.")]
    public bool playAnimations = true;

    [TitleGroup("State"), LabelText("👁 Hide When Disabled")]
    [Tooltip("When enabled, disabling the element will make it invisible. When disabled, the element stays visible but becomes non-interactive.")]
    public bool hideWhenDisabled = true;

    [TitleGroup("State"), ShowInInspector, ReadOnly, LabelText("📦 Root")]
    public UIBlock Root { get; private set; }

    [TitleGroup("State"), ShowInInspector, ReadOnly, LabelText("🖱 Interactable")]
    public Interactable interactable { get; private set; }

    [TitleGroup("State"), ShowInInspector, ReadOnly, LabelText("🌀 Spring Component")]
    public TransformSpringComponent springComponent;

#if UNITY_EDITOR
    [TitleGroup("Components"), Button("↻ Auto‑Assign (Editor)")]
    private void EditorAutoAssign() => AutoCacheComponents();
#endif

    #endregion

    #region ANIMATION SETTINGS

    [TitleGroup("Enable Tweens")]
    [LabelText("▶ Play Mode")]
    [SerializeField] private PlayMode enablePlayMode = PlayMode.Parallel;

    [TitleGroup("Enable Tweens")]
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
    [LabelText("🎞 Enable Presets")]
    [SerializeField] private List<TweenItem> enableTweens = new();

    [TitleGroup("Disable Tweens")]
    [LabelText("⏹ Play Mode")]
    [SerializeField] private PlayMode disablePlayMode = PlayMode.Parallel;

    [TitleGroup("Disable Tweens")]
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
    [LabelText("🎞 Disable Presets")]
    [SerializeField] private List<TweenItem> disableTweens = new();

    #endregion

    #region EVENTS & AUDIO

    [TitleGroup("Events"), SerializeField, LabelText("📣 On Enable")]
    public UnityEvent OnEnable;

    [TitleGroup("Events"), SerializeField, LabelText("📣 On Enable Completion")]
    public UnityEvent OnEnableFinish;

    [TitleGroup("Events"), SerializeField, LabelText("📣 On Disable")]
    public UnityEvent OnDisable;

    [TitleGroup("Events"), SerializeField, LabelText("📣 On Disable Completion")]
    public UnityEvent OnDisableFinish;

    [TitleGroup("Audio"), SerializeField, LabelText("🔊 Show SFX")]
    public AudioTrigger showSound = AudioTrigger.UI_Show;

    [TitleGroup("Audio"), SerializeField, LabelText("🔇 Hide SFX")]
    public AudioTrigger hideSound = AudioTrigger.UI_Hide;

    #endregion

    #region EDITOR HOOKS

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoCacheComponents();
    }
#endif

    private void Reset()
    {
        AutoCacheComponents();
    }

    #endregion

    #region UNITY

    private void Awake()
    {
        AutoCacheComponents();
    }

    private void OnDestroy()
    {
        DOTween.Kill(transform);
        DOTween.Kill(this);
    }

    #endregion

    #region PUBLIC ACTIONS (ANIMATED)

    [Button("▶ Enable", ButtonSizes.Large), GUIColor(0.55f, 0.9f, 0.55f)]
    public virtual void EnableElement()
    {
        if (!playAnimations)
        {
            EnableElementImmediate(toggleInteractable: true, triggerEvents: true, playAudio: true);
            return;
        }

        // Animated version
        EnableElementAnimated();
    }

    [Button("⏹ Disable", ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.5f)]
    public virtual void DisableElement()
    {
        if (!playAnimations)
        {
            DisableElementImmediate(toggleInteractable: true, triggerEvents: true, playAudio: true);
            return;
        }

        // Animated version
        DisableElementAnimated();
    }

    /// <summary>
    /// Enables the element with animations.
    /// </summary>
    private void EnableElementAnimated()
    {
        // Prepare spring to neutral (no animation here)
        if (springComponent != null)
        {
            springComponent.SetTargetScale(1f);
            springComponent.SetTargetRotation(Quaternion.identity);
        }

        // Prep visual state
        if (Root != null)
        {
            Root.Visible = true;
        }

        DOTween.Kill(transform);

        // Set initial states before playing tweens
        SetInitialStatesForEnableTweens();

        // Play enable list
        PlayPresetList(enableTweens, enablePlayMode, onComplete: () =>
        {
            EnableComponents();
            OnEnableFinish?.Invoke();
        });

        // SFX & Events
        EventBus<AudioEvent>.Raise(new AudioEvent(showSound));
        OnEnable?.Invoke();
        isEnabled = true;
    }

    /// <summary>
    /// Disables the element with animations.
    /// </summary>
    private void DisableElementAnimated()
    {
        if (interactable != null) interactable.enabled = false;

        DOTween.Kill(transform);

        bool hadPresets = disableTweens != null && disableTweens.Count > 0;

        if (hideWhenDisabled)
        {
            if (hadPresets)
            {
                PlayPresetList(disableTweens, disablePlayMode, onComplete: () =>
                {
                    if (Root != null) Root.Visible = false;
                    // Don't reset scale here - let the tween handle final state
                    OnDisableFinish?.Invoke();
                });
            }
            else
            {
                var t = Root != null
                    ? Root.transform
                        .DOScale(0f, 0.25f)
                        .SetEase(Ease.InCubic)
                    : null;

                if (t != null)
                {
                    t.OnComplete(() => { 
                        if (Root != null) Root.Visible = false; 
                        // Don't reset scale here either
                        OnDisableFinish?.Invoke();
                    });
                }
                else
                {
                    if (Root != null) Root.Visible = false;
                    OnDisableFinish?.Invoke();
                }
            }
        }
        else
        {
            // Don't hide the element, but still play disable animations if they exist
            if (hadPresets)
            {
                PlayPresetList(disableTweens, disablePlayMode, onComplete: () =>
                {
                    OnDisableFinish?.Invoke();
                });
            }
            else
            {
                // If no presets and not hiding, trigger completion immediately
                OnDisableFinish?.Invoke();
            }
        }

        EventBus<AudioEvent>.Raise(new AudioEvent(hideSound));
        OnDisable?.Invoke();
        isEnabled = false;
    }

    #endregion

    #region PUBLIC ACTIONS (FORCED MODES)

    /// <summary>
    /// Forces the element to enable with animations, regardless of the playAnimations setting.
    /// </summary>
    [Button("▶ Force Enable Animated", ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 0.4f)]
    public void EnableElementForceAnimated()
    {
        EnableElementAnimated();
    }

    /// <summary>
    /// Forces the element to disable with animations, regardless of the playAnimations setting.
    /// </summary>
    [Button("⏹ Force Disable Animated", ButtonSizes.Medium), GUIColor(0.8f, 0.4f, 0.4f)]
    public void DisableElementForceAnimated()
    {
        DisableElementAnimated();
    }

    /// <summary>
    /// Forces the element to enable instantly, regardless of the playAnimations setting.
    /// </summary>
    [Button("▶ Force Enable Instant", ButtonSizes.Medium), GUIColor(0.6f, 1f, 0.6f)]
    public void EnableElementForceInstant()
    {
        EnableElementImmediate(toggleInteractable: true, triggerEvents: true, playAudio: true);
    }

    /// <summary>
    /// Forces the element to disable instantly, regardless of the playAnimations setting.
    /// </summary>
    [Button("⏹ Force Disable Instant", ButtonSizes.Medium), GUIColor(1f, 0.6f, 0.6f)]
    public void DisableElementForceInstant()
    {
        DisableElementImmediate(toggleInteractable: true, triggerEvents: true, playAudio: true);
    }

    #endregion

    #region PUBLIC ACTIONS (INSTANT)  // <-- NEW

    /// <summary>
    /// Immediately shows/enables the element without playing any tweens.
    /// </summary>
    public void EnableElementImmediate(bool toggleInteractable = true, bool triggerEvents = true, bool playAudio = false)
    {
        AutoCacheComponents();

        // Stop any running tweens now
        DOTween.Kill(transform);
        DOTween.Kill(this);

        // Ensure a clean, visible state
        if (Root != null)
        {
            Root.Visible = true;
            Root.transform.localScale = Vector3.one; // in case a previous scale-out left it small
        }

        // Reset spring to rest (no animation)
        if (springComponent != null)
        {
            springComponent.SetTargetScale(1f);
            springComponent.SetTargetRotation(Quaternion.identity);
            springComponent.ReachEquilibrium();
        }

        if (toggleInteractable && interactable != null)
            interactable.enabled = true;

        isEnabled = true;

        if (playAudio) EventBus<AudioEvent>.Raise(new AudioEvent(showSound));
        if (triggerEvents) OnEnable?.Invoke();
        
        // Trigger completion event immediately for instant enable
        if (triggerEvents) OnEnableFinish?.Invoke();
    }

    /// <summary>
    /// Immediately hides/disables the element without playing any tweens.
    /// </summary>
    public void DisableElementImmediate(bool toggleInteractable = true, bool triggerEvents = true, bool playAudio = false)
    {
        AutoCacheComponents();

        // Stop any running tweens now
        DOTween.Kill(transform);
        DOTween.Kill(this);

        // Reset spring to rest (no animation) and optionally keep it at rest
        if (springComponent != null)
        {
            springComponent.SetTargetScale(1f);
            springComponent.SetTargetRotation(Quaternion.identity);
            springComponent.ReachEquilibrium();
        }

        if (toggleInteractable && interactable != null)
            interactable.enabled = false;

        if (hideWhenDisabled && Root != null)
        {
            Root.Visible = false;
            // Don't reset scale here - preserve whatever state the tweens left it in
        }

        isEnabled = false;

        if (playAudio) EventBus<AudioEvent>.Raise(new AudioEvent(hideSound));
        if (triggerEvents) OnDisable?.Invoke();
        
        // Trigger completion event immediately for instant disable
        if (triggerEvents) OnDisableFinish?.Invoke();
    }

    #endregion

    #region INTERNAL HELPERS

    private void SetInitialStatesForEnableTweens()
    {
        if (enableTweens == null || enableTweens.Count == 0) return;

        foreach (var item in enableTweens)
        {
            if (item.preset == null) continue;

            var preset = item.preset;
            var overrideObj = item.targetOverride;

            // Set common initial states for intro animations
            switch (preset.tweenType)
            {
                case TweenPreset.TweenType.Scale:
                {
                    Transform targetTransform = GetTargetTransform(overrideObj);
                    if (targetTransform != null)
                    {
                        // Only reset to zero if the scale is currently at normal size (1,1,1)
                        // This preserves the scaled-down state from disable animations
                        if (Vector3.Distance(targetTransform.localScale, Vector3.one) < 0.1f)
                        {
                            targetTransform.localScale = Vector3.zero;
                        }
                        // If it's already small (from disable), leave it as is for the enable animation
                    }
                    break;
                }
                case TweenPreset.TweenType.Fade:
                {
                    CanvasGroup cg = GetTargetCanvasGroup(overrideObj);
                    if (cg != null)
                    {
                        // Start from alpha 0 for intro fade animations
                        cg.alpha = 0f;
                    }
                    break;
                }
                // For Move and Rotate, let the preset handle the initial state
                case TweenPreset.TweenType.Move:
                case TweenPreset.TweenType.Rotate:
                default:
                    break;
            }
        }
    }

    private Transform GetTargetTransform(UnityEngine.Object overrideObj)
    {
        if (overrideObj is Transform t) return t;
        if (overrideObj is Component comp) return comp.transform;
        return this.transform;
    }

    private CanvasGroup GetTargetCanvasGroup(UnityEngine.Object overrideObj)
    {
        if (overrideObj is CanvasGroup cg) return cg;
        if (overrideObj is Component comp) return comp.GetComponent<CanvasGroup>();
        return GetComponent<CanvasGroup>();
    }

    private void AutoCacheComponents()
    {
        if (Root == null) Root = GetComponent<UIBlock>();
        if (interactable == null) interactable = GetComponent<Interactable>();
        if (springComponent == null) springComponent = GetComponent<TransformSpringComponent>();
    }

    private void EnableComponents()
    {
        if (interactable != null) interactable.enabled = true;
    }

    private void PlayPresetList(List<TweenItem> list, PlayMode mode, Action onComplete = null)
    {
        if (list == null || list.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = DOTween.Sequence().SetTarget(this);
        bool addedAny = false;

        foreach (var item in list)
        {
            if (item.preset == null) continue;

            Tween t = CreateTweenFromItem(item);
            if (t == null) continue;

            addedAny = true;
            if (mode == PlayMode.Sequential) seq.Append(t);
            else seq.Join(t);
        }

        if (!addedAny)
        {
            seq.Kill();
            onComplete?.Invoke();
            return;
        }

        if (onComplete != null)
            seq.OnComplete(onComplete.Invoke);

        seq.Play();
    }

    private Tween CreateTweenFromItem(TweenItem item)
    {
        var preset = item.preset;
        var overrideObj = item.targetOverride;

        switch (preset.tweenType)
        {
            case TweenPreset.TweenType.Fade:
            {
                CanvasGroup cg = null;
                if (overrideObj is CanvasGroup cgo) cg = cgo;
                else if (overrideObj is Component compCg) cg = compCg.GetComponent<CanvasGroup>();
                else cg = GetComponent<CanvasGroup>();

                if (cg != null) return preset.ApplyTween(cg);

                Debug.LogWarning($"[{name}] UIElement: Fade preset requires a CanvasGroup target (override or on this object). Skipping.");
                return null;
            }

            case TweenPreset.TweenType.Move:
            case TweenPreset.TweenType.Scale:
            case TweenPreset.TweenType.Rotate:
            default:
            {
                Transform targetTransform = null;

                if (overrideObj is Transform t) targetTransform = t;
                else if (overrideObj is Component comp) targetTransform = comp.transform;
                else targetTransform = this.transform;

                return preset.ApplyTween(targetTransform);
            }
        }
    }

    #endregion
}
