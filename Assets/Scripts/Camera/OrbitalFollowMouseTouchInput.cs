using System;
using System.IO;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.EventSystems;   // UI CHECKS
using Sirenix.OdinInspector;      // ODIN INSPECTOR
using Sirenix.Serialization;      // ODIN SERIALIZER
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// <summary>
/// CINEMACHINE 3 INPUT CONTROLLER (ORBITAL FOLLOW + HARD LOOK AT)
/// - ROTATION: DESKTOP/WEB -> LMB DRAG ; MOBILE -> ONE-FINGER DRAG  => HorizontalAxis.Value
/// - ZOOM: **CINEMACHINE LENS FOV**
/// - INPUT MODE: SystemInfo.deviceType (Handheld => Mobile), with WebGL touch fallback
/// - CLICK→DRAG BUFFER: avoids rotating on short clicks (world or UI)
/// - UI GUARD: optional "don't rotate if press started over UI"
/// - SETTINGS: Odin-serializable data + Save/Load
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
public class OrbitalFollowMouseTouchInput : MonoBehaviour
{
    #region CONSTANTS
    private const string FileName = "OrbitalInputSettings.json";
    private const string PlayerPrefsKey = "OrbitalInputSettings_JSON_B64";
    #endregion

    #region DATA CLASS (ODIN-SERIALIZABLE)
    [Serializable]
    public class OrbitalInputSettings
    {
        [BoxGroup("ROTATION"), LabelText("Rotate Sensitivity"), Tooltip("DEGREES PER PIXEL DRAG")]
        [PropertyRange(0.01f, 2f)]
        public float rotateSensitivity = 0.2f;

        [BoxGroup("ROTATION"), LabelText("Require Hold To Rotate"), Tooltip("ONLY ROTATE WHILE LMB/TOUCH HELD")]
        public bool requireHoldToRotate = true;

        [BoxGroup("CLICK→DRAG BUFFER"), LabelText("Buffer Time (s)"), Tooltip("MINIMUM TIME BEFORE ROTATION CAN START")]
        [PropertyRange(0f, 1f)]
        public float dragStartTimeBuffer = 0.08f;

        [BoxGroup("CLICK→DRAG BUFFER"), LabelText("Buffer Pixels"), Tooltip("MINIMUM PIXEL MOVE BEFORE ROTATION CAN START")]
        [PropertyRange(0f, 300f)]
        public float dragStartPixelBuffer = 6f;

        [BoxGroup("CLICK→DRAG BUFFER"), LabelText("Block If Press On UI"), Tooltip("IF THE PRESS STARTS OVER UI, DISABLE ROTATION FOR THAT PRESS")]
        public bool blockRotationWhenPointerOverUI = true;

        [BoxGroup("ZOOM / FOV"), LabelText("Min FOV")]
        [PropertyRange(5f, 120f)]
        public float minFov = 25f;

        [BoxGroup("ZOOM / FOV"), LabelText("Max FOV")]
        [PropertyRange(10f, 179f)]
        public float maxFov = 80f;

        [BoxGroup("ZOOM / FOV"), LabelText("FOV Step/Scroll"), Tooltip("DESKTOP/WEB SCROLL STEP")]
        [PropertyRange(0.1f, 20f)]
        public float fovStepPerScroll = 2.0f;

        [BoxGroup("ZOOM / FOV"), LabelText("Pinch → FOV Factor"), Tooltip("MOBILE PINCH SCALE (PIXELS → FOV)")]
        [PropertyRange(0.001f, 0.5f)]
        public float pinchFovFactor = 0.05f;

        [BoxGroup("OVERRIDE"), LabelText("Override SystemInfo.deviceType")]
        public bool overrideInputMode = false;

        [BoxGroup("OVERRIDE"), ShowIf(nameof(overrideInputMode)), LabelText("Treat As Mobile")]
        public bool treatAsMobile = false;

        [HideInInspector] public string version = "1.2.0";
    }
    #endregion

