using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using DG.Tweening;

//REF: https://www.youtube.com/watch?v=K_aAnBn5khA&ab_channel=PressStart

public class PanAndZoom : MonoBehaviour
{
    // CAMERA REFERENCE FOR CINEMACHINE
    [BoxGroup("Camera")]
    [LabelText("Virtual Camera")]
    [SerializeField] public CinemachineCamera virtCam;

    // MAIN CAMERA REFERENCE
    [BoxGroup("Camera")]
    [LabelText("Main Camera")]
    [SerializeField] public Camera thisCamera;

    // TOUCH/DRAG START POSITION
    [BoxGroup("Input")]
    [LabelText("Touch Start")]
    [ShowInInspector] public Vector3 touchStart;
    [BoxGroup("Input")]
    [LabelText("Touch Start Screen")]
    [ShowInInspector] public Vector3 touchStartScreen;
    [BoxGroup("Input")]
    [LabelText("Is Dragging")]
    [ShowInInspector] public bool isDragging = false;
    [BoxGroup("Input")]
    [LabelText("Drag Threshold")]
    [SerializeField] public float dragThreshold = 5f; // pixels

    // ZOOM LIMITS
    [BoxGroup("Zoom")]
    [LabelText("Zoom Out Min")]
    [SerializeField] public float zoomOutMin = 1F;
    [BoxGroup("Zoom")]
    [LabelText("Zoom Out Max")]
    [SerializeField] public float zoomOutMax = 6F;

    // PANNING LIMITS
    [BoxGroup("Panning")]
    [LabelText("Pan Radius")]
    [SerializeField] public float panRadius = 20f; // Set this in the Inspector
    [BoxGroup("Panning")]
    [LabelText("Pan Center")]
    [ShowInInspector] public Vector3 panCenter;
    [BoxGroup("Panning")]
    [LabelText("Pan Offset")]
    [SerializeField] public Vector3 panOffset = new Vector3(0, -2, 0); // Default offset (slightly lower)

    // INPUT CONTROL
    [BoxGroup("Input")]
    [LabelText("Allow Input")]
    [SerializeField] public bool allowInput = true;

    // ORBIT VARIABLES
    [BoxGroup("Orbit")]
    [LabelText("Orbit Rotation")]
    [ShowInInspector] public float orbitRotation = 0f;
    [BoxGroup("Orbit")]
    [LabelText("Orbit Rotation Default")]
    [SerializeField] public float orbitRotationDefault = 0f;
    [BoxGroup("Orbit")]
    [LabelText("Orbit Sensitivity")]
    [SerializeField] public float orbitSensitivity = 0.3f; // Adjust as needed
    [BoxGroup("Orbit")]
    [LabelText("Is Orbiting")]
    [ShowInInspector] public bool isOrbiting = false;
    [BoxGroup("Orbit")]
    [LabelText("Orbit Start Screen")]
    [ShowInInspector] public Vector3 orbitStartScreen;

    // RESET VARIABLES
    [BoxGroup("Reset")]
    [LabelText("Reset Duration")]
    [SerializeField] public float resetDuration = 0.5f;

    // ==== RUNTIME HELPERS (moved OUT of UNITY_EDITOR so builds succeed) ====

