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
    public enum DeviceMode { Auto, Desktop, Mobile }
   
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

    private IInteractable lastHovered;
    private Camera cam;

    // Build mode tracking
    private bool isBuildMode = false;
    private EventBinding<BuildingEvent> buildingEventBinding;
    private HexTile currentHoveredHexTile;
    private Vector2 screenPosThisFrame;

    private void Awake()
    {
        cam = overrideCamera != null ? overrideCamera : Camera.main;
        if (cam == null)
            Debug.LogWarning($"{nameof(Interactor)}: No Camera found. Assign one to 'overrideCamera'.");

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

    private void OnDestroy()
    {
        // Unregister from building events
        if (buildingEventBinding != null)
        {
            EventBus<BuildingEvent>.Deregister(buildingEventBinding);
            buildingEventBinding = null;
        }
    }

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

        // Raycast
        IInteractable hovered = RaycastInteractable(screenPosThisFrame);

        // Click
        if (hovered != null && lmbDown)
        {
            hovered.OnClick();
        }
    }

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


}