using UnityEngine;
using Sirenix.OdinInspector;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public interface IInteractable
{
    void OnHover();
    void OnHoverExit();
    void OnClick();
}

[DefaultExecutionOrder(-10)]
public class Interactor : MonoBehaviour
{
    #region ENUMS
    public enum DeviceMode { Auto, Desktop, Mobile }
    #endregion

    #region CONFIG (ODIN)

    [FoldoutGroup("Raycast")]
    [PropertyTooltip("Layer(s) considered interactable.")]
    public LayerMask interactableLayer = ~0;

    [FoldoutGroup("Raycast")]
    [MinValue(0.1f)]
    [PropertyTooltip("Max ray distance.")]
    public float maxDistance = 1000f;

    [FoldoutGroup("Raycast")]
    [Tooltip("Camera used for screen→ray. Defaults to Camera.main if null.")]
    public Camera overrideCamera;

    [FoldoutGroup("Input")]
    [PropertyTooltip("Auto: SystemInfo.deviceType (with WebGL touch fallback). You can force Desktop or Mobile here for testing.")]
    public DeviceMode inputMode = DeviceMode.Auto;

    [FoldoutGroup("Input")]
    [PropertyTooltip("Block interactions while hovering Nova UI (via NovaHoverGuard).")]
    public bool blockWhenHoveringNovaUI = true;

    [FoldoutGroup("Mobile Tap")]
    [ShowIf("@ResolvedMode == DeviceMode.Mobile")]
    [MinMaxSlider(0.05f, 0.6f, true)]
    [LabelText("Tap Time (min..max)")]
    public Vector2 tapTimeRange = new Vector2(0.05f, 0.3f);

    [FoldoutGroup("Mobile Tap")]
    [ShowIf("@ResolvedMode == DeviceMode.Mobile")]
    [MinValue(0f), LabelText("Tap Move Pixels (max)")]
    public float tapMaxMovePixels = 20f;

    #endregion

    #region STATE
    private IInteractable lastHovered;
    private Camera cam;

    // Mobile tap bookkeeping
    private bool tapCandidate;
    private float tapStartTime;
    private Vector2 tapStartPos;
    private bool pointerStartedOverNovaUI;

    // Cached screen pos used this frame (mouse or touch)
    private Vector2 screenPosThisFrame;

    // Computed device mode
    private DeviceMode ResolvedMode
    {
        get
        {
            if (inputMode != DeviceMode.Auto) return inputMode;

            bool isMobile = (SystemInfo.deviceType == DeviceType.Handheld);

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            // WebGL touch fallback (mobile browsers sometimes report as Desktop)
            bool webGlTouchActive =
                (Application.platform == RuntimePlatform.WebGLPlayer) &&
                (Touchscreen.current != null) &&
                Touchscreen.current.touches.Count > 0;
            if (webGlTouchActive) isMobile = true;
#endif
            return isMobile ? DeviceMode.Mobile : DeviceMode.Desktop;
        }
    }
    #endregion

    #region UNITY
    private void Awake()
    {
        cam = overrideCamera != null ? overrideCamera : Camera.main;
        if (cam == null)
            Debug.LogWarning($"{nameof(Interactor)}: No Camera found. Assign one to 'overrideCamera'.");
    }

    private void Update()
    {
        if (cam == null) return;

        switch (ResolvedMode)
        {
            case DeviceMode.Desktop:
                UpdateDesktop();
                break;
            case DeviceMode.Mobile:
                UpdateMobile();
                break;
        }
    }

    private void OnDisable()
    {
        // Ensure hover exit is sent when we're disabled
        if (lastHovered != null)
        {
            lastHovered.OnHoverExit();
            lastHovered = null;
        }
    }
    #endregion

    #region DESKTOP (MOUSE)
    private void UpdateDesktop()
    {
        // Mouse position
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Mouse.current == null) return;
        screenPosThisFrame = Mouse.current.position.ReadValue();
        bool lmbDown = Mouse.current.leftButton.wasPressedThisFrame;
#else
        screenPosThisFrame = Input.mousePosition;
        bool lmbDown = Input.GetMouseButtonDown(0);
#endif

        // NOVA UI guard
        if (blockWhenHoveringNovaUI && IsNovaUIHovered())
        {
            HandleHover(null); // clear any world hover while over UI
            return;
        }

        // Raycast
        IInteractable hovered = RaycastInteractable(screenPosThisFrame);

        // Hover enter/exit
        HandleHover(hovered);

        // Click
        if (hovered != null && lmbDown)
        {
            hovered.OnClick();
        }
    }
    #endregion

    #region MOBILE (TOUCH)
    private void UpdateMobile()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Touchscreen.current == null)
        {
            HandleHover(null);
            return;
        }

        // Choose the primary finger for interactions (first in-progress touch)
        var touches = Touchscreen.current.touches;
        TouchControl primary = null;
        for (int i = 0; i < touches.Count; i++)
        {
            if (touches[i].isInProgress)
            {
                primary = touches[i];
                break;
            }
        }

        if (primary == null)
        {
            HandleHover(null);
            tapCandidate = false;
            return;
        }

        var phase = primary.phase.ReadValue();
        var pos = primary.position.ReadValue();
        screenPosThisFrame = pos;

        if (phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            pointerStartedOverNovaUI = blockWhenHoveringNovaUI && IsNovaUIHovered();
            tapCandidate = !pointerStartedOverNovaUI;
            tapStartTime = Time.unscaledTime;
            tapStartPos = pos;
        }

        // If UI blocking, keep world hover cleared while over Nova UI
        if (blockWhenHoveringNovaUI && (pointerStartedOverNovaUI || IsNovaUIHovered()))
        {
            HandleHover(null);
            return;
        }

        // Hover while the finger is down (began/moved/stationary)
        if (phase == UnityEngine.InputSystem.TouchPhase.Began ||
            phase == UnityEngine.InputSystem.TouchPhase.Moved ||
            phase == UnityEngine.InputSystem.TouchPhase.Stationary)
        {
            var hovered = RaycastInteractable(screenPosThisFrame);
            HandleHover(hovered);
        }

        // Click on Ended if it looks like a tap
        if (phase == UnityEngine.InputSystem.TouchPhase.Ended && tapCandidate)
        {
            float dt = Time.unscaledTime - tapStartTime;
            float move = (pos - tapStartPos).magnitude;

            if (dt >= tapTimeRange.x && dt <= tapTimeRange.y && move <= tapMaxMovePixels)
            {
                var hovered = RaycastInteractable(screenPosThisFrame);
                if (hovered != null) hovered.OnClick();
            }

            tapCandidate = false;
        }

        // Cancel resets
        if (phase == UnityEngine.InputSystem.TouchPhase.Canceled)
        {
            tapCandidate = false;
            HandleHover(null);
        }
#else
        // Legacy Input fallback (best-effort): treat as desktop mouse
        UpdateDesktop();
#endif
    }
    #endregion

    #region CORE
    private IInteractable RaycastInteractable(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out var hit, maxDistance, interactableLayer))
        {
            var interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable == null)
                interactable = hit.collider.GetComponentInParent<IInteractable>();
            return interactable;
        }
        return null;
    }

    private void HandleHover(IInteractable current)
    {
        if (current != lastHovered)
        {
            if (lastHovered != null) lastHovered.OnHoverExit();
            if (current != null) current.OnHover();
            lastHovered = current;
        }
    }
    #endregion

    #region UI GUARD (NOVA)
    // Uses NovaHoverGuard's static flag.
    private static bool IsNovaUIHovered()
    {
#if UNITY_EDITOR
        if (NovaHoverGuard_IsOverNovaUINotFound)
        {
            // Helpful message if the helper isn't present in the project
            return false;
        }
#endif
        return NovaHoverGuard.IsOverNovaUI;
    }

#if UNITY_EDITOR
    // If the user hasn't added NovaHoverGuard yet, avoid spammy exceptions in editor playmode.
    private static bool NovaHoverGuard_IsOverNovaUINotFound
    {
        get
        {
            // Reflection check only in editor; in builds this is stripped.
            var t = System.Type.GetType("NovaHoverGuard");
            if (t == null) return true;
            var p = t.GetProperty("IsOverNovaUI", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            return p == null;
        }
    }
#endif
    #endregion
}
