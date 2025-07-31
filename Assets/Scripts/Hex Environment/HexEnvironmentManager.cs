using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

[System.Serializable]
public struct HexCoordinates
{
    public int q; // Column
    public int r; // Row
    
    public HexCoordinates(int q, int r)
    {
        this.q = q;
        this.r = r;
    }
    
    public static HexCoordinates Zero => new HexCoordinates(0, 0);
    
    public override string ToString() => $"({q}, {r})";
    
    public override bool Equals(object obj)
    {
        if (obj is HexCoordinates other)
            return q == other.q && r == other.r;
        return false;
    }
    
    public override int GetHashCode()
    {
        return q.GetHashCode() ^ (r.GetHashCode() << 2);
    }
    
    public static bool operator ==(HexCoordinates a, HexCoordinates b)
    {
        return a.q == b.q && a.r == b.r;
    }
    
    public static bool operator !=(HexCoordinates a, HexCoordinates b)
    {
        return !(a == b);
    }
}

public enum HexType
{
    [LabelText("🌿 Environment (Decorative)")]
    Environment,
    
    [LabelText("🏠 Center Hub")]
    CenterHub,
    
    [LabelText("🛤️ Pathway (Enemy Route)")]
    Pathway,
    
    [LabelText("🛡️ Defender Spot")]
    DefenderSpot,
    
    [LabelText("🚪 Edge Spawn")]
    EdgeSpawn
}

[System.Serializable]
public class HexData
{
    public HexCoordinates coordinates;
    public HexType type;
    public GameObject gameObject;
    public int laneId = -1; // Which lane this hex belongs to (-1 = no lane)
    public bool isJunctionPoint = false; // Where lanes merge
    
    public HexData(HexCoordinates coords, HexType hexType)
    {
        coordinates = coords;
        type = hexType;
    }
}

[System.Serializable]
public class LaneConfiguration
{
    [TitleGroup("Basic Settings")]
    [LabelText("🎯 Lane Active")]
    public bool isActive = true;
    
    [TitleGroup("Basic Settings")]
    [LabelText("🧭 Target Direction"), Range(0f, 360f)]
    public float direction = 0f; // Degrees from center
    
    [TitleGroup("Basic Settings")]
    [LabelText("📏 Lane Length"), Range(2, 15)]
    public int length = 5;
    
    [TitleGroup("Basic Settings")]
    [LabelText("🎨 Lane Color")]
    public Color laneColor = Color.yellow;
    
    [TitleGroup("Path Behavior")]
    [LabelText("🌊 Path Curviness"), Range(0f, 1f)]
    [Tooltip("How much the path can curve (0 = straight, 1 = very curvy)")]
    public float curviness = 0.3f;
    
    [TitleGroup("Path Behavior")]
    [LabelText("🎲 Randomness Factor"), Range(0f, 1f)]
    [Tooltip("How much randomness to apply to path generation")]
    public float randomnessFactor = 0.2f;
    
    [TitleGroup("Lane Merging")]
    [LabelText("🔗 Allow Lane Merging")]
    public bool allowMerging = false;
    
    [TitleGroup("Lane Merging")]
    [ShowIf("allowMerging")]
    [LabelText("🎯 Merge with Lane"), Range(-1, 5)]
    [Tooltip("-1 = no specific target, will find best lane to merge with")]
    public int mergeWithLane = -1; // -1 = auto-find merge target
    
    [TitleGroup("Lane Merging")]
    [ShowIf("allowMerging")]
    [LabelText("📍 Merge at Distance"), Range(1, 10)]
    [Tooltip("How far from center to attempt merging")]
    public int mergeAtDistance = 3;
    
    [TitleGroup("Lane Merging")]
    [ShowIf("allowMerging")]
    [LabelText("🎯 Merge Probability"), Range(0f, 1f)]
    [Tooltip("Chance of actually merging when merge conditions are met")]
    public float mergeProbability = 0.7f;
}

public class HexEnvironmentManager : MonoBehaviour
{
    [TabGroup("Grid", "🗺️ Grid Setup")]
    [TitleGroup("Grid/Basic Grid Settings")]
    [SerializeField, Range(1, 20), Tooltip("Number of hex rings around the center hex")]
    private int gridRadius = 5;
    
    [TabGroup("Grid", "🗺️ Grid Setup")]
    [TitleGroup("Grid/Basic Grid Settings")]
    [SerializeField, Range(0.1f, 10f), Tooltip("Size of each hexagon")]
    private float hexSize = 1f;
    
