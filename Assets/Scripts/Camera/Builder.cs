using UnityEngine;
using Sirenix.OdinInspector;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class Builder : MonoBehaviour
{
    // [TitleGroup("Prefabs")]
    // [SerializeField] private GameObject testBuild;
    // [SerializeField] private GameObject previewTest;
    
    [TitleGroup("Build State")]
    [LabelText("🔨 Build Mode Active")]
    public bool isBuildMode = false;
    
    [TitleGroup("Raycast Settings")]
    [PropertyTooltip("Layer(s) considered for hover effects (should include all hex tiles).")]
    public LayerMask hoverableLayer = ~0;

    [TitleGroup("Raycast Settings")]
    [MinValue(0.1f)]
    [PropertyTooltip("Max ray distance for building raycast.")]
    public float maxDistance = 1000f;

    [TitleGroup("Raycast Settings")]
    [Tooltip("Camera used for screen→ray. Defaults to Camera.main if null.")]
    public Camera overrideCamera;
    
    // Event binding for BuildingEvent
    private EventBinding<BuildingEvent> buildingEventBinding;
    private HexTile lastLoggedHexTile; // For build mode tracking
    private HexTile lastHoveredHexTile; // For general hover tracking
    private Camera cam;

    private SO_Defender buildData;

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
        if (cam != null)
        {
            // Always track hover for animations (regardless of build mode)
            TrackGeneralHover();
            
            // Only track build-specific hover when in build mode
            if (isBuildMode)
            {
                TrackBuildModeHover();
            }
        }
    }
    
    void OnDestroy()
    {
        // Clean up hover states
        if (lastHoveredHexTile != null)
        {
            lastHoveredHexTile.OnHoverExit();
            lastHoveredHexTile = null;
        }
        
        if (lastLoggedHexTile != null)
        {
            lastLoggedHexTile = null;
        }
        
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
        buildData = buildingEvent.defenderToBuild;
        

        //Debug.Log($"[Builder] Build mode changed: {isBuildMode}");

        // Notify all hex tiles about build mode state change
        NotifyAllHexTilesOfBuildModeChange(isBuildMode);
        
        // When exiting build mode, attempt to build on the last targeted tile
        if (!isBuildMode)
        {
            if (lastLoggedHexTile != null)
            {
                // Only attempt build if it's a valid buildable tile
                if (lastLoggedHexTile.CanBuild())
                {
                    lastLoggedHexTile.AttemptBuild(buildData);
                    Debug.Log($"[Builder] Building placed on valid defender spot at {lastLoggedHexTile.coordinates}");
                }
                else
                {
                    Debug.Log($"[Builder] Cannot build on {lastLoggedHexTile.hexType} tile at {lastLoggedHexTile.coordinates}");
                }
            }

            lastLoggedHexTile = null;
        }
        else
        {
            // When entering build mode, sync the build tracker with the current hover
            lastLoggedHexTile = lastHoveredHexTile;
        }
    }
    
    private void TrackGeneralHover()
    {
        HexTile currentHovered = RaycastForHexTile();
        
        // Handle general hover state changes (for animations)
        if (currentHovered != lastHoveredHexTile)
        {
            // Call OnHoverExit on the previously hovered hex
            if (lastHoveredHexTile != null)
            {
                lastHoveredHexTile.OnHoverExit();
                //Debug.Log($"[Builder] GENERAL HOVER: No longer hovering over hex {lastHoveredHexTile.coordinates}");
            }
            
            // Call OnHover on the newly hovered hex
            if (currentHovered != null)
            {
                currentHovered.OnHover(buildData);
                //Debug.Log($"[Builder] GENERAL HOVER: Now hovering over hex {currentHovered.coordinates} (Type: {currentHovered.hexType})");
            }
            
            lastHoveredHexTile = currentHovered;
        }
    }
    
    private void TrackBuildModeHover()
    {
        HexTile currentHovered = RaycastForHexTile();
        
        // Handle build mode specific tracking (for build validation)
        if (currentHovered != lastLoggedHexTile)
        {
            // Update build mode tracking
            if (lastLoggedHexTile != null)
            {
                Debug.Log($"[Builder] BUILD MODE: No longer targeting hex {lastLoggedHexTile.coordinates}");
            }
            
            if (currentHovered != null)
            {
                Debug.Log($"[Builder] BUILD MODE: Now targeting hex {currentHovered.coordinates} (Type: {currentHovered.hexType}) - Can Build: {currentHovered.CanBuild()}");
            }
            
            lastLoggedHexTile = currentHovered;
        }
    }
    
    private void NotifyAllHexTilesOfBuildModeChange(bool buildModeActive)
    {
        // Find all HexTile components in the scene and notify them of build mode state
        HexTile[] allHexTiles = FindObjectsByType<HexTile>(FindObjectsSortMode.None);
        foreach (HexTile hexTile in allHexTiles)
        {
            hexTile.SetBuildModeState(buildModeActive);
        }
        
        Debug.Log($"[Builder] Notified {allHexTiles.Length} hex tiles of build mode change: {buildModeActive}");
    }
    
    private HexTile RaycastForHexTile()
    {
        Vector2 screenPos = GetScreenPosition();
        Ray ray = cam.ScreenPointToRay(screenPos);
        
        if (Physics.Raycast(ray, out var hit, maxDistance, hoverableLayer))
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