    /// <summary>
    /// True if the current pointer is over any Unity UI element.
    /// Safe for builds (EventSystem null-checked).
    /// </summary>
    private bool IsPointerOverUIElement()
    {
        if (EventSystem.current == null)
            return false;

        // For mouse, the parameterless overload is fine. For touch, you can
        // add fingerId variants if you later handle multi-touch UI.
        return EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// Central place to decide if camera input should be blocked.
    /// Currently just blocks when pointer is over Unity UI.
    /// Extend here if you later add other blockers.
    /// </summary>
    private bool ShouldBlockCameraInput()
    {
        return IsPointerOverUIElement();
    }

#if UNITY_EDITOR
    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("🎮 Component Enabled")]
    private bool Debug_ComponentEnabled => enabled;

    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("🎯 Allow Input")]
    private bool Debug_AllowInput => allowInput;

    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("🖱 Is Dragging")]
    private bool Debug_IsDragging => isDragging;

    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("🌀 Is Orbiting")]
    private bool Debug_IsOrbiting => isOrbiting;

    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText(" Over Unity UI")]
    private bool Debug_OverUnityUI => IsPointerOverUIElement();

    [BoxGroup("Debug Info"), ShowInInspector, ReadOnly]
    [LabelText("📊 Input State")]
    [DisplayAsString]
    private string Debug_InputState =>
        $"Enabled: {enabled} | " +
        $"AllowInput: {allowInput} | " +
        $"OverUnityUI: {IsPointerOverUIElement()} | " +
        $"CardDragged: {CardHandManager.IsAnyCardBeingDragged} | " + // Editor-only ref is fine here
        $"Dragging: {isDragging}";

    [BoxGroup("Debug Controls"), GUIColor(0.2f, 0.7f, 1f)]
    [Button(ButtonSizes.Large, Name = "Reset Camera", Icon = SdfIconType.Camera)]
    private void Debug_ResetCamera()
    {
        ResetCam();
    }

    [BoxGroup("Debug Controls"), GUIColor(0.7f, 1f, 0.2f)]
    [Button(ButtonSizes.Large, Name = "Set Pan Center", Icon = SdfIconType.GeoAlt)]
    private void Debug_SetPanCenter()
    {
        if (thisCamera != null)
        {
            panCenter = thisCamera.transform.position + panOffset;
        }
    }

    [BoxGroup("Debug Controls"), GUIColor(1f, 0.5f, 0.2f)]
    [Button(ButtonSizes.Large, Name = "Toggle Input", Icon = SdfIconType.ToggleOn)]
    private void Debug_ToggleInput()
    {
        allowInput = !allowInput;
    }

    [BoxGroup("Debug Controls"), GUIColor(1f, 0.2f, 0.8f)]
    [Button(ButtonSizes.Large, Name = "Reset Orbit", Icon = SdfIconType.ArrowClockwise)]
    private void Debug_ResetOrbit()
    {
        orbitRotation = orbitRotationDefault;
        ApplyOrbitRotation();
    }

    [BoxGroup("Debug Controls"), GUIColor(0.8f, 0.3f, 0.8f)]
    [Button(ButtonSizes.Medium, Name = "Force Enable Component", Icon = SdfIconType.PlayFill)]
    private void Debug_ForceEnable()
    {
        enabled = true;
        allowInput = true;
    }

    [BoxGroup("Debug Controls"), GUIColor(1f, 0.8f, 0.2f)]
    [Button(ButtonSizes.Medium, Name = "Reset Unity UI Detection", Icon = SdfIconType.XCircle)]
    private void Debug_ResetUnityUIDetection()
    {
        Debug.Log($"Unity UI Detection - Over UI: {IsPointerOverUIElement()}");
    }