    #region REFERENCES
    [Header("REFERENCES")]
    [SerializeField] private CinemachineCamera cineCam;            // AUTO-GRABBED IF NULL
    private CinemachineOrbitalFollow orbital;                      // REQUIRED ON THE CM CAMERA
    #endregion

    #region SETTINGS (ODIN)
    [TitleGroup("Settings (Odin)")]
    [InlineProperty, HideLabel]
    public OrbitalInputSettings settings = new OrbitalInputSettings();

    [TitleGroup("Settings (Odin)")]
    [Button(ButtonSizes.Medium), GUIColor(0.2f, 0.7f, 1f)]
    private void SaveSettings() => SaveSettingsInternal();

    [TitleGroup("Settings (Odin)")]
    [Button(ButtonSizes.Medium), GUIColor(0.2f, 1f, 0.4f)]
    private void LoadSettings() => LoadSettingsInternal();

#if UNITY_EDITOR
    [TitleGroup("Settings (Odin)")]
    [Button(ButtonSizes.Medium), GUIColor(1f, 0.75f, 0.25f)]
    private void ResetToDefaults() => settings = new OrbitalInputSettings();
#endif
    #endregion

    #region INTERNAL STATE
    // MOBILE VS DESKTOP
    private bool _isMobile;
    private bool _warnedOrthoOnce = false;

    // CLICK→DRAG BUFFER STATE (MOUSE)
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    private bool _mousePressed;
    private bool _mouseRotateBlockedThisPress;
    private bool _mouseDraggingCamera;
    private Vector2 _mouseStartPos;
    private Vector2 _mouseLastPos;
    private float _mouseStartTime;

    // CLICK→DRAG BUFFER STATE (TOUCH)
    private int _activeFingerId = -1;
    private bool _touchRotateBlockedThisPress;
    private bool _touchDraggingCamera;
    private Vector2 _touchStartPos;
    private Vector2 _touchLastPos;
    private float _touchStartTime;
#endif
    #endregion

    #region UNITY LIFECYCLE
    private void Awake()
    {
        if (!cineCam) cineCam = GetComponent<CinemachineCamera>();
        orbital = cineCam ? cineCam.GetComponent<CinemachineOrbitalFollow>() : null;

        if (orbital == null)
        {
            Debug.LogError("OrbitalFollowMouseTouchInput: CinemachineOrbitalFollow is required on this CinemachineCamera.");
            enabled = false;
            return;
        }

       // LoadSettingsInternal();
        RecomputeInputMode();
    }

    private void Update()
    {
        if (!orbital || cineCam == null) return;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        bool webGlTouchActive =
            (Application.platform == RuntimePlatform.WebGLPlayer) &&
            (Touchscreen.current != null) &&
            (GetActiveTouchCount() > 0);

        bool useTouchThisFrame = _isMobile || webGlTouchActive;
#else
        bool useTouchThisFrame = _isMobile;
#endif

        HandleRotation(useTouchThisFrame);
        HandleZoom(useTouchThisFrame);
    }

    private void OnDisable()
    {
        //SaveSettingsInternal();
    }
    #endregion

    #region INPUT MODE
    private void RecomputeInputMode()
    {
        if (settings.overrideInputMode)
        {
            _isMobile = settings.treatAsMobile;
            return;
        }
        _isMobile = (SystemInfo.deviceType == UnityEngine.DeviceType.Handheld);
    }
    #endregion

