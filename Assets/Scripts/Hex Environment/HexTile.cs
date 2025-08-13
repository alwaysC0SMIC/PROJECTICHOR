using UnityEngine;
using Sirenix.OdinInspector;
using AllIn1SpringsToolkit;

/// <summary>
/// Component attached to individual hex tile GameObjects to store their data and type.
/// Note: This GameObject needs a Collider component to be detected by the Interactor's raycast.
/// </summary>
public class HexTile : MonoBehaviour, IInteractable
{
    #region VARIABLES

    [TitleGroup("Hex Data")]
    [LabelText("🗺️ Coordinates")]
    [ReadOnly, ShowInInspector]
    public HexCoordinates coordinates;
    
    [TitleGroup("Hex Data")]
    [LabelText("🏷️ Hex Type")]
    [ReadOnly, ShowInInspector]
    public HexType hexType;
    
    [TitleGroup("Hex Data")]
    [LabelText("🛤️ Lane ID")]
    [ReadOnly, ShowInInspector]
    public int laneId = -1;
    
    [TitleGroup("Hex Data")]
    [LabelText("🔗 Is Junction Point")]
    [ReadOnly, ShowInInspector]
    public bool isJunctionPoint = false;
    
    [TitleGroup("Visual")]
    [LabelText("🎨 Lane Color")]
    [ShowIf("@laneId >= 0")]
    [ReadOnly, ShowInInspector]
    public Color laneColor = Color.white;

    [TitleGroup("Materials")]
    [LabelText("🏛️ Center Hub Material")]
    [SerializeField] private Material centerHubMaterial;
    
    [TitleGroup("Materials")]
    [LabelText("🛤️ Pathway Material")]
    [SerializeField] private Material pathwayMaterial;
    
    [TitleGroup("Materials")]
    [LabelText("🛡️ Defender Spot Material")]
    [SerializeField] private Material defenderSpotMaterial;
    
    [TitleGroup("Materials")]
    [LabelText("🚀 Edge Spawn Material")]
    [SerializeField] private Material edgeSpawnMaterial;
    
    [TitleGroup("Materials")]
    [LabelText("🌿 Environment Material")]
    [SerializeField] private Material environmentMaterial;
    
    [TitleGroup("Materials")]
    [LabelText("🎨 Use Lane Color Override")]
    [Tooltip("When enabled, pathway materials will be tinted with the lane color")]
    [SerializeField] private bool useLaneColorTint = true;

    [TitleGroup("Layer Settings")]
    [LabelText("🏗️ Buildable Layer Name")]
    [Tooltip("Name of the layer for buildable hex tiles (DefenderSpots)")]
    [SerializeField] private string buildableLayerName = "Buildable";

    [TitleGroup("Hover Effects")]
    [LabelText("📈 Hover Y Offset")]
    [Tooltip("Y position offset applied when hovering over buildable tiles")]
    [SerializeField] private float hoverYOffset = 0.1f;

    [TitleGroup("Components")]
    [LabelText("🎨 Renderer")]
    [Tooltip("The renderer component for applying materials and colors")]
    [SerializeField] private Renderer tileRenderer;

    [TitleGroup("Components")]
    [LabelText("🌸 Transform Spring")]
    [Tooltip("The transform spring component for smooth hover animations")]
    [SerializeField] private TransformSpringComponent transformSpring;

    [TitleGroup("Components")]
    [LabelText("💡 Edge Spawn Light Prefab")]
    [Tooltip("Light prefab to spawn above edge spawn tiles")]
    [SerializeField] private GameObject edgeSpawnLightPrefab;

    // Store original position and scale for hover effect
    private Vector3 originalPosition;
    private Vector3 originalScale;

    // Store reference to spawned light object
    private GameObject spawnedLight;

    #endregion

    public bool isOccupied = false;
    private bool isBuildModeActive = false; // Track build mode state

    [SerializeField] GameObject previewObject;
    [SerializeField] GameObject buildObject;

    [SerializeField] GameObject hexagonUI;