    [TabGroup("Grid", "🗺️ Grid Setup")]
    [TitleGroup("Grid/Basic Grid Settings")]
    [SerializeField, Range(0f, 0.5f), Tooltip("Gap between hexagons (0 = touching)")]
    private float hexSpacing = 0.05f;
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Lane Generation")]
    [SerializeField, Range(1, 6), Tooltip("Number of lanes to generate")]
    private int numberOfLanes = 3;
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Lane Generation")]
    [SerializeField, Tooltip("Seed for deterministic generation (0 = random)")]
    private int generationSeed = 0;
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Lane Generation")]
    [SerializeField, Tooltip("Configuration for each lane")]
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = false, ShowPaging = false)]
    private List<LaneConfiguration> laneConfigurations = new List<LaneConfiguration>();
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Gameplay Features")]
    [SerializeField, Tooltip("Automatically generate defender spots adjacent to pathways")]
    private bool autoGenerateDefenderSpots = true;
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Gameplay Features")]
    [SerializeField, Tooltip("Generate one spawn point per lane at the outermost hex")]
    private bool generateEdgeSpawns = true;
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Gameplay Features")]
    [SerializeField, Tooltip("Generate only gizmos without instantiating GameObjects")]
    private bool gizmosOnlyMode = false;
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Generation Actions")]
    [Button(ButtonSizes.Large, Name = "🚀 Generate Tower Defense Environment")]
    [GUIColor(0.4f, 0.8f, 0.4f)]
    private void GenerateButtonPressed()
    {
        GenerateTowerDefenseEnvironment();
    }
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Generation Actions")]
    [Button(ButtonSizes.Medium, Name = "🎲 Randomize Seed")]
    [GUIColor(0.8f, 0.8f, 0.4f)]
    private void RandomizeButtonPressed()
    {
        RandomizeSeed();
    }
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Generation Actions")]
    [Button(ButtonSizes.Medium, Name = "🗑️ Clear Environment")]
    [GUIColor(0.8f, 0.4f, 0.4f)]
    private void ClearButtonPressed()
    {
        ClearExistingHexes();
    }
    
    [TabGroup("Prefabs", "🎮 Prefabs")]
    [TitleGroup("Prefabs/Required Prefabs")]
    [SerializeField, Tooltip("Default hexagon prefab (optional - leave null for gizmos only)")]
    private GameObject hexPrefab;
    
    [TabGroup("Prefabs", "🎮 Prefabs")]
    [TitleGroup("Prefabs/Optional Overrides")]
    [SerializeField, Tooltip("Center hub prefab (optional)")]
    private GameObject centerHubPrefab;
    
    [TabGroup("Prefabs", "🎮 Prefabs")]
    [TitleGroup("Prefabs/Optional Overrides")]
    [SerializeField, Tooltip("Pathway prefab (optional)")]
    private GameObject pathwayPrefab;
    
    [TabGroup("Prefabs", "🎮 Prefabs")]
    [TitleGroup("Prefabs/Optional Overrides")]
    [SerializeField, Tooltip("Defender spot prefab (optional)")]
    private GameObject defenderSpotPrefab;
    
    [TabGroup("Prefabs", "🎮 Prefabs")]
    [TitleGroup("Prefabs/Optional Overrides")]
    [SerializeField, Tooltip("Edge spawn prefab (optional)")]
    private GameObject edgeSpawnPrefab;
    
    [TabGroup("Prefabs", "🎮 Prefabs")]
    [TitleGroup("Prefabs/Hierarchy Settings")]
    [SerializeField, Tooltip("Optional parent transform for spawned hexagons")]
    private Transform hexParent;
    
    [TabGroup("Prefabs", "🎮 Prefabs")]
    [TitleGroup("Prefabs/Generation Settings")]
    [SerializeField, Tooltip("Generate environment automatically on Start")]
    private bool generateOnStart = true;
    
    [TabGroup("Debug", "🔧 Debug & Visualization")]
    [TitleGroup("Debug/Gizmo Display Settings")]
    [SerializeField, Tooltip("Show gizmos for hex positions")]
    private bool showGizmos = true;
    
    [TabGroup("Debug", "🔧 Debug & Visualization")]
    [TitleGroup("Debug/Gizmo Display Settings")]
    [SerializeField, Tooltip("Show hex type labels in scene view")]
    private bool showTypeLabels = true;
    
    [TabGroup("Debug", "🔧 Debug & Visualization")]
    [TitleGroup("Debug/Gizmo Display Settings")]
    [SerializeField, Tooltip("Show filled gizmos instead of wireframe")]
    private bool showFilledGizmos = false;
    
    [TabGroup("Debug", "🔧 Debug & Visualization")]
    [TitleGroup("Debug/Gizmo Display Settings")]
    [SerializeField, Tooltip("Show lane connections between hexes")]
    private bool showLaneConnections = true;
    
    [TabGroup("Debug", "🔧 Debug & Visualization")]
    [TitleGroup("Debug/Gizmo Display Settings")]
    [SerializeField, Tooltip("Gizmo transparency"), Range(0.1f, 1f)]
    private float gizmoAlpha = 0.7f;
    
    [TabGroup("Debug", "🔧 Debug & Visualization")]
    [TitleGroup("Debug/Hex Type Visibility")]
    [SerializeField, Tooltip("Show center hub hexes")]
    private bool showCenterHub = true;
    
    [TabGroup("Debug", "🔧 Debug & Visualization")]
    [TitleGroup("Debug/Hex Type Visibility")]
    [SerializeField, Tooltip("Show pathway hexes")]
    private bool showPathways = true;
    
    [TabGroup("Debug", "🔧 Debug & Visualization")]
    [TitleGroup("Debug/Hex Type Visibility")]
    [SerializeField, Tooltip("Show defender spot hexes")]
    private bool showDefenderSpots = true;
    
    [TabGroup("Debug", "🔧 Debug & Visualization")]
    [TitleGroup("Debug/Hex Type Visibility")]
    [SerializeField, Tooltip("Show edge spawn hexes")]
    private bool showEdgeSpawns = true;
    
    [TabGroup("Debug", "🔧 Debug & Visualization")]
    [TitleGroup("Debug/Hex Type Visibility")]
    [SerializeField, Tooltip("Show environment hexes")]
    private bool showEnvironment = false;
    
    [TabGroup("Debug", "🔧 Debug & Visualization")]
    [TitleGroup("Debug/Hex Colors")]
    [SerializeField, Tooltip("Color for center hub")]
    private Color centerHubColor = new Color(0.2f, 1f, 0.2f, 1f);
    
    [TabGroup("Debug", "🔧 Debug & Visualization")]
    [TitleGroup("Debug/Hex Colors")]
    [SerializeField, Tooltip("Color for defender spots")]
    private Color defenderSpotColor = new Color(1f, 0.3f, 0.3f, 1f);
    
    [TabGroup("Debug", "🔧 Debug & Visualization")]
    [TitleGroup("Debug/Hex Colors")]
    [SerializeField, Tooltip("Color for edge spawn points")]
    private Color edgeSpawnColor = new Color(0.3f, 0.6f, 1f, 1f);
    
    [TabGroup("Debug", "🔧 Debug & Visualization")]
    [TitleGroup("Debug/Hex Colors")]
    [SerializeField, Tooltip("Color for environment hexes")]
    private Color environmentColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    
    [TabGroup("Debug", "🔧 Debug & Visualization")]
    [TitleGroup("Debug/Advanced Options")]
    [SerializeField, Tooltip("Show hex coordinates")]
    private bool showCoordinates = false;
    
    [TabGroup("Debug", "🔧 Debug & Visualization")]
    [TitleGroup("Debug/Advanced Options")]
    [SerializeField, Tooltip("Show only specific lane (0-5, -1 = all)"), Range(-1, 5)]
    private int showOnlyLane = -1;
    
    // Runtime data
    private Dictionary<HexCoordinates, HexData> hexGrid = new Dictionary<HexCoordinates, HexData>();
    private List<HexCoordinates> generatedCoordinates = new List<HexCoordinates>();
    private List<List<HexCoordinates>> generatedLanes = new List<List<HexCoordinates>>();
    private List<HexCoordinates> laneSpawnPoints = new List<HexCoordinates>(); // One spawn per lane
    
    // Hex math constants
    private readonly float SQRT_3 = Mathf.Sqrt(3f);
    
    // Hex direction vectors (for pathfinding)
    private readonly HexCoordinates[] HEX_DIRECTIONS = {
        new HexCoordinates(1, 0),   // E
        new HexCoordinates(0, 1),   // NE
        new HexCoordinates(-1, 1),  // NW
        new HexCoordinates(-1, 0),  // W
        new HexCoordinates(0, -1),  // SW
        new HexCoordinates(1, -1)   // SE
    };
    
    #region Unity Events
    
    private void Start()
    {
        InitializeLaneConfigurations();
        
        if (generateOnStart)
        {
            GenerateTowerDefenseEnvironment();
        }
    }
    
    private void OnValidate()
    {
        InitializeLaneConfigurations();
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeLaneConfigurations()
    {
        // Ensure we have the right number of lane configurations
        while (laneConfigurations.Count < numberOfLanes)
        {
            float direction = (360f / numberOfLanes) * laneConfigurations.Count;
            laneConfigurations.Add(new LaneConfiguration
            {
                direction = direction,
                laneColor = Color.HSVToRGB((float)laneConfigurations.Count / numberOfLanes, 0.8f, 1f),
                curviness = 0.3f,
                randomnessFactor = 0.2f,
                allowMerging = false
            });
        }
        
        // Remove excess configurations
        if (laneConfigurations.Count > numberOfLanes)
        {
            laneConfigurations.RemoveRange(numberOfLanes, laneConfigurations.Count - numberOfLanes);
        }
    }
    
    #endregion
    
    #region Public Methods
    
    public void GenerateTowerDefenseEnvironment()
    {
        ClearExistingHexes();
        InitializeLaneConfigurations();
        
        // Initialize random seed
        if (generationSeed == 0)
        {
            generationSeed = Random.Range(1, int.MaxValue);
        }
        Random.InitState(generationSeed);
        
        // Check if we're in gizmos-only mode or if hex prefab is missing
        bool canInstantiateObjects = !gizmosOnlyMode && hexPrefab != null;
        
        if (!canInstantiateObjects)
        {
            Debug.Log($"[HexEnvironmentManager] Running in gizmos-only mode. {(gizmosOnlyMode ? "Gizmos-only mode enabled." : "Hex prefab is null.")}");
        }
        
        // Setup parent if not assigned and we're instantiating objects
        if (canInstantiateObjects && hexParent == null)
        {
            GameObject parentGO = new GameObject("Tower Defense Grid");
            parentGO.transform.SetParent(transform);
            hexParent = parentGO.transform;
        }
        
        // Step 1: Generate all hex coordinates
        GenerateAllHexCoordinates();
        
        // Step 2: Generate lanes from center outwards
        GenerateLanes();
        
        // Step 3: Generate defender spots adjacent to pathways
        if (autoGenerateDefenderSpots)
            GenerateDefenderSpots();
            
        // Step 4: Generate edge spawn points (one per lane)
        if (generateEdgeSpawns)
            GenerateEdgeSpawnPoints();
            
        // Step 5: Instantiate GameObjects (only if not in gizmos-only mode and prefab exists)
        if (canInstantiateObjects)
        {
            InstantiateHexGameObjects();
        }
        
        string modeText = canInstantiateObjects ? "with GameObjects" : "gizmos-only";
        Debug.Log($"[HexEnvironmentManager] Generated tower defense environment {modeText} with {hexGrid.Count} hexagons, {generatedLanes.Count} lanes (Seed: {generationSeed})");
    }
    
    [TabGroup("TD", "🏰 Tower Defense")]
    public void RandomizeSeed()
    {
        generationSeed = Random.Range(1, int.MaxValue);
        Debug.Log($"[HexEnvironmentManager] New seed: {generationSeed}");
    }
    
    public void ClearExistingHexes()
    {
        // Clear runtime collections
        hexGrid.Clear();
        generatedCoordinates.Clear();
        generatedLanes.Clear();
        laneSpawnPoints.Clear();
        
        // Destroy existing hex objects
        if (hexParent != null)
        {
            for (int i = hexParent.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                    Destroy(hexParent.GetChild(i).gameObject);
                else
                    DestroyImmediate(hexParent.GetChild(i).gameObject);
            }
        }
    }
    
    public HexData GetHexAt(HexCoordinates coord)
    {
        hexGrid.TryGetValue(coord, out HexData hex);
        return hex;
    }
    
    public GameObject GetHexGameObjectAt(HexCoordinates coord)
    {
        var hexData = GetHexAt(coord);
        return hexData?.gameObject;
    }
    
    public List<HexData> GetHexesOfType(HexType type)
    {
        return hexGrid.Values.Where(hex => hex.type == type).ToList();
    }
    
    public List<HexData> GetLaneHexes(int laneId)
    {
        return hexGrid.Values.Where(hex => hex.laneId == laneId).ToList();
    }
    
    public HexCoordinates WorldToHex(Vector3 worldPosition)
    {
        Vector3 localPos = worldPosition - transform.position;
        float size = hexSize * (1f + hexSpacing);
        
        // Convert world position to axial coordinates (flat-top orientation)
        float q = (2f/3f * localPos.x) / size;
        float r = (-1f/3f * localPos.x + SQRT_3/3f * localPos.z) / size;
        
        return CubeToHex(CubeRound(q, -q-r, r));
    }
    
    #endregion
    
    #region Tower Defense Generation
    
    private void GenerateAllHexCoordinates()
    {
        // Generate all hex coordinates in rings from center outwards
        for (int ring = 0; ring <= gridRadius; ring++)
        {
            List<HexCoordinates> ringCoords = GetHexRing(HexCoordinates.Zero, ring);
            
            foreach (var coord in ringCoords)
            {
                // Start as environment, will be changed by lane generation
                HexType type = ring == 0 ? HexType.CenterHub : HexType.Environment;
                HexData hexData = new HexData(coord, type);
                
                hexGrid[coord] = hexData;
                generatedCoordinates.Add(coord);
            }
        }
    }
    
    private void GenerateLanes()
    {
        generatedLanes.Clear();
        
        for (int laneIndex = 0; laneIndex < numberOfLanes; laneIndex++)
        {
            if (!laneConfigurations[laneIndex].isActive) continue;
            
            var config = laneConfigurations[laneIndex];
            List<HexCoordinates> laneCoords = new List<HexCoordinates>();
            
            // Calculate direction vector with curviness and randomness
            float baseDirection = config.direction;
            float directionVariance = config.randomnessFactor * Random.Range(-20f, 20f);
            float radians = (baseDirection + directionVariance) * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(radians), 0, Mathf.Sin(radians));
            
            // Generate lane path from center outwards
            HexCoordinates currentHex = HexCoordinates.Zero;
            laneCoords.Add(currentHex);
            Vector3 currentDirection = direction;
            
            for (int distance = 1; distance <= config.length; distance++)
            {
                // Apply curviness - gradually change direction
                if (config.curviness > 0f && distance > 1)
                {
                    float curveAmount = config.curviness * Random.Range(-30f, 30f) * Mathf.Deg2Rad;
                    float newAngle = Mathf.Atan2(currentDirection.z, currentDirection.x) + curveAmount;
                    currentDirection = new Vector3(Mathf.Cos(newAngle), 0, Mathf.Sin(newAngle));
                }
                
                // Find the hex closest to our desired direction
                HexCoordinates nextHex = FindClosestHexInDirection(currentHex, currentDirection);
                
                // Add some randomness to path selection based on randomness factor
                if (config.randomnessFactor > 0f && Random.value < config.randomnessFactor && distance > 1)
                {
                    var alternatives = GetAlternativeDirections(currentHex, currentDirection);
                    if (alternatives.Count > 0)
                    {
                        nextHex = alternatives[Random.Range(0, alternatives.Count)];
                    }
                }
                
                // Check for lane merging if enabled
                if (config.allowMerging && distance >= config.mergeAtDistance && 
                    Random.value < config.mergeProbability)
                {
                    HexCoordinates mergeTarget = FindBestMergeTarget(currentHex, config, laneIndex);
                    if (mergeTarget != currentHex)
                    {
                        nextHex = mergeTarget;
                    }
                }
                
                if (hexGrid.ContainsKey(nextHex))
                {
                    laneCoords.Add(nextHex);
                    currentHex = nextHex;
                }
                else
                {
                    break; // Reached edge of grid
                }
            }
            
            // Apply lane to hex data
            foreach (var coord in laneCoords)
            {
                if (hexGrid.ContainsKey(coord))
                {
                    hexGrid[coord].type = coord == HexCoordinates.Zero ? HexType.CenterHub : HexType.Pathway;
                    hexGrid[coord].laneId = laneIndex;
                }
            }
            
            generatedLanes.Add(laneCoords);
        }
    }
    
    private HexCoordinates FindBestMergeTarget(HexCoordinates currentHex, LaneConfiguration config, int currentLaneIndex)
    {
        // If specific merge target is set, try to merge with that lane
        if (config.mergeWithLane >= 0 && config.mergeWithLane < generatedLanes.Count && config.mergeWithLane != currentLaneIndex)
        {
            var targetLane = generatedLanes[config.mergeWithLane];
            if (targetLane.Count > 1)
            {
                // Find closest hex from target lane
                HexCoordinates closestHex = targetLane[1]; // Skip center
                float closestDistance = float.MaxValue;
                
                foreach (var coord in targetLane.Skip(1))
                {
                    float distance = HexDistance(currentHex, coord);
                    if (distance < closestDistance && distance <= 2) // Only merge if within 2 hex distance
                    {
                        closestDistance = distance;
                        closestHex = coord;
                    }
                }
                
                if (closestDistance <= 2)
                    return closestHex;
            }
        }
        
        // Auto-find merge target from any existing lane
        foreach (var lane in generatedLanes)
        {
            foreach (var coord in lane.Skip(1)) // Skip center hex
            {
                float distance = HexDistance(currentHex, coord);
                if (distance <= 2 && hexGrid[coord].type == HexType.Pathway)
                {
                    return coord;
                }
            }
        }
        
        return currentHex; // No merge target found
    }
    
    private float HexDistance(HexCoordinates a, HexCoordinates b)
    {
        return (Mathf.Abs(a.q - b.q) + Mathf.Abs(a.q + a.r - b.q - b.r) + Mathf.Abs(a.r - b.r)) / 2f;
    }
    
    private void GenerateDefenderSpots()
    {
        var pathwayHexes = hexGrid.Values.Where(hex => hex.type == HexType.Pathway).ToList();
        
        foreach (var pathwayHex in pathwayHexes)
        {
            // Check all 6 adjacent hexes
            foreach (var direction in HEX_DIRECTIONS)
            {
                HexCoordinates adjacentCoord = new HexCoordinates(
                    pathwayHex.coordinates.q + direction.q,
                    pathwayHex.coordinates.r + direction.r
                );
                
                if (hexGrid.ContainsKey(adjacentCoord) && 
                    hexGrid[adjacentCoord].type == HexType.Environment)
                {
                    hexGrid[adjacentCoord].type = HexType.DefenderSpot;
                }
            }
        }
    }
    
    private void GenerateEdgeSpawnPoints()
    {
        laneSpawnPoints.Clear();
        
        // For each lane, find the outermost hex and randomly pick an adjacent edge hex as spawn
        for (int laneIndex = 0; laneIndex < generatedLanes.Count; laneIndex++)
        {
            var lane = generatedLanes[laneIndex];
            if (lane.Count < 2) continue;
            
            // Get the outermost hex of this lane
            HexCoordinates outermostHex = lane[lane.Count - 1];
            
            // Find all edge hexes adjacent to the outermost pathway hex
            List<HexCoordinates> possibleSpawns = new List<HexCoordinates>();
            
            foreach (var direction in HEX_DIRECTIONS)
            {
                HexCoordinates adjacentCoord = new HexCoordinates(
                    outermostHex.q + direction.q,
                    outermostHex.r + direction.r
                );
                
                // Check if this hex is on the grid edge
                int distance = Mathf.Max(Mathf.Abs(adjacentCoord.q), 
                                       Mathf.Abs(adjacentCoord.r), 
                                       Mathf.Abs(-adjacentCoord.q - adjacentCoord.r));
                
                if (distance <= gridRadius && hexGrid.ContainsKey(adjacentCoord) && 
                    hexGrid[adjacentCoord].type == HexType.Environment)
                {
                    possibleSpawns.Add(adjacentCoord);
                }
            }
            
            // Randomly select one spawn point for this lane
            if (possibleSpawns.Count > 0)
            {
                HexCoordinates selectedSpawn = possibleSpawns[Random.Range(0, possibleSpawns.Count)];
                hexGrid[selectedSpawn].type = HexType.EdgeSpawn;
                hexGrid[selectedSpawn].laneId = laneIndex;
                laneSpawnPoints.Add(selectedSpawn);
            }
        }
    }
    
    private void InstantiateHexGameObjects()
    {
        if (hexPrefab == null)
        {
            Debug.LogWarning("[HexEnvironmentManager] Cannot instantiate GameObjects - hex prefab is null. Use gizmos for visualization.");
            return;
        }
        
        foreach (var kvp in hexGrid)
        {
            var coord = kvp.Key;
            var hexData = kvp.Value;
            
            Vector3 worldPos = HexToWorldPosition(coord);
            GameObject prefabToUse = GetPrefabForHexType(hexData.type);
            
            if (prefabToUse != null)
            {
                GameObject hexGO = Instantiate(prefabToUse, worldPos, Quaternion.identity, hexParent);
                hexGO.name = $"Hex_{coord}_{hexData.type}";
                hexData.gameObject = hexGO;
            }
        }
    }
    
    private GameObject GetPrefabForHexType(HexType type)
    {
        return type switch
        {
            HexType.CenterHub => centerHubPrefab ?? hexPrefab,
            HexType.Pathway => pathwayPrefab ?? hexPrefab,
            HexType.DefenderSpot => defenderSpotPrefab ?? hexPrefab,
            HexType.EdgeSpawn => edgeSpawnPrefab ?? hexPrefab,
            _ => hexPrefab
        };
    }
    
    private HexCoordinates FindClosestHexInDirection(HexCoordinates from, Vector3 worldDirection)
    {
        HexCoordinates bestHex = from;
        float bestDot = -1f;
        
        foreach (var direction in HEX_DIRECTIONS)
        {
            HexCoordinates candidate = new HexCoordinates(from.q + direction.q, from.r + direction.r);
            
            if (!hexGrid.ContainsKey(candidate)) continue;
            
            Vector3 candidateWorld = HexToWorldPosition(candidate);
            Vector3 fromWorld = HexToWorldPosition(from);
            Vector3 hexDirection = (candidateWorld - fromWorld).normalized;
            
            float dot = Vector3.Dot(hexDirection, worldDirection);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestHex = candidate;
            }
        }
        
        return bestHex;
    }
    
    private List<HexCoordinates> GetAlternativeDirections(HexCoordinates from, Vector3 preferredDirection)
    {
        List<HexCoordinates> alternatives = new List<HexCoordinates>();
        
        foreach (var direction in HEX_DIRECTIONS)
        {
            HexCoordinates candidate = new HexCoordinates(from.q + direction.q, from.r + direction.r);
            
            if (!hexGrid.ContainsKey(candidate)) continue;
            
            Vector3 candidateWorld = HexToWorldPosition(candidate);
            Vector3 fromWorld = HexToWorldPosition(from);
            Vector3 hexDirection = (candidateWorld - fromWorld).normalized;
            
            // Only consider directions that are somewhat aligned (dot > 0.3)
            float dot = Vector3.Dot(hexDirection, preferredDirection);
            if (dot > 0.3f)
            {
                alternatives.Add(candidate);
            }
        }
        
        return alternatives;
    }
    
    #endregion
    
    private Vector3 HexToWorldPosition(HexCoordinates hex)
    {
        // For flat-top hexagons (as shown in image)
        float size = hexSize * (1f + hexSpacing);
        
        // Correct hexagonal grid positioning for flat-top orientation
        float x = size * (3f/2f * hex.q);
        float z = size * (SQRT_3/2f * hex.q + SQRT_3 * hex.r);
        
        return transform.position + new Vector3(x, 0, z);
    }
    
    private List<HexCoordinates> GetHexRing(HexCoordinates center, int radius)
    {
        List<HexCoordinates> results = new List<HexCoordinates>();
        
        if (radius == 0)
        {
            results.Add(center);
            return results;
        }
        
        // Start at the "right" corner of the ring and walk counter-clockwise
        HexCoordinates hex = new HexCoordinates(center.q + radius, center.r);
        
        // Six directions for hexagonal movement (axial coordinates)
        HexCoordinates[] directions = {
            new HexCoordinates(-1, 1),  // NW
            new HexCoordinates(-1, 0),  // W  
            new HexCoordinates(0, -1),  // SW
            new HexCoordinates(1, -1),  // SE
            new HexCoordinates(1, 0),   // E
            new HexCoordinates(0, 1)    // NE
        };
        
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < radius; j++)
            {
                results.Add(hex);
                hex = new HexCoordinates(hex.q + directions[i].q, hex.r + directions[i].r);
            }
        }
        
        return results;
    }
    
    private HexCoordinates CubeToHex(Vector3 cube)
    {
        return new HexCoordinates((int)cube.x, (int)cube.z);
    }
    
    private Vector3 CubeRound(float x, float y, float z)
    {
        int rx = Mathf.RoundToInt(x);
        int ry = Mathf.RoundToInt(y);
        int rz = Mathf.RoundToInt(z);
        
        float x_diff = Mathf.Abs(rx - x);
        float y_diff = Mathf.Abs(ry - y);
        float z_diff = Mathf.Abs(rz - z);
        
        if (x_diff > y_diff && x_diff > z_diff)
            rx = -ry - rz;
        else if (y_diff > z_diff)
            ry = -rx - rz;
        else
            rz = -rx - ry;
        
        return new Vector3(rx, ry, rz);
    }
    

    
    #region Gizmos
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Show hex grid both in play mode and edit mode after generation
        if (hexGrid.Count > 0)
        {
            // Draw lane connections first (behind hexes)
            if (showLaneConnections && showPathways)
                DrawLaneConnections();
            
            // Draw existing hexes with type-based colors and visibility
            foreach (var kvp in hexGrid)
            {
                var coord = kvp.Key;
                var hexData = kvp.Value;
                
                // Check visibility for this hex type
                if (!ShouldShowHexType(hexData.type, hexData.laneId)) continue;
                
                Vector3 pos = HexToWorldPosition(coord);
                
                // Get enhanced color for hex type - ensure solid color
                Color hexColor = GetEnhancedColorForHexType(hexData.type, hexData.laneId);
                hexColor.a = gizmoAlpha;
                Gizmos.color = hexColor;
                
                // Draw hex with special styling based on type
                DrawEnhancedHexGizmo(pos, hexSize, hexData.type, hexData.laneId);
                
                // Draw type labels
                if (showTypeLabels)
                {
                    #if UNITY_EDITOR
                    string label = GetHexTypeLabel(hexData.type, hexData.laneId);
                    if (showCoordinates)
                        label += $"\n{hexData.coordinates}";
                    UnityEditor.Handles.Label(pos + Vector3.up * (hexSize * 0.2f), label);
                    #endif
                }
            }
        }
        else
        {
            // Preview mode in editor - show basic grid with rainbow effect
            for (int ring = 0; ring <= gridRadius; ring++)
            {
                List<HexCoordinates> ringCoords = GetHexRing(HexCoordinates.Zero, ring);
                
                foreach (var coord in ringCoords)
                {
                    Vector3 pos = HexToWorldPosition(coord);
                    
                    // Rainbow preview effect
                    float hue = (ring * 0.15f + Vector3.Angle(pos, Vector3.right) / 360f) % 1f;
                    Color previewColor = Color.HSVToRGB(hue, 0.6f, 0.9f);
                    previewColor.a = gizmoAlpha;
                    
                    if (coord == HexCoordinates.Zero)
                        previewColor = Color.white;
                    
                    Gizmos.color = previewColor;
                    DrawEnhancedHexGizmo(pos, hexSize, HexType.Environment, -1);
                }
            }
        }
    }
    
    private bool ShouldShowHexType(HexType type, int laneId)
    {
        // Check lane-specific visibility
        if (showOnlyLane >= 0 && type == HexType.Pathway && laneId != showOnlyLane)
            return false;
        
        // Check type-specific visibility
        return type switch
        {
            HexType.CenterHub => showCenterHub,
            HexType.Pathway => showPathways,
            HexType.DefenderSpot => showDefenderSpots,
            HexType.EdgeSpawn => showEdgeSpawns,
            HexType.Environment => showEnvironment,
            _ => true
        };
    }
    
    private void DrawLaneConnections()
    {
        for (int laneIndex = 0; laneIndex < generatedLanes.Count; laneIndex++)
        {
            var lane = generatedLanes[laneIndex];
            if (lane.Count < 2) continue;
            
            // Skip if only showing specific lane and this isn't it
            if (showOnlyLane >= 0 && laneIndex != showOnlyLane) continue;
            
            // Get lane color
            Color connectionColor = laneIndex < laneConfigurations.Count ? 
                                  laneConfigurations[laneIndex].laneColor : Color.yellow;
            connectionColor.a = 0.5f;
            Gizmos.color = connectionColor;
            
            // Draw connections between lane hexes
            for (int i = 0; i < lane.Count - 1; i++)
            {
                Vector3 from = HexToWorldPosition(lane[i]) + Vector3.up * (hexSize * 0.1f);
                Vector3 to = HexToWorldPosition(lane[i + 1]) + Vector3.up * (hexSize * 0.1f);
                
                // Draw thick line scaled to hex size
                DrawThickLine(from, to, hexSize * 0.1f);
                
                // Draw arrow at end scaled to hex size
                if (i == lane.Count - 2)
                {
                    Vector3 direction = (to - from).normalized;
                    DrawArrow(to, direction, hexSize * 0.3f);
                }
            }
        }
    }
    
    private Color GetEnhancedColorForHexType(HexType type, int laneId = -1)
    {
        return type switch
        {
            HexType.CenterHub => centerHubColor,
            HexType.Pathway => GetLaneColor(laneId),
            HexType.DefenderSpot => defenderSpotColor,
            HexType.EdgeSpawn => edgeSpawnColor,
            _ => environmentColor
        };
    }
    
    private Color GetLaneColor(int laneId)
    {
        if (laneId >= 0 && laneId < laneConfigurations.Count)
        {
            return laneConfigurations[laneId].laneColor;
        }
        
        // Fallback rainbow colors for lanes without configuration
        float hue = (laneId * 0.618f) % 1f; // Golden ratio for even distribution
        return Color.HSVToRGB(hue, 0.8f, 1f);
    }
    
    private string GetHexTypeLabel(HexType type, int laneId)
    {
        return type switch
        {
            HexType.CenterHub => "🏠 HUB",
            HexType.Pathway => $"🛤️ L{laneId}",
            HexType.DefenderSpot => "🛡️ DEF",
            HexType.EdgeSpawn => "🚪 SPAWN",
            _ => "🌿 ENV"
        };
    }
    
    private void DrawEnhancedHexGizmo(Vector3 center, float size, HexType type, int laneId)
    {
        // Use the actual hex size from settings instead of passed parameter
        float actualHexSize = hexSize;
        
        Vector3[] corners = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = 60f * i * Mathf.Deg2Rad;
            corners[i] = center + new Vector3(
                actualHexSize * Mathf.Cos(angle),
                0,
                actualHexSize * Mathf.Sin(angle)
            );
        }
        
        // Set solid color for this hex - no gradients
        Color solidColor = GetEnhancedColorForHexType(type, laneId);
        solidColor.a = gizmoAlpha;
        Gizmos.color = solidColor;
        
        // Draw based on type and settings
        if (showFilledGizmos)
        {
            // Draw filled hexagon with solid color
            DrawSolidFilledHex(corners, center, solidColor);
        }
        
        // Always draw outline with type-specific styling
        DrawHexOutline(corners, type, solidColor);
        
        // Draw special markers based on type
        DrawTypeSpecificMarkers(center, actualHexSize, type, laneId, solidColor);
    }
    
    private void DrawSolidFilledHex(Vector3[] corners, Vector3 center, Color solidColor)
    {
        // Ensure solid color throughout
        Gizmos.color = solidColor;
        
        // Draw filled hexagon using Unity's mesh drawing for solid color
        #if UNITY_EDITOR
        UnityEditor.Handles.color = solidColor;
        
        // Draw filled polygon using Handles for better solid fill
        Vector3[] vertices = new Vector3[corners.Length];
        for (int i = 0; i < corners.Length; i++)
        {
            vertices[i] = corners[i];
        }
        UnityEditor.Handles.DrawAAConvexPolygon(vertices);
        #else
        // Fallback for runtime - draw triangular segments
        for (int i = 0; i < 6; i++)
        {
            Gizmos.color = solidColor;
            Vector3[] triangle = {
                center,
                corners[i],
                corners[(i + 1) % 6]
            };
            
            Gizmos.DrawLine(triangle[0], triangle[1]);
            Gizmos.DrawLine(triangle[1], triangle[2]);
            Gizmos.DrawLine(triangle[2], triangle[0]);
        }
        #endif
    }
    
    private void DrawHexOutline(Vector3[] corners, HexType type, Color solidColor)
    {
        // Ensure solid color for outline
        Gizmos.color = solidColor;
        
        float lineThickness = type switch
        {
            HexType.CenterHub => 0.15f,
            HexType.Pathway => 0.12f,
            HexType.DefenderSpot => 0.1f,
            HexType.EdgeSpawn => 0.08f,
            _ => 0.05f
        };
        
        // Draw hexagon outline with varying thickness
        for (int i = 0; i < 6; i++)
        {
            Vector3 from = corners[i];
            Vector3 to = corners[(i + 1) % 6];
            
            if (type == HexType.CenterHub || type == HexType.Pathway)
            {
                DrawThickLine(from, to, lineThickness, solidColor);
            }
            else
            {
                Gizmos.color = solidColor;
                Gizmos.DrawLine(from, to);
            }
        }
    }
    
    private void DrawTypeSpecificMarkers(Vector3 center, float size, HexType type, int laneId, Color solidColor)
    {
        // Ensure markers use the same solid color
        Gizmos.color = solidColor;
        
        switch (type)
        {
            case HexType.CenterHub:
                // Draw pulsing center sphere scaled to hex size
                float pulseSize = size * 0.3f * (1f + 0.2f * Mathf.Sin(Time.time * 3f));
                Gizmos.DrawWireSphere(center + Vector3.up * (size * 0.1f), pulseSize);
                break;
                
            case HexType.Pathway:
                // Draw direction arrow if part of a lane
                if (laneId >= 0 && laneId < generatedLanes.Count)
                {
                    var lane = generatedLanes[laneId];
                    var currentIndex = lane.FindIndex(coord => coord == WorldToHex(center));
                    if (currentIndex >= 0 && currentIndex < lane.Count - 1)
                    {
                        Vector3 nextPos = HexToWorldPosition(lane[currentIndex + 1]);
                        Vector3 direction = (nextPos - center).normalized;
                        DrawArrow(center + Vector3.up * (size * 0.05f), direction, size * 0.4f, solidColor);
                    }
                }
                break;
                
            case HexType.DefenderSpot:
                // Draw shield symbol scaled to hex size
                DrawShieldSymbol(center, size * 0.4f, solidColor);
                break;
                
            case HexType.EdgeSpawn:
                // Draw prominent cylinder gizmo for spawn points scaled to hex size
                Vector3 cylinderBottom = center;
                Vector3 cylinderTop = center + Vector3.up * (size * 0.8f);
                float cylinderRadius = size * 0.3f;
                
                // Draw cylinder wireframe
                Gizmos.DrawWireCube(center + Vector3.up * (size * 0.4f), new Vector3(cylinderRadius * 2f, size * 0.8f, cylinderRadius * 2f));
                
                // Draw top and bottom circles
                #if UNITY_EDITOR
                UnityEditor.Handles.color = solidColor;
                UnityEditor.Handles.DrawWireDisc(cylinderBottom, Vector3.up, cylinderRadius);
                UnityEditor.Handles.DrawWireDisc(cylinderTop, Vector3.up, cylinderRadius);
                #endif
                
                // Draw spawn portal effect inside cylinder scaled to hex size
                DrawSpawnPortal(center + Vector3.up * (size * 0.2f), size * 0.2f, solidColor);
                break;
        }
    }
    
    private void DrawThickLine(Vector3 from, Vector3 to, float thickness, Color color = default)
    {
        if (color != default(Color))
            Gizmos.color = color;
            
        Vector3 direction = (to - from).normalized;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up) * thickness * 0.5f;
        
        // Draw multiple lines to simulate thickness
        Gizmos.DrawLine(from + perpendicular, to + perpendicular);
        Gizmos.DrawLine(from - perpendicular, to - perpendicular);
        Gizmos.DrawLine(from, to); // Center line
    }
    
    private void DrawArrow(Vector3 position, Vector3 direction, float size, Color color = default)
    {
        if (color != default(Color))
            Gizmos.color = color;
            
        Vector3 right = Vector3.Cross(direction, Vector3.up) * size * 0.3f;
        Vector3 arrowHead = position + direction * size;
        Vector3 arrowLeft = position + direction * size * 0.7f - right;
        Vector3 arrowRight = position + direction * size * 0.7f + right;
        
        Gizmos.DrawLine(position, arrowHead);
        Gizmos.DrawLine(arrowHead, arrowLeft);
        Gizmos.DrawLine(arrowHead, arrowRight);
    }
    
    private void DrawShieldSymbol(Vector3 center, float size, Color color = default)
    {
        if (color != default(Color))
            Gizmos.color = color;
            
        // Draw a simple shield shape scaled properly
        Vector3 top = center + Vector3.forward * size + Vector3.up * (size * 0.05f);
        Vector3 bottom = center - Vector3.forward * size + Vector3.up * (size * 0.05f);
        Vector3 left = center - Vector3.right * size * 0.7f + Vector3.up * (size * 0.05f);
        Vector3 right = center + Vector3.right * size * 0.7f + Vector3.up * (size * 0.05f);
        
        Gizmos.DrawLine(top, left);
        Gizmos.DrawLine(left, bottom);
        Gizmos.DrawLine(bottom, right);
        Gizmos.DrawLine(right, top);
        Gizmos.DrawLine(left, right); // Cross
        Gizmos.DrawLine(top, bottom); // Cross
    }
    
    private void DrawSpawnPortal(Vector3 center, float size, Color color = default)
    {
        if (color != default(Color))
            Gizmos.color = color;
            
        // Draw rotating portal rings scaled properly
        float time = Time.time * 2f;
        int rings = 3;
        
        for (int ring = 0; ring < rings; ring++)
        {
            float ringSize = size * (0.5f + ring * 0.3f);
            float rotation = time + ring * 120f;
            
            for (int i = 0; i < 8; i++)
            {
                float angle1 = (rotation + i * 45f) * Mathf.Deg2Rad;
                float angle2 = (rotation + (i + 1) * 45f) * Mathf.Deg2Rad;
                
                Vector3 point1 = center + new Vector3(
                    Mathf.Cos(angle1) * ringSize,
                    size * 0.1f,
                    Mathf.Sin(angle1) * ringSize
                );
                Vector3 point2 = center + new Vector3(
                    Mathf.Cos(angle2) * ringSize,
                    size * 0.1f,
                    Mathf.Sin(angle2) * ringSize
                );
                
                Gizmos.DrawLine(point1, point2);
            }
        }
    }
    
    #endregion
}