    #region ROTATION (WITH CLICK→DRAG BUFFER)
    private void HandleRotation(bool useTouch)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (useTouch)
        {
            HandleTouchRotation();
        }
        else
        {
            HandleMouseRotation();
        }
#else
        // LEGACY INPUT (DESKTOP ONLY): Basic LMB drag (no touch state machine)
        bool lmb = Input.GetMouseButton(0);
        if (!settings.requireHoldToRotate) lmb = lmb || Mathf.Abs(Input.GetAxis("Mouse X")) > 0.0001f;
        if (lmb)
        {
            float degrees = Input.GetAxis("Mouse X") * 100f * settings.rotateSensitivity;
            orbital.HorizontalAxis.Value += degrees;
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    private void HandleMouseRotation()
    {
        if (Mouse.current == null) return;

        // PRESS BEGAN
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            _mousePressed = true;
            _mouseDraggingCamera = false;
            _mouseStartPos = Mouse.current.position.ReadValue();
            _mouseLastPos = _mouseStartPos;
            _mouseStartTime = Time.unscaledTime;

            _mouseRotateBlockedThisPress = false;
            if (settings.blockRotationWhenPointerOverUI)
            {
                if (NovaHoverGuard.IsOverNovaUI || NovaHoverGuard.IsBeingInteractedWith || CardHandManager.IsAnyCardBeingDragged)
                    _mouseRotateBlockedThisPress = true;
            }
        }

        // WHILE PRESSED
        if (_mousePressed && Mouse.current.leftButton.isPressed)
        {
            Vector2 curPos = Mouse.current.position.ReadValue();
            float elapsed = Time.unscaledTime - _mouseStartTime;
            float moved = (curPos - _mouseStartPos).magnitude;

            if (!_mouseDraggingCamera)
            {
                if (!_mouseRotateBlockedThisPress &&
                    (!settings.requireHoldToRotate || Mouse.current.leftButton.isPressed) &&
                    elapsed >= settings.dragStartTimeBuffer &&
                    moved >= settings.dragStartPixelBuffer)
                {
                    // Double-check UI state right before starting camera drag
                    if (settings.blockRotationWhenPointerOverUI && 
                        (NovaHoverGuard.IsOverNovaUI || NovaHoverGuard.IsBeingInteractedWith || CardHandManager.IsAnyCardBeingDragged))
                    {
                        _mouseRotateBlockedThisPress = true;
                        return;
                    }
                    
                    _mouseDraggingCamera = true;
                    _mouseLastPos = curPos; // start consuming deltas from here
                }
            }

            if (_mouseDraggingCamera)
            {
                // Continuously check if we should stop camera rotation due to Nova UI interaction
                if (settings.blockRotationWhenPointerOverUI && (NovaHoverGuard.IsOverNovaUI || NovaHoverGuard.IsBeingInteractedWith || CardHandManager.IsAnyCardBeingDragged))
                {
                    _mouseDraggingCamera = false;
                    return;
                }

                Vector2 delta = curPos - _mouseLastPos;
                _mouseLastPos = curPos;

                float degrees = delta.x * settings.rotateSensitivity;
                orbital.HorizontalAxis.Value += degrees;
            }
        }

        // RELEASED
        if (_mousePressed && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            _mousePressed = false;
            _mouseDraggingCamera = false;
            _mouseRotateBlockedThisPress = false;
        }
    }

    private void HandleTouchRotation()
    {
        if (Touchscreen.current == null) return;

        // FIND A TOUCH THAT JUST BEGAN TO OWN THIS GESTURE
        if (_activeFingerId < 0)
        {
            for (int i = 0; i < Touchscreen.current.touches.Count; i++)
            {
                var t = Touchscreen.current.touches[i];
                if (t.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    _activeFingerId = t.touchId.ReadValue();
                    _touchDraggingCamera = false;
                    _touchStartPos = t.position.ReadValue();
                    _touchLastPos = _touchStartPos;
                    _touchStartTime = Time.unscaledTime;

                    _touchRotateBlockedThisPress = false;
                    if (settings.blockRotationWhenPointerOverUI)
                    {
                        // FOR TOUCH, CHECK NOVA UI HOVER GUARD
                        if (NovaHoverGuard.IsOverNovaUI || NovaHoverGuard.IsBeingInteractedWith || CardHandManager.IsAnyCardBeingDragged)
                            _touchRotateBlockedThisPress = true;
                    }
                    break;
                }
            }
        }

        // IF TWO OR MORE TOUCHES: likely pinch/zoom — don't start rotation with the primary
        int activeCount = GetActiveTouchCount();
        if (activeCount >= 2)
        {
            // If we weren’t dragging yet, keep rotation off during pinch.
            if (!_touchDraggingCamera)
                return;
        }

        // UPDATE CURRENT ACTIVE TOUCH (IF ANY)
        if (_activeFingerId >= 0)
        {
            TouchControl cur = GetTouchById(_activeFingerId);
            if (cur == null || !cur.isInProgress)
            {
                // ENDED / CANCELED
                _activeFingerId = -1;
                _touchDraggingCamera = false;
                _touchRotateBlockedThisPress = false;
                return;
            }

            Vector2 curPos = cur.position.ReadValue();
            float elapsed = Time.unscaledTime - _touchStartTime;
            float moved = (curPos - _touchStartPos).magnitude;

            if (!_touchDraggingCamera)
            {
                if (!_touchRotateBlockedThisPress &&
                    elapsed >= settings.dragStartTimeBuffer &&
                    moved >= settings.dragStartPixelBuffer)
                {
                    // Double-check UI state right before starting camera drag
                    if (settings.blockRotationWhenPointerOverUI && 
                        (NovaHoverGuard.IsOverNovaUI || NovaHoverGuard.IsBeingInteractedWith || CardHandManager.IsAnyCardBeingDragged))
                    {
                        _touchRotateBlockedThisPress = true;
                        return;
                    }
                    
                    _touchDraggingCamera = true;
                    _touchLastPos = curPos;
                }
            }

            if (_touchDraggingCamera)
            {
                // Continuously check if we should stop camera rotation due to Nova UI interaction
                if (settings.blockRotationWhenPointerOverUI && (NovaHoverGuard.IsOverNovaUI || NovaHoverGuard.IsBeingInteractedWith || CardHandManager.IsAnyCardBeingDragged))
                {
                    _touchDraggingCamera = false;
                    return;
                }

                Vector2 delta = curPos - _touchLastPos;
                _touchLastPos = curPos;

                float degrees = delta.x * settings.rotateSensitivity;
                orbital.HorizontalAxis.Value += degrees;
            }

            // IF THIS TOUCH ENDED THIS FRAME, RESET (handled at next Update)
        }
    }

    private static TouchControl GetTouchById(int id)
    {
        var touches = Touchscreen.current.touches;
        for (int i = 0; i < touches.Count; i++)
            if (touches[i].touchId.ReadValue() == id)
                return touches[i];
        return null;
    }
#endif
    #endregion

    #region ZOOM (CINEMACHINE FOV ONLY)
    private void HandleZoom(bool useTouch)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (useTouch) HandlePinchZoom_Mobile();
        else HandleScrollZoom_DesktopWeb();
#else
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
            ApplyFovDeltaFromScroll(scroll);
#endif
    }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    private void HandleScrollZoom_DesktopWeb()
    {
        if (Mouse.current == null) return;
        float scrollY = Mouse.current.scroll.y.ReadValue(); // +UP
        if (Mathf.Abs(scrollY) > 0.001f)
            ApplyFovDeltaFromScroll(scrollY);
    }

    private void HandlePinchZoom_Mobile()
    {
        if (Touchscreen.current == null || GetActiveTouchCount() < 2) return;

        var t0 = FirstActiveTouch();
        var t1 = SecondActiveTouch();

        float curr = Vector2.Distance(t0.position.ReadValue(), t1.position.ReadValue());
        float prev = Vector2.Distance(t0.position.ReadValue() - t0.delta.ReadValue(),
                                      t1.position.ReadValue() - t1.delta.ReadValue());
        float pinchDelta = curr - prev; // >0 = APART

        ApplyFovDeltaFromPinch(pinchDelta);
    }