    void Start()
    {
        // Initialize renderer reference if not assigned
        if (tileRenderer == null)
            tileRenderer = GetComponent<Renderer>();

        // Initialize transform spring component if not assigned
        if (transformSpring == null)
            transformSpring = GetComponent<TransformSpringComponent>();

        previewObject.SetActive(false);
        buildObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // Clean up spawned light when this tile is destroyed
        RemoveEdgeSpawnLight();
    }

    public void AttemptBuild()
    {
        // Only allow building on valid defender spots
        if (CanBuild())
        {
            buildObject.SetActive(true);
            isOccupied = true;
            
            // Show hexagonUI when a defender is built
            if (hexagonUI != null)
            {
                hexagonUI.SetActive(true);
            }
            
            Debug.Log($"[HexTile] Successfully built on defender spot at {coordinates}");
        }
        else
        {
            Debug.LogWarning($"[HexTile] Cannot build on {hexType} tile at {coordinates} - Occupied: {isOccupied}");
        }
    }

    public void OnTowerDestroyed()
    {
        // Called when the tower/defender on this tile is destroyed
        if (hexType == HexType.DefenderSpot)
        {
            isOccupied = false;
            
            // Hide hexagonUI when defender is destroyed
            if (hexagonUI != null)
            {
                hexagonUI.SetActive(false);
            }
            
            // Hide build object if it was active
            if (buildObject != null)
            {
                buildObject.SetActive(false);
            }
            
            Debug.Log($"[HexTile] Tower destroyed on defender spot at {coordinates} - hexagonUI hidden");
        }
    }

    public void Initialize(HexCoordinates coords, HexType type, int lane = -1, bool junction = false, Color color = default)
    {
        coordinates = coords;
        hexType = type;
        laneId = lane;
        isJunctionPoint = junction;
        laneColor = color == default ? Color.white : color;

        // Store original position and scale for hover effects
        originalPosition = transform.position;
        originalScale = transform.localScale;

        // Set the GameObject name for better hierarchy organization
        gameObject.name = $"Hex_{GetHexTypePrefix(type)}_{coords.q}_{coords.r}";

        // Add lane info to name if applicable
        if (laneId >= 0)
        {
            gameObject.name += $"_L{laneId}";
        }

        if (isJunctionPoint)
        {
            gameObject.name += "_Junction";
        }

        ApplyMaterialForType();

        // Spawn light if this is an edge spawn tile
        if (hexType == HexType.EdgeSpawn)
        {
            SpawnEdgeSpawnLight();
        }

        // Show hexagonUI only for occupied defender spots
        if (hexType == HexType.DefenderSpot)
        {
            hexagonUI.SetActive(isOccupied);
        }
        else
        {
            hexagonUI.SetActive(false);
        }
    }

    private void SetTileBuild()
    {
        if (hexType == HexType.DefenderSpot)
        { 
            // Convert layer name to layer index
            int buildableLayerIndex = LayerMask.NameToLayer(buildableLayerName);
            if (buildableLayerIndex != -1)
            {
                gameObject.layer = buildableLayerIndex;
                Debug.Log($"[HexTile] Set {gameObject.name} to {buildableLayerName} layer (index: {buildableLayerIndex})");
            }
            else
            {
                Debug.LogWarning($"[HexTile] Layer '{buildableLayerName}' not found! Please create this layer in Project Settings > Tags and Layers");
            }
        }
    }

    private void ApplyMaterialForType()
    {
        if (tileRenderer == null)
        {
            Debug.LogWarning($"[HexTile] No Renderer assigned on {gameObject.name}");
            return;
        }

        Material materialToApply = GetMaterialForType(hexType);

        if (materialToApply != null)
        {
            // Create a new material instance if we need to tint with lane color
            if (useLaneColorTint && hexType == HexType.Pathway && laneId >= 0 && laneColor != Color.white)
            {
                Material tintedMaterial = new Material(materialToApply);
                tintedMaterial.color = laneColor;
                tileRenderer.material = tintedMaterial;
            }
            else
            {
                tileRenderer.material = materialToApply;
            }
        }
        else
        {
            Debug.LogWarning($"[HexTile] No material assigned for hex type {hexType} on {gameObject.name}");
        }
        
        SetTileBuild();
    }
    
