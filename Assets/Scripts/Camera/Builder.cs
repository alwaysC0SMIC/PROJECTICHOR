using UnityEngine;
using Sirenix.OdinInspector;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class Builder : MonoBehaviour
{
    [TitleGroup("Prefabs")]
    [SerializeField] private GameObject testBuild;
    [SerializeField] private GameObject previewTest;
    
    [TitleGroup("Build State")]
    [LabelText("🔨 Build Mode Active")]
    public bool isBuildMode = false;
    
    [TitleGroup("Raycast Settings")]
    [PropertyTooltip("Layer(s) considered interactable for building.")]
    public LayerMask buildableLayer = ~0;

    [TitleGroup("Raycast Settings")]
    [MinValue(0.1f)]
    [PropertyTooltip("Max ray distance for building raycast.")]
    public float maxDistance = 1000f;

    [TitleGroup("Raycast Settings")]
    [Tooltip("Camera used for screen→ray. Defaults to Camera.main if null.")]
    public Camera overrideCamera;
    
    // Event binding for BuildingEvent
    private EventBinding<BuildingEvent> buildingEventBinding;
    private HexTile lastLoggedHexTile;
    private Camera cam;

    void Start()
    {
        // Setup camera for raycasting
        cam = overrideCamera != null ? overrideCamera : Camera.main;
        if (cam == null)
            Debug.LogWarning($"[Builder] No Camera found. Assign one to 'overrideCamera'.");
        
        // Register for building events
        buildingEventBinding = new EventBinding<BuildingEvent>(OnBuildingEvent);
        EventBus<BuildingEvent>.Register(buildingEventBinding);
    }
    
    void Update()
    {
        if (isBuildMode && cam != null)
        {
            TrackHoveredHexTile();
        }
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
        isBuildMode = buildingEvent.isBuilding;
        
        Debug.Log($"[Builder] Build mode changed: {isBuildMode}");
        
        // Clear last logged hex when exiting build mode
        if (!isBuildMode)
        {
            // Make sure to clear hover state when exiting build mode
            if (lastLoggedHexTile != null)
            {
                lastLoggedHexTile.OnHoverExit();


                //ATTEMPT BUILD
                //if(lastLoggedHexTile.y)
                lastLoggedHexTile.AttemptBuild();
            }

            lastLoggedHexTile = null;
        }
    }
    
    private void TrackHoveredHexTile()
    {
        HexTile currentHovered = RaycastForHexTile();
        
        // Handle hover state changes
        if (currentHovered != lastLoggedHexTile)
        {
            // Call OnHoverExit on the previously hovered hex
            if (lastLoggedHexTile != null)
            {
                lastLoggedHexTile.OnHoverExit();
                Debug.Log($"[Builder] BUILD MODE: No longer hovering over hex {lastLoggedHexTile.coordinates}");
            }
            
            // Call OnHover on the newly hovered hex
            if (currentHovered != null)
            {
                currentHovered.OnHover();
                Debug.Log($"[Builder] BUILD MODE: Now hovering over hex {currentHovered.coordinates} (Type: {currentHovered.hexType})");
            }
            
            lastLoggedHexTile = currentHovered;
        }
    }
    
    private HexTile RaycastForHexTile()
    {
        Vector2 screenPos = GetScreenPosition();
        Ray ray = cam.ScreenPointToRay(screenPos);
        
        if (Physics.Raycast(ray, out var hit, maxDistance, buildableLayer))
        {
            // First try to get HexTile component directly
            var hexTile = hit.collider.GetComponent<HexTile>();
            if (hexTile == null)
            {
                // If not found, try to get it from parent
                hexTile = hit.collider.GetComponentInParent<HexTile>();
            }
            return hexTile;
        }
        return null;
    }
    
    private Vector2 GetScreenPosition()
    {
        // Get mouse/touch position based on input system
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            return Touchscreen.current.touches[0].position.ReadValue();
        }
        return Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }
}