#endif

    private void ApplyFovDeltaFromScroll(float scrollY)
    {
        var lens = cineCam.Lens;
        if (lens.Orthographic)
        {
            WarnOnceOrtho();
            return;
        }

        float fov = lens.FieldOfView - (scrollY * settings.fovStepPerScroll);
        lens.FieldOfView = Mathf.Clamp(fov, settings.minFov, settings.maxFov);
        cineCam.Lens = lens; // ASSIGN BACK
    }

    private void ApplyFovDeltaFromPinch(float pinchDelta)
    {
        var lens = cineCam.Lens;
        if (lens.Orthographic)
        {
            WarnOnceOrtho();
            return;
        }

        float fov = lens.FieldOfView + (pinchDelta * settings.pinchFovFactor);
        lens.FieldOfView = Mathf.Clamp(fov, settings.minFov, settings.maxFov);
        cineCam.Lens = lens; // ASSIGN BACK
    }

    private void WarnOnceOrtho()
    {
        if (_warnedOrthoOnce) return;
        _warnedOrthoOnce = true;
        Debug.LogWarning("[OrbitalFollowMouseTouchInput] Zoom targets Cinemachine FOV, but the lens is Orthographic. Switch to Perspective for FOV zoom.");
    }
    #endregion

    #region SAVE / LOAD (ODIN SERIALIZER)
    private void SaveSettingsInternal()
    {
        try
        {
            var bytes = SerializationUtility.SerializeValue(settings, DataFormat.JSON);
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                string payload = Convert.ToBase64String(bytes);
                PlayerPrefs.SetString(PlayerPrefsKey, payload);
                PlayerPrefs.Save();
            }
            else
            {
                string path = Path.Combine(Application.persistentDataPath, FileName);
                File.WriteAllBytes(path, bytes);
            }
#if UNITY_EDITOR
            Debug.Log("[OrbitalFollowMouseTouchInput] Settings saved.");
#endif
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[OrbitalFollowMouseTouchInput] Save failed: {e.Message}");
        }
    }

    private void LoadSettingsInternal()
    {
        try
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                if (!PlayerPrefs.HasKey(PlayerPrefsKey)) return;
                string payload = PlayerPrefs.GetString(PlayerPrefsKey);
                var bytes = Convert.FromBase64String(payload);
                settings = SerializationUtility.DeserializeValue<OrbitalInputSettings>(bytes, DataFormat.JSON);
            }
            else
            {
                string path = Path.Combine(Application.persistentDataPath, FileName);
                if (!File.Exists(path)) return;
                var bytes = File.ReadAllBytes(path);
                settings = SerializationUtility.DeserializeValue<OrbitalInputSettings>(bytes, DataFormat.JSON);
            }
#if UNITY_EDITOR
            Debug.Log("[OrbitalFollowMouseTouchInput] Settings loaded.");
#endif
            SanitizeSettings();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[OrbitalFollowMouseTouchInput] Load failed: {e.Message}");
        }
    }

    private void SanitizeSettings()
    {
        settings.maxFov = Mathf.Max(settings.maxFov, settings.minFov + 0.001f);
        settings.fovStepPerScroll = Mathf.Max(0.0001f, settings.fovStepPerScroll);
        settings.pinchFovFactor = Mathf.Max(0.000001f, settings.pinchFovFactor);
        settings.dragStartTimeBuffer = Mathf.Max(0f, settings.dragStartTimeBuffer);
        settings.dragStartPixelBuffer = Mathf.Max(0f, settings.dragStartPixelBuffer);
    }
    #endregion

    #region TOUCH HELPERS (INPUT SYSTEM)
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    private static int GetActiveTouchCount()
    {
        int count = 0;
        var touches = Touchscreen.current.touches;
        for (int i = 0; i < touches.Count; i++)
            if (touches[i].isInProgress) count++;
        return count;
    }

    private static TouchControl FirstActiveTouch()
    {
        var touches = Touchscreen.current.touches;
        for (int i = 0; i < touches.Count; i++)
            if (touches[i].isInProgress) return touches[i];
        return touches[0];
    }

    private static TouchControl SecondActiveTouch()
    {
        bool sawFirst = false;
        var touches = Touchscreen.current.touches;
        for (int i = 0; i < touches.Count; i++)
        {
            if (!touches[i].isInProgress) continue;
            if (!sawFirst) { sawFirst = true; continue; }
            return touches[i];
        }
        return touches[Mathf.Min(1, touches.Count - 1)];
    }
#endif
    #endregion
}