    private Material GetMaterialForType(HexType type)
    {
        return type switch
        {
            HexType.CenterHub => centerHubMaterial,
            HexType.Pathway => pathwayMaterial,
            HexType.DefenderSpot => defenderSpotMaterial,
            HexType.EdgeSpawn => edgeSpawnMaterial,
            HexType.Environment => environmentMaterial,
            _ => null
        };
    }

    public void UpdateType(HexType newType, int newLaneId = -1, bool newJunction = false, Color newColor = default)
    {
        // Store old type to check for changes
        HexType oldType = hexType;
        
        hexType = newType;
        laneId = newLaneId;
        isJunctionPoint = newJunction;

        if (newColor != default)
        {
            laneColor = newColor;
        }

        // Update GameObject name
        gameObject.name = $"Hex_{GetHexTypePrefix(newType)}_{coordinates.q}_{coordinates.r}";
        if (laneId >= 0)
        {
            gameObject.name += $"_L{laneId}";
        }
        if (isJunctionPoint)
        {
            gameObject.name += "_Junction";
        }
        
        // Update layer and material
        SetTileBuild();
        ApplyMaterialForType();

        // Handle light spawning/removal based on type change
        if (oldType != HexType.EdgeSpawn && newType == HexType.EdgeSpawn)
        {
            // Became an edge spawn - spawn light
            SpawnEdgeSpawnLight();
        }
        else if (oldType == HexType.EdgeSpawn && newType != HexType.EdgeSpawn)
        {
            // No longer an edge spawn - remove light
            RemoveEdgeSpawnLight();
        }

        // Handle hexagonUI visibility based on type change
        if (newType == HexType.DefenderSpot)
        {
            // Show hexagonUI only if occupied
            if (hexagonUI != null)
            {
                hexagonUI.SetActive(isOccupied);
            }
        }
        else
        {
            // Hide hexagonUI for non-defender tiles
            if (hexagonUI != null)
            {
                hexagonUI.SetActive(false);
            }
        }
    }
    
    public void SetTileType(HexType newType)
    {
        UpdateType(newType);
    }

    public HexType GetTileType()
    {
        return hexType;
    }
    
    private string GetHexTypePrefix(HexType type)
    {
        switch (type)
        {
            case HexType.CenterHub: return "Hub";
            case HexType.Pathway: return "Path";
            case HexType.DefenderSpot: return "Def";
            case HexType.EdgeSpawn: return "Spawn";
            case HexType.Environment: return "Env";
            default: return "Unknown";
        }
    }
    
    public Vector3 GetWorldPosition()
    {
        return transform.position;
    }

    public bool CanBuild()
    {
        return hexType == HexType.DefenderSpot && !isOccupied;
    }

    public bool ShouldShowHoverAnimation()
    {
        return hexType != HexType.Pathway && hexType != HexType.EdgeSpawn;
    }

    /// <summary>
    /// Spawns a light prefab 2.5 units above this edge spawn tile
    /// </summary>
    private void SpawnEdgeSpawnLight()
    {
        if (edgeSpawnLightPrefab == null)
        {
            Debug.LogWarning($"[HexTile] No edge spawn light prefab assigned for {gameObject.name}");
            return;
        }

        if (spawnedLight != null)
        {
            Debug.LogWarning($"[HexTile] Light already spawned for {gameObject.name}");
            return;
        }

        // Calculate spawn position 2.5 units above this tile
        Vector3 lightPosition = transform.position + Vector3.up * 0.5f;

        // Instantiate the light prefab
        spawnedLight = Instantiate(edgeSpawnLightPrefab, lightPosition, Quaternion.identity);
        
        // Parent it to this tile
        spawnedLight.transform.SetParent(transform);
        
        // Give it a descriptive name
        spawnedLight.name = $"EdgeSpawnLight_{coordinates.q}_{coordinates.r}";

        Debug.Log($"[HexTile] Spawned edge spawn light for {gameObject.name} at position {lightPosition}");
    }