#endif

    private void Start()
    {
        // ALLOW CAMERA TO BE SET IN INSPECTOR OR AUTOMATICALLY
        if (thisCamera == null)
            thisCamera = GetComponent<Camera>();
        if (thisCamera == null)
            thisCamera = Camera.main;
        panCenter = thisCamera.transform.position + panOffset;
    }

    void Update()
    {
        if (allowInput)
        {
            //INTERACTION WITH UI
            HandlePanInput();
            HandleOrbitInput();
            Zoom(Input.GetAxis("Mouse ScrollWheel"));
        }
    }

    // HANDLE PANNING INPUT (LEFT MOUSE OR SINGLE TOUCH)
    private void HandlePanInput()
    {
        // Don't handle input if mouse is over UI or if a card is being dragged
        if (ShouldBlockCameraInput())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            // Double-check UI state immediately on mouse down
            if (ShouldBlockCameraInput())
                return;

            touchStart = thisCamera.ScreenToWorldPoint(Input.mousePosition);
            touchStartScreen = Input.mousePosition;
            isDragging = false;
        }

        if (Input.GetMouseButton(0))
        {
            // Continuously check if we should stop panning due to UI interaction
            if (ShouldBlockCameraInput())
            {
                isDragging = false;
                return;
            }

            if (!isDragging && (Input.mousePosition - touchStartScreen).magnitude > dragThreshold)
            {
                isDragging = true;
            }

            if (isDragging)
            {
                Vector3 direction = touchStart - thisCamera.ScreenToWorldPoint(Input.mousePosition);
                Vector3 newPosition = thisCamera.transform.position + direction;

                // Clamp to pan radius
                Vector3 offset = newPosition - panCenter;
                if (offset.magnitude > panRadius)
                {
                    offset = offset.normalized * panRadius;
                    newPosition = panCenter + offset;
                }

                thisCamera.transform.position = newPosition;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    // APPLY ORBIT ROTATION TO CAMERA
    private void ApplyOrbitRotation()
    {
        // CINEMACHINE ORBIT ROTATION
        if (virtCam != null)
        {
            // Anchor orbit to a persistent value
            virtCam.transform.localRotation = Quaternion.Euler(virtCam.transform.localRotation.eulerAngles.x, orbitRotation, virtCam.transform.localRotation.eulerAngles.z);
        }
        // Optionally rotate the Unity camera if needed
        if (thisCamera != null)
        {
            thisCamera.transform.localRotation = Quaternion.Euler(thisCamera.transform.localRotation.eulerAngles.x, orbitRotation, thisCamera.transform.localRotation.eulerAngles.z);
        }
    }

    // HANDLE ORBIT INPUT (RIGHT MOUSE OR TWO-FINGER TOUCH)
    private void HandleOrbitInput()
    {
        // Don't handle input if mouse is over UI or if a card is being dragged
        if (ShouldBlockCameraInput())
            return;

        // PC/Mac/WebGL: Right Mouse Drag
        if (Input.GetMouseButtonDown(1))
        {
            isOrbiting = true;
            orbitStartScreen = Input.mousePosition;
        }
        if (Input.GetMouseButton(1) && isOrbiting)
        {
            Vector3 currentMousePos = Input.mousePosition;
            float deltaX = currentMousePos.x - orbitStartScreen.x;

            // Only apply rotation if we're actually dragging (not on the first frame)
            if (Vector3.Distance(currentMousePos, orbitStartScreen) > 0.1f)
            {
                orbitRotation += deltaX * orbitSensitivity;
                ApplyOrbitRotation();
            }

            orbitStartScreen = currentMousePos; // Update for next frame
        }
        if (Input.GetMouseButtonUp(1))
        {
            isOrbiting = false;
        }

        // Mobile: Two-Finger Drag
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            // Use average movement for orbit
            if (t0.phase == TouchPhase.Moved || t1.phase == TouchPhase.Moved)
            {
                Vector2 avgDelta = (t0.deltaPosition + t1.deltaPosition) / 2f;
                orbitRotation += avgDelta.x * orbitSensitivity;
                ApplyOrbitRotation();
            }
        }
    }

    void Zoom(float increment)
    {
        // Don't handle zoom if mouse is over UI or if a card is being dragged
        if (ShouldBlockCameraInput())
            return;

        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - increment, zoomOutMin, zoomOutMax);
        virtCam.Lens.OrthographicSize = Camera.main.orthographicSize;
    }

    // Returns the shortest angle for smooth rotation
    private float ShortestAngleLerp(float start, float end, float t)
    {
        float shortest = Mathf.DeltaAngle(start, end);
        return start + shortest * t;
    }

    public void ResetCam()
    {
        float targetOrthoSize = 2F;
        Vector3 targetPosition = panCenter;
        float startOrbit = orbitRotation;
        float targetOrbit = orbitRotationDefault;

        // DOTween for camera position
        if (thisCamera != null)
        {
            thisCamera.transform.DOMove(targetPosition, resetDuration);
            DOTween.To(() => thisCamera.orthographicSize, x => thisCamera.orthographicSize = x, targetOrthoSize, resetDuration);
        }
        // DOTween for Cinemachine lens
        if (virtCam != null)
        {
            DOTween.To(() => virtCam.Lens.OrthographicSize, x => virtCam.Lens.OrthographicSize = x, targetOrthoSize, resetDuration);
            DOTween.To(() => 0f, x => {
                orbitRotation = ShortestAngleLerp(startOrbit, targetOrbit, x);
                ApplyOrbitRotation();
            }, 1f, resetDuration);
        }
        else
        {
            DOTween.To(() => 0f, x => {
                orbitRotation = ShortestAngleLerp(startOrbit, targetOrbit, x);
                ApplyOrbitRotation();
            }, 1f, resetDuration);
        }
    }
}