    /// <summary>
    /// Removes the spawned light if it exists
    /// </summary>
    private void RemoveEdgeSpawnLight()
    {
        if (spawnedLight != null)
        {
            if (Application.isPlaying)
            {
                Destroy(spawnedLight);
            }
            else
            {
                DestroyImmediate(spawnedLight);
            }
            spawnedLight = null;
            Debug.Log($"[HexTile] Removed edge spawn light for {gameObject.name}");
        }
    }

    public void SetBuildModeState(bool buildModeActive)
    {
        isBuildModeActive = buildModeActive;
        
        // If build mode is disabled, hide preview immediately
        if (!isBuildModeActive)
        {
            previewObject.SetActive(false);
        }
    }

    private bool ShouldShowPreview()
    {
        return isBuildModeActive && hexType == HexType.DefenderSpot && !isOccupied;
    }
    
#if UNITY_EDITOR
    [TitleGroup("Debug")]
    [Button(ButtonSizes.Medium, Name = "🔍 Show Debug Info")]
    [GUIColor(0.5f, 0.8f, 1f)]
    private void ShowDebugInfo()
    {
        Debug.Log($"[HexTile] {gameObject.name}\n" +
                  $"Coordinates: {coordinates}\n" +
                  $"Type: {hexType}\n" +
                  $"Lane ID: {laneId}\n" +
                  $"Junction: {isJunctionPoint}\n" +
                  $"Lane Color: {laneColor}\n" +
                  $"World Position: {transform.position}\n" +
                  $"Is Occupied: {isOccupied}\n" +
                  $"Build Mode Active: {isBuildModeActive}\n" +
                  $"Can Build: {CanBuild()}\n" +
                  $"Should Show Preview: {ShouldShowPreview()}");
    }
    
    [TitleGroup("Debug")]
    [Button(ButtonSizes.Medium, Name = "🎨 Apply Type Material")]
    [GUIColor(0.8f, 1f, 0.5f)]
    private void ApplyTypeMaterial()
    {
        ApplyMaterialForType();
        Debug.Log($"[HexTile] Applied material for type {hexType} to {gameObject.name}");
    }
    
    [TitleGroup("Debug")]
    [Button(ButtonSizes.Medium, Name = "🌈 Apply Lane Color Tint")]
    [GUIColor(1f, 0.8f, 0.5f)]
    [ShowIf("@hexType == HexType.Pathway && laneId >= 0")]
    private void ApplyLaneColorTint()
    {
        if (tileRenderer != null && tileRenderer.material != null)
        {
            // Create a tinted version of the pathway material
            if (pathwayMaterial != null)
            {
                Material tintedMaterial = new Material(pathwayMaterial);
                tintedMaterial.color = laneColor;
                tileRenderer.material = tintedMaterial;
                Debug.Log($"[HexTile] Applied lane color tint {laneColor} to {gameObject.name}");
            }
            else
            {
                // Fallback: just apply the lane color directly
                tileRenderer.material.color = laneColor;
                Debug.Log($"[HexTile] Applied lane color {laneColor} to existing material on {gameObject.name}");
            }
        }
        else
        {
            Debug.LogWarning($"[HexTile] No renderer or material found on {gameObject.name}");
        }
    }
    
    [TitleGroup("Debug")]
    [Button(ButtonSizes.Medium, Name = "🔄 Test Hover Effect")]
    [GUIColor(0.5f, 1f, 0.8f)]
    [ShowIf("@ShouldShowHoverAnimation()")]
    private void TestHoverEffect()
    {
        StartCoroutine(TestHoverCoroutine());
    }
    
    [TitleGroup("Debug")]
    [Button(ButtonSizes.Medium, Name = "💡 Toggle Edge Spawn Light")]
    [GUIColor(1f, 1f, 0.2f)]
    [ShowIf("@hexType == HexType.EdgeSpawn")]
    private void ToggleEdgeSpawnLight()
    {
        if (spawnedLight != null)
        {
            RemoveEdgeSpawnLight();
        }
        else
        {
            SpawnEdgeSpawnLight();
        }
    }

    [TitleGroup("Debug")]
    [Button(ButtonSizes.Medium, Name = "💡 Force Spawn Light")]
    [GUIColor(0.2f, 1f, 1f)]
    private void ForceSpawnLight()
    {
        SpawnEdgeSpawnLight();
    }

    [TitleGroup("Debug")]
    [Button(ButtonSizes.Medium, Name = "🏗️ Test Build/Destroy")]
    [GUIColor(1f, 0.5f, 0.2f)]
    [ShowIf("@hexType == HexType.DefenderSpot")]
    private void ToggleBuildDestroy()
    {
        if (isOccupied)
        {
            OnTowerDestroyed();
        }
        else
        {
            AttemptBuild();
        }
    }
    
    private System.Collections.IEnumerator TestHoverCoroutine()
    {
        Debug.Log($"[HexTile] Testing hover effect on {gameObject.name}");
        OnHover();
        yield return new WaitForSeconds(1f);
        OnHoverExit();
        Debug.Log($"[HexTile] Hover effect test completed");
    }
#endif

    #region IInteractable Implementation
    
    public void OnHover()
    {
        // Apply hover animation to tiles that should show hover effects (excludes pathway and edge spawn)
        if (hexType != HexType.Pathway && hexType != HexType.EdgeSpawn)
        {
            // Ensure we have the original position stored
            if (originalPosition == Vector3.zero)
                originalPosition = transform.position;

            Vector3 hoverPosition = originalPosition;
            hoverPosition.y += hoverYOffset;

            // Use TransformSpring only for unoccupied defender tiles
            if (hexType == HexType.DefenderSpot && !isOccupied && transformSpring != null)
            {
                transformSpring.SetTargetPosition(hoverPosition);
                Debug.Log($"[HexTile] Hovering over unoccupied defender hex {coordinates} - Spring target set to Y: {hoverPosition.y:F2}");
            }
            else
            {
                // Direct position setting for all other hoverable tiles (CenterHub, Environment, occupied defenders)
                transform.position = hoverPosition;
                Debug.Log($"[HexTile] Hovering over hex {coordinates} (Type: {hexType}, Occupied: {isOccupied}) - Position set to Y: {hoverPosition.y:F2} (direct)");
            }
        }

        // Only show preview for valid buildable tiles when in build mode
        if (ShouldShowPreview())
        {
            previewObject.SetActive(true);
            Debug.Log($"[HexTile] Preview activated for buildable hex {coordinates} (Build Mode: {isBuildModeActive})");
        }
    }

    public void OnHoverExit()
    {
        // Reset position for tiles that show hover effects (excludes pathway and edge spawn)
        if (hexType != HexType.Pathway && hexType != HexType.EdgeSpawn)
        {
            // Ensure we have the original position stored
            if (originalPosition == Vector3.zero)
                originalPosition = transform.position - new Vector3(0, hoverYOffset, 0); // Estimate original if not stored

            // Use TransformSpring only for unoccupied defender tiles
            if (hexType == HexType.DefenderSpot && !isOccupied && transformSpring != null)
            {
                transformSpring.SetTargetPosition(originalPosition);
                Debug.Log($"[HexTile] Stopped hovering over unoccupied defender hex {coordinates} - Spring target reset to Y: {originalPosition.y:F2}");
            }
            else
            {
                // Direct position setting for all other hoverable tiles (CenterHub, Environment, occupied defenders)
                transform.position = originalPosition;
                Debug.Log($"[HexTile] Stopped hovering over hex {coordinates} (Type: {hexType}, Occupied: {isOccupied}) - Position reset to Y: {originalPosition.y:F2} (direct)");
            }
        }

        // Only hide preview object if it was potentially showing (build mode check)
        if (previewObject.activeInHierarchy)
        {
            previewObject.SetActive(false);
            Debug.Log($"[HexTile] Preview deactivated for hex {coordinates}");
        }
    }

    public void OnClick()
    {
        Debug.Log($"[HexTile] Clicked on hex {coordinates} (Type: {hexType})");

        // Additional click logic can be added here if needed
        if (CanBuild())
        {
            Debug.Log($"[HexTile] Clicked on defender spot - could place building here!");
        }
        else
        { 
            Debug.Log($"[HexTile] Invalid Build - Type: {hexType}, Occupied: {isOccupied}");
        }
    }

    #endregion
}
