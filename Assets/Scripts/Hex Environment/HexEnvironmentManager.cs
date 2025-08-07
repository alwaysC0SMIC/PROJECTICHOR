using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

public enum LanePreset
{
    [Tooltip("Custom settings - use manual configuration")]
    Custom,
    [Tooltip("Beginner-friendly: Straight, predictable lanes")]
    Beginner,
    [Tooltip("Balanced gameplay with moderate complexity")]
    Balanced,
    [Tooltip("Challenging: High curviness and randomness")]
    Chaotic,
    [Tooltip("Strategic: Spread lanes with potential merging")]
    Strategic,
    [Tooltip("Defensive: Short, focused lanes for tower placement")]
    Defensive,
    [Tooltip("Speed: Long, direct lanes for fast gameplay")]
    Speedway,
    [Tooltip("Maze-like: High curviness with frequent merging")]
    Labyrinth,
    [Tooltip("Minimal: Few, simple lanes")]
    Minimalist,
    [Tooltip("Maximum complexity with all features enabled")]
    Maximum
}

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
    public float height = 0f; // Height displacement for terrain
    
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
    [TitleGroup("TD/Center Hub Settings")]
    [SerializeField, Range(1, 2), Tooltip("Size of center hub (1 = single hex, 2 = center + 6 surrounding = 7 total)")]
    private int centerHubSize = 2;
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Overall Lane Settings")]
    [SerializeField, Tooltip("Auto-randomize lane settings when seed changes")]
    private bool autoRandomizeLanes = true;
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Overall Lane Settings")]
    [SerializeField, Tooltip("Apply preset configurations for different gameplay styles")]
    private LanePreset lanePreset = LanePreset.Custom;
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Overall Lane Settings")]
    [Button(ButtonSizes.Medium, Name = "🎯 Apply Preset")]
    [GUIColor(0.6f, 0.8f, 1f)]
    private void ApplyPresetButtonPressed()
    {
        ApplyLanePreset();
    }
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Overall Lane Settings")]
    [SerializeField, Range(0f, 1f), Tooltip("Overall curviness multiplier for all lanes")]
    private float globalCurviness = 1f;
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Overall Lane Settings")]
    [SerializeField, Range(0f, 1f), Tooltip("Overall randomness multiplier for all lanes")]
    private float globalRandomness = 1f;
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Overall Lane Settings")]
    [SerializeField, Range(0f, 1f), Tooltip("Probability that any lane will allow merging")]
    private float globalMergeProbability = 0.4f;
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Overall Lane Settings")]
    [SerializeField, Range(3, 12), Tooltip("Base length for randomized lanes")]
    private int baseLaneLength = 6;
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Overall Lane Settings")]
    [SerializeField, Range(1, 4), Tooltip("Maximum variation from base lane length")]
    private int lengthVariation = 2;
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Overall Lane Settings")]
    [SerializeField, Range(0f, 1f), Tooltip("How much to spread out lane starting directions (0 = random, 1 = maximum spread)")]
    private float directionSpreadWeight = 0.8f;
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Overall Lane Settings")]
    [SerializeField, Range(0f, 60f), Tooltip("Minimum angle between adjacent lane directions in degrees")]
    private float minAngleBetweenLanes = 30f;
    
    // Force to edge is now always enabled - no longer configurable
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Individual Lane Settings")]
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
    [TitleGroup("TD/Validation Settings")]
    [SerializeField, Range(3, 20), Tooltip("Maximum attempts to regenerate if validation fails")]
    private int maxValidationAttempts = 10;
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [TitleGroup("TD/Validation Settings")]
    [SerializeField, Tooltip("Automatically validate environment during generation to prevent clumping and ensure lane completion")]
    private bool enableValidation = true;
    
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
    [SerializeField, Tooltip("Color for pathway connections")]
    private Color pathwayConnectionColor = new Color(1f, 0.8f, 0.2f, 1f);
    
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
    
    [TabGroup("Terrain", "🏔️ Terrain Generation")]
    [TitleGroup("Terrain/Environment Expansion")]
    [SerializeField, Range(1, 10), Tooltip("Number of additional environment rings around the main grid")]
    private int additionalEnvironmentRings = 2;
    
    [TabGroup("Terrain", "🏔️ Terrain Generation")]
    [TitleGroup("Terrain/Height Generation")]
    [SerializeField, Tooltip("Enable terrain height generation using Perlin noise")]
    private bool enableTerrainGeneration = true;
    
    [TabGroup("Terrain", "🏔️ Terrain Generation")]
    [TitleGroup("Terrain/Height Generation")]
    [SerializeField, Range(0.01f, 0.5f), Tooltip("Scale factor for Perlin noise sampling")]
    private float noiseScale = 0.1f;
    
    [TabGroup("Terrain", "🏔️ Terrain Generation")]
    [TitleGroup("Terrain/Height Generation")]
    [SerializeField, Range(0f, 0.5f), Tooltip("Maximum height variation in world units (0.5 max)")]
    private float maxHeightVariation = 0.5f;
    
    [TabGroup("Terrain", "🏔️ Terrain Generation")]
    [TitleGroup("Terrain/Height Generation")]
    [SerializeField, Range(0f, 2f), Tooltip("Height contour step (0.5 recommended for smooth connections)")]
    private float heightContour = 0.5f;
    
    [TabGroup("Terrain", "🏔️ Terrain Generation")]
    [TitleGroup("Terrain/Height Smoothing")]
    [SerializeField, Range(1, 5), Tooltip("Number of smoothing passes to ensure connected heights")]
    private int heightSmoothingPasses = 3;
    
    [TabGroup("Terrain", "🏔️ Terrain Generation")]
    [TitleGroup("Terrain/Height Smoothing")]
    [SerializeField, Range(0.5f, 2f), Tooltip("Maximum height difference between adjacent hexes")]
    private float maxHeightDifference = 0.5f;
    
    [TabGroup("Terrain", "🏔️ Terrain Generation")]
    [TitleGroup("Terrain/Generation Actions")]
    [Button(ButtonSizes.Medium, Name = "🌄 Generate Terrain")]
    [GUIColor(0.6f, 0.9f, 0.6f)]
    private void GenerateTerrainButtonPressed()
    {
        if (enableTerrainGeneration)
        {
            GenerateTerrainHeights();
            // Clear existing objects and recreate with new heights
            if (hexPrefab != null && !gizmosOnlyMode)
            {
                ClearExistingHexGameObjects();
                InstantiateHexGameObjects();
            }
        }
    }
    
    [TabGroup("Terrain", "🏔️ Terrain Generation")]
    [TitleGroup("Terrain/Generation Actions")]
    [Button(ButtonSizes.Medium, Name = "🗻 Add Environment Rings")]
    [GUIColor(0.6f, 0.6f, 0.9f)]
    private void AddEnvironmentRingsButtonPressed()
    {
        AddAdditionalEnvironmentRings();
        if (enableTerrainGeneration)
        {
            GenerateTerrainHeights();
        }
        // Clear existing objects and recreate with new heights and additional rings
        if (hexPrefab != null && !gizmosOnlyMode)
        {
            ClearExistingHexGameObjects();
            InstantiateHexGameObjects();
        }
    }
    
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
            // Use spread-weighted direction assignment for initial setup too
            float direction;
            if (directionSpreadWeight > 0.5f && numberOfLanes > 1)
            {
                // Use evenly spaced directions for better initial spread
                direction = (360f / numberOfLanes) * laneConfigurations.Count;
            }
            else
            {
                // Use random directions
                direction = Random.Range(0f, 360f);
            }
            
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

        // Initialize random seed and auto-randomize lanes if needed
        if (generationSeed == 0)
        {
            generationSeed = Random.Range(1, int.MaxValue);

            // Auto-randomize lanes when generating with a new random seed
            if (autoRandomizeLanes)
            {
                Random.InitState(generationSeed); // Initialize with the new seed first
                RandomizeAllLaneConfigurations();
            }
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

        // Step 2: Generate lanes from center outwards with validation
        GenerateLanesWithValidation();

        // Step 3: Generate defender spots adjacent to pathways
        if (autoGenerateDefenderSpots)
            GenerateDefenderSpots();

        // Step 4: Generate edge spawn points (one per lane)
        if (generateEdgeSpawns)
            GenerateEdgeSpawnPoints();

        // Step 5: Add additional environment rings around the main grid
        if (additionalEnvironmentRings > 0)
        {
            AddAdditionalEnvironmentRings();
        }

        // Step 6: Generate terrain heights with Perlin noise for existing environment tiles
        if (enableTerrainGeneration)
        {
            GenerateTerrainHeights();
        }

        // Step 7: Instantiate GameObjects (only if not in gizmos-only mode and prefab exists)
        if (canInstantiateObjects)
        {
            // Clear any existing objects first to prevent duplicates
            ClearExistingHexGameObjects();
            InstantiateHexGameObjects();
        }

        string modeText = canInstantiateObjects ? "with GameObjects" : "gizmos-only";
        Debug.Log($"[HexEnvironmentManager] Generated tower defense environment {modeText} with {hexGrid.Count} hexagons, {generatedLanes.Count} lanes (Seed: {generationSeed})");

        // Automatically trigger pathway transforms event after generation
        if (canInstantiateObjects)
        {
            RequestPathwayTransforms();
        }
        
        EventBus<EnvironmentGeneratedEvent>.Raise(new EnvironmentGeneratedEvent
        {

        });
    }
    
    [TabGroup("TD", "🏰 Tower Defense")]
    public void RandomizeSeed()
    {
        generationSeed = Random.Range(1, int.MaxValue);
        
        // Auto-randomize lanes if enabled
        if (autoRandomizeLanes)
        {
            RandomizeAllLaneConfigurations();
        }
        
        Debug.Log($"[HexEnvironmentManager] New seed: {generationSeed}" + (autoRandomizeLanes ? " (lanes auto-randomized)" : ""));
    }
    
    public void RandomizeAllLaneConfigurations()
    {
        InitializeLaneConfigurations(); // Ensure we have the right number of lanes
        
        // Generate well-spread directions first
        List<float> spreadDirections = GenerateSpreadDirections();
        
        for (int i = 0; i < laneConfigurations.Count; i++)
        {
            var config = laneConfigurations[i];
            
            // Ensure all lanes are always active (user controls lane count)
            config.isActive = true; // All lanes are always active
            
            // Use spread directions with some randomness based on weight
            if (i < spreadDirections.Count)
            {
                float spreadDirection = spreadDirections[i];
                float randomDirection = Random.Range(0f, 360f);
                config.direction = Mathf.Lerp(randomDirection, spreadDirection, directionSpreadWeight);
            }
            else
            {
                config.direction = Random.Range(0f, 360f);
            }
            
            config.length = Mathf.Clamp(baseLaneLength + Random.Range(-lengthVariation, lengthVariation + 1), 2, 15);
            config.laneColor = Color.HSVToRGB(Random.Range(0f, 1f), Random.Range(0.6f, 1f), Random.Range(0.7f, 1f));
            
            // Randomize path behavior using global multipliers
            config.curviness = Random.Range(0f, 0.8f) * globalCurviness;
            config.randomnessFactor = Random.Range(0f, 0.6f) * globalRandomness;
            
            // Randomize lane merging settings using global probability
            config.allowMerging = Random.value < globalMergeProbability;
            if (config.allowMerging)
            {
                // Random merge target (-1 for auto-find, or specific lane)
                config.mergeWithLane = Random.value > 0.5f ? -1 : Random.Range(0, numberOfLanes);
                config.mergeAtDistance = Random.Range(2, 6);
                config.mergeProbability = Random.Range(0.3f, 0.9f);
            }
            else
            {
                config.mergeWithLane = -1;
                config.mergeAtDistance = 3;
                config.mergeProbability = 0.7f;
            }
        }
        
        Debug.Log($"[HexEnvironmentManager] Randomized all {laneConfigurations.Count} lane configurations (lane count remains user-controlled)!");
    }
    
    public void ApplyLanePreset()
    {
        if (lanePreset == LanePreset.Custom) return;
        
        // Apply preset values to global settings
        switch (lanePreset)
        {
            case LanePreset.Beginner:
                numberOfLanes = 1;
                globalCurviness = 0.1f;
                globalRandomness = 0.0f;
                globalMergeProbability = 0.0f;
                baseLaneLength = 5;
                lengthVariation = 1;
                directionSpreadWeight = 1.0f;
                minAngleBetweenLanes = 60f;
                break;
                
            case LanePreset.Balanced:
                numberOfLanes = 2;
                globalCurviness = 0.4f;
                globalRandomness = 0.3f;
                globalMergeProbability = 0.2f;
                baseLaneLength = 6;
                lengthVariation = 2;
                directionSpreadWeight = 0.8f;
                minAngleBetweenLanes = 30f;
                break;
                
            case LanePreset.Chaotic:
                numberOfLanes = 3;
                globalCurviness = 0.9f;
                globalRandomness = 0.8f;
                globalMergeProbability = 0.6f;
                baseLaneLength = 7;
                lengthVariation = 3;
                directionSpreadWeight = 0.3f;
                minAngleBetweenLanes = 15f;
                break;
                
            case LanePreset.Strategic:
                numberOfLanes = 4;
                globalCurviness = 0.5f;
                globalRandomness = 0.4f;
                globalMergeProbability = 0.7f;
                baseLaneLength = 8;
                lengthVariation = 2;
                directionSpreadWeight = 0.9f;
                minAngleBetweenLanes = 25f;
                break;
                
            case LanePreset.Defensive:
                numberOfLanes = 3;
                globalCurviness = 0.2f;
                globalRandomness = 0.1f;
                globalMergeProbability = 0.1f;
                baseLaneLength = 4;
                lengthVariation = 1;
                directionSpreadWeight = 1.0f;
                minAngleBetweenLanes = 45f;
                break;
                
            case LanePreset.Speedway:
                numberOfLanes = 2;
                globalCurviness = 0.0f;
                globalRandomness = 0.0f;
                globalMergeProbability = 0.0f;
                baseLaneLength = 10;
                lengthVariation = 1;
                directionSpreadWeight = 1.0f;
                minAngleBetweenLanes = 90f;
                break;
                
            case LanePreset.Labyrinth:
                numberOfLanes = 4;
                globalCurviness = 1.0f;
                globalRandomness = 0.7f;
                globalMergeProbability = 0.8f;
                baseLaneLength = 9;
                lengthVariation = 4;
                directionSpreadWeight = 0.5f;
                minAngleBetweenLanes = 10f;
                break;
                
            case LanePreset.Minimalist:
                numberOfLanes = 1;
                globalCurviness = 0.2f;
                globalRandomness = 0.1f;
                globalMergeProbability = 0.0f;
                baseLaneLength = 6;
                lengthVariation = 1;
                directionSpreadWeight = 1.0f;
                minAngleBetweenLanes = 60f;
                break;
                
            case LanePreset.Maximum:
                numberOfLanes = 4;
                globalCurviness = 0.8f;
                globalRandomness = 0.6f;
                globalMergeProbability = 0.5f;
                baseLaneLength = 8;
                lengthVariation = 3;
                directionSpreadWeight = 0.7f;
                minAngleBetweenLanes = 20f;
                break;
        }
        
        // Reinitialize lane configurations with new settings
        InitializeLaneConfigurations();
        
        // If auto-randomize is enabled, apply randomization with new global settings
        if (autoRandomizeLanes)
        {
            RandomizeAllLaneConfigurations();
        }
        
        Debug.Log($"[HexEnvironmentManager] Applied {lanePreset} preset configuration!");
    }
    
    private List<float> GenerateSpreadDirections()
    {
        List<float> directions = new List<float>();
        
        if (numberOfLanes <= 1)
        {
            directions.Add(Random.Range(0f, 360f));
            return directions;
        }
        
        // Start with evenly spaced directions as the ideal spread
        float baseSpacing = 360f / numberOfLanes;
        float startOffset = Random.Range(0f, baseSpacing); // Random rotation of the whole pattern
        
        for (int i = 0; i < numberOfLanes; i++)
        {
            float idealDirection = (startOffset + i * baseSpacing) % 360f;
            directions.Add(idealDirection);
        }
        
        // Apply some controlled randomness while maintaining minimum spacing
        for (int attempts = 0; attempts < 10; attempts++) // Try to improve spacing
        {
            bool improved = false;
            
            for (int i = 0; i < directions.Count; i++)
            {
                // Check if this direction is too close to others
                float minDistanceToOthers = float.MaxValue;
                
                for (int j = 0; j < directions.Count; j++)
                {
                    if (i == j) continue;
                    
                    float distance = Mathf.Min(
                        Mathf.Abs(directions[i] - directions[j]),
                        360f - Mathf.Abs(directions[i] - directions[j])
                    );
                    
                    minDistanceToOthers = Mathf.Min(minDistanceToOthers, distance);
                }
                
                // If too close, try to move it to a better position
                if (minDistanceToOthers < minAngleBetweenLanes)
                {
                    float newDirection = FindBestDirectionSpot(directions, i);
                    if (newDirection != directions[i])
                    {
                        directions[i] = newDirection;
                        improved = true;
                    }
                }
            }
            
            if (!improved) break; // No more improvements possible
        }
        
        return directions;
    }
    
    private float FindBestDirectionSpot(List<float> existingDirections, int excludeIndex)
    {
        float bestDirection = existingDirections[excludeIndex];
        float bestMinDistance = 0f;
        
        // Try different angles and find the one with maximum minimum distance to others
        for (float testAngle = 0f; testAngle < 360f; testAngle += 5f) // Test every 5 degrees
        {
            float minDistanceToOthers = float.MaxValue;
            
            for (int i = 0; i < existingDirections.Count; i++)
            {
                if (i == excludeIndex) continue;
                
                float distance = Mathf.Min(
                    Mathf.Abs(testAngle - existingDirections[i]),
                    360f - Mathf.Abs(testAngle - existingDirections[i])
                );
                
                minDistanceToOthers = Mathf.Min(minDistanceToOthers, distance);
            }
            
            if (minDistanceToOthers > bestMinDistance)
            {
                bestMinDistance = minDistanceToOthers;
                bestDirection = testAngle;
            }
        }
        
        return bestDirection;
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
    
    /// <summary>
    /// Clears only the instantiated GameObjects while preserving hex data for terrain regeneration
    /// </summary>
    private void ClearExistingHexGameObjects()
    {
        // Clear GameObject references from hex data
        foreach (var kvp in hexGrid)
        {
            kvp.Value.gameObject = null;
        }
        
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
    
    public HexTile GetHexTileAt(HexCoordinates coord)
    {
        var hexGameObject = GetHexGameObjectAt(coord);
        return hexGameObject?.GetComponent<HexTile>();
    }
    
    public List<HexData> GetHexesOfType(HexType type)
    {
        return hexGrid.Values.Where(hex => hex.type == type).ToList();
    }
    
    public List<HexData> GetLaneHexes(int laneId)
    {
        return hexGrid.Values.Where(hex => hex.laneId == laneId).ToList();
    }
    
    /// <summary>
    /// Triggers an event containing pathway transforms organized by lane
    /// </summary>
    public void RequestPathwayTransforms()
    {
        var pathwayTransformsByLane = GetPathwayTransformsByLaneInternal();
        
        // Raise event with the pathway data
        EventBus<PathwayTransformsEvent>.Raise(new PathwayTransformsEvent 
        { 
            pathwayTransformsByLane = pathwayTransformsByLane 
        });
        
        Debug.Log($"[HexEnvironmentManager] Raised PathwayTransformsEvent with {pathwayTransformsByLane?.Count ?? 0} lanes");
    }
    
    /// <summary>
    /// Internal method to get pathway transforms organized by lane
    /// </summary>
    private List<List<Transform>> GetPathwayTransformsByLaneInternal()
    {
        var pathwayTransformsByLane = new List<List<Transform>>();
        
        // Initialize lists for each lane
        int maxLaneId = -1;
        foreach (var hex in hexGrid.Values)
        {
            if (hex.type == HexType.Pathway && hex.laneId > maxLaneId)
            {
                maxLaneId = hex.laneId;
            }
        }
        
        // Create empty lists for each lane
        for (int i = 0; i <= maxLaneId; i++)
        {
            pathwayTransformsByLane.Add(new List<Transform>());
        }
        
        // Group pathway hexes by lane ID and collect their transforms
        foreach (var hex in hexGrid.Values)
        {
            if (hex.type == HexType.Pathway && hex.laneId >= 0 && hex.gameObject != null)
            {
                pathwayTransformsByLane[hex.laneId].Add(hex.gameObject.transform);
            }
        }
        
        // Sort each lane's transforms by their order in the generated lanes and add edge spawners
        for (int laneId = 0; laneId < pathwayTransformsByLane.Count && laneId < generatedLanes.Count; laneId++)
        {
            var laneCoords = generatedLanes[laneId];
            var laneTransforms = pathwayTransformsByLane[laneId];
            
            // Sort transforms based on their order in the lane coordinates
            var sortedTransforms = laneTransforms
                .OrderBy(transform => {
                    // Find the hex coordinate for this transform
                    var hexData = hexGrid.Values.FirstOrDefault(h => h.gameObject != null && h.gameObject.transform == transform);
                    if (hexData != null)
                    {
                        return laneCoords.IndexOf(hexData.coordinates);
                    }
                    return int.MaxValue; // Put unmatched transforms at the end
                })
                .ToList();
            
            // Find and add the edge spawner for this lane as the first transform
            Transform edgeSpawnerTransform = null;
            if (laneId < laneSpawnPoints.Count)
            {
                var spawnPoint = laneSpawnPoints[laneId];
                var spawnHexData = hexGrid.Values.FirstOrDefault(h => 
                    h.coordinates.Equals(spawnPoint) && 
                    h.type == HexType.EdgeSpawn && 
                    h.gameObject != null);
                
                if (spawnHexData != null)
                {
                    edgeSpawnerTransform = spawnHexData.gameObject.transform;
                }
            }
            
            // Find the center hub transform for this lane's final destination
            Transform centerHubTransform = null;
            if (laneCoords.Count > 0)
            {
                // Get the last coordinate in the lane (should be center hub)
                HexCoordinates finalCoord = laneCoords[laneCoords.Count - 1];
                var centerHubData = hexGrid.Values.FirstOrDefault(h => 
                    h.coordinates.Equals(finalCoord) && 
                    h.type == HexType.CenterHub && 
                    h.gameObject != null);
                
                if (centerHubData != null)
                {
                    centerHubTransform = centerHubData.gameObject.transform;
                }
                else
                {
                    // Fallback: find any center hub transform near the end of the path
                    var centerHubCoords = GetCenterHubCoordinates();
                    foreach (var centerCoord in centerHubCoords)
                    {
                        var fallbackCenterData = hexGrid.Values.FirstOrDefault(h => 
                            h.coordinates.Equals(centerCoord) && 
                            h.type == HexType.CenterHub && 
                            h.gameObject != null);
                        
                        if (fallbackCenterData != null)
                        {
                            centerHubTransform = fallbackCenterData.gameObject.transform;
                            break;
                        }
                    }
                }
            }
            
            // Create the final list with edge spawner first, pathway transforms, then center hub
            var finalTransformList = new List<Transform>();
            
            // Add edge spawner at the beginning
            if (edgeSpawnerTransform != null)
            {
                finalTransformList.Add(edgeSpawnerTransform);
            }
            
            // Add pathway transforms in order
            finalTransformList.AddRange(sortedTransforms);
            
            // Add center hub at the end (final destination)
            if (centerHubTransform != null)
            {
                finalTransformList.Add(centerHubTransform);
            }
            
            pathwayTransformsByLane[laneId] = finalTransformList;
        }
        
        return pathwayTransformsByLane;
    }
    
    public HexCoordinates WorldToHex(Vector3 worldPosition)
    {
        // Since center point is always at world origin (0,0,0), use world position directly
        float size = hexSize * (1f + hexSpacing);
        
        // Convert world position to axial coordinates (flat-top orientation)
        float q = (2f/3f * worldPosition.x) / size;
        float r = (-1f/3f * worldPosition.x + SQRT_3/3f * worldPosition.z) / size;
        
        return CubeToHex(CubeRound(q, -q-r, r));
    }
    
    #endregion
    
    #region Tower Defense Generation
    
    private void GenerateAllHexCoordinates()
    {
        // Get center hub coordinates (center + 6 surrounding hexes = 7 total)
        List<HexCoordinates> centerHubCoords = GetCenterHubCoordinates();
        
        // Generate all hex coordinates in rings from center outwards
        for (int ring = 0; ring <= gridRadius; ring++)
        {
            List<HexCoordinates> ringCoords = GetHexRing(HexCoordinates.Zero, ring);
            
            foreach (var coord in ringCoords)
            {
                // Check if this coordinate is part of the center hub
                HexType type = centerHubCoords.Contains(coord) ? HexType.CenterHub : HexType.Environment;
                HexData hexData = new HexData(coord, type);
                
                hexGrid[coord] = hexData;
                generatedCoordinates.Add(coord);
            }
        }
    }
    
    private List<HexCoordinates> GetCenterHubCoordinates()
    {
        List<HexCoordinates> centerHubCoords = new List<HexCoordinates>();
        
        // Always add the center hex
        centerHubCoords.Add(HexCoordinates.Zero);
        
        // Add surrounding hexes based on size setting
        if (centerHubSize >= 2)
        {
            // Add the 6 surrounding hexes (ring 1 around center)
            foreach (var direction in HEX_DIRECTIONS)
            {
                centerHubCoords.Add(new HexCoordinates(direction.q, direction.r));
            }
        }
        
        return centerHubCoords;
    }
    
    private bool IsPartOfCenterHub(HexCoordinates coord)
    {
        // Center hex is always part of the hub
        if (coord.Equals(HexCoordinates.Zero))
            return true;
            
        // Check surrounding hexes based on size setting
        if (centerHubSize >= 2)
        {
            // Check if it's one of the 6 directions from center
            foreach (var direction in HEX_DIRECTIONS)
            {
                if (coord.q == direction.q && coord.r == direction.r)
                    return true;
            }
        }
        
        return false;
    }
    
    private float GetDistanceToNearestCenterHub(HexCoordinates hex)
    {
        List<HexCoordinates> centerHubCoords = GetCenterHubCoordinates();
        float minDistance = float.MaxValue;
        
        foreach (var centerHex in centerHubCoords)
        {
            float distance = HexDistance(hex, centerHex);
            if (distance < minDistance)
            {
                minDistance = distance;
            }
        }
        
        return minDistance;
    }
    
    private HexCoordinates GetNearestCenterHubHex(HexCoordinates hex)
    {
        List<HexCoordinates> centerHubCoords = GetCenterHubCoordinates();
        HexCoordinates nearest = HexCoordinates.Zero;
        float minDistance = float.MaxValue;
        
        foreach (var centerHex in centerHubCoords)
        {
            float distance = HexDistance(hex, centerHex);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = centerHex;
            }
        }
        
        return nearest;
    }
    
    private void GenerateLanes()
    {
        generatedLanes.Clear();
        
        // Step 1: Find well-spaced starting positions on the edge
        List<HexCoordinates> startingPositions = FindSpacedEdgeStartingPositions();
        
        if (startingPositions.Count == 0)
        {
            Debug.LogError("[HexEnvironmentManager] Could not find any valid starting positions!");
            return;
        }
        
        // Initialize lanes with their starting positions
        for (int i = 0; i < startingPositions.Count; i++)
        {
            List<HexCoordinates> lane = new List<HexCoordinates>();
            lane.Add(startingPositions[i]);
            generatedLanes.Add(lane);
        }
        
        // Step 2: Generate lanes in parallel waves
        GenerateLanesInWaves();
        
        // Step 3: Apply lane data to hex grid
        ApplyLanesToHexGrid();
    }
    
    private List<HexCoordinates> FindSpacedEdgeStartingPositions()
    {
        List<HexCoordinates> startingPositions = new List<HexCoordinates>();
        List<HexCoordinates> edgeHexes = GetHexRing(HexCoordinates.Zero, gridRadius);
        
        // Calculate minimum distance between starting positions
        float minDistanceBetweenStarts = Mathf.Max(3f, gridRadius * 0.4f); // At least 3 hexes apart
        
        // Try to place starting positions with good spacing
        for (int laneIndex = 0; laneIndex < numberOfLanes; laneIndex++)
        {
            if (!laneConfigurations[laneIndex].isActive) continue;
            
            var config = laneConfigurations[laneIndex];
            
            // Find best edge hex for this lane's direction
            HexCoordinates bestStart = FindBestEdgeHexForDirection(edgeHexes, config.direction, startingPositions, minDistanceBetweenStarts);
            
            if (bestStart != HexCoordinates.Zero)
            {
                startingPositions.Add(bestStart);
            }
        }
        
        return startingPositions;
    }
    
    private HexCoordinates FindBestEdgeHexForDirection(List<HexCoordinates> edgeHexes, float direction, List<HexCoordinates> existingStarts, float minDistance)
    {
        // Convert direction to radians
        float radians = direction * Mathf.Deg2Rad;
        Vector3 dirVector = new Vector3(Mathf.Cos(radians), 0, Mathf.Sin(radians));
        
        HexCoordinates bestHex = HexCoordinates.Zero;
        float bestScore = -1f;
        
        foreach (var hex in edgeHexes)
        {
            // Check distance to existing starts
            bool tooClose = false;
            foreach (var existingStart in existingStarts)
            {
                if (HexDistance(hex, existingStart) < minDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            
            if (tooClose) continue;
            
            // Calculate alignment with desired direction
            Vector3 hexWorldPos = HexToWorldPosition(hex);
            Vector3 centerWorldPos = HexToWorldPosition(HexCoordinates.Zero);
            Vector3 hexDirection = (hexWorldPos - centerWorldPos).normalized;
            float alignment = Vector3.Dot(hexDirection, dirVector);
            
            if (alignment > bestScore)
            {
                bestScore = alignment;
                bestHex = hex;
            }
        }
        
        return bestHex;
    }
    
    private void GenerateLanesInWaves()
    {
        HashSet<HexCoordinates> occupiedHexes = new HashSet<HexCoordinates>();
        
        // Add all starting positions to occupied set
        foreach (var lane in generatedLanes)
        {
            if (lane.Count > 0)
            {
                occupiedHexes.Add(lane[0]);
            }
        }
        
        int maxWaves = gridRadius * 2; // Safety limit
        
        for (int wave = 0; wave < maxWaves; wave++)
        {
            bool anyLaneProgressed = false;
            List<HexCoordinates> newOccupiedThisWave = new List<HexCoordinates>();
            
            // Process each lane in this wave
            for (int laneIndex = 0; laneIndex < generatedLanes.Count; laneIndex++)
            {
                var lane = generatedLanes[laneIndex];
                
                // Skip if lane already reached center hub
                if (lane.Count > 0 && IsPartOfCenterHub(lane[lane.Count - 1]))
                    continue;
                
                // Get current position (last hex in lane)
                HexCoordinates currentPos = lane[lane.Count - 1];
                
                // Find next hex for this lane
                HexCoordinates nextHex = FindNextHexForLane(currentPos, laneIndex, occupiedHexes, newOccupiedThisWave);
                
                if (nextHex != currentPos && !IsPartOfCenterHub(nextHex))
                {
                    // Check if we can place this hex (maintain spacing from other lanes)
                    if (CanPlaceHexWithSpacing(nextHex, newOccupiedThisWave, laneIndex))
                    {
                        lane.Add(nextHex);
                        newOccupiedThisWave.Add(nextHex);
                        anyLaneProgressed = true;
                    }
                }
                else if (IsPartOfCenterHub(nextHex))
                {
                    // Reached center hub
                    lane.Add(nextHex);
                    newOccupiedThisWave.Add(nextHex);
                    anyLaneProgressed = true;
                }
            }
            
            // Add this wave's occupied hexes to the global set
            foreach (var hex in newOccupiedThisWave)
            {
                occupiedHexes.Add(hex);
            }
            
            // Check if all lanes have reached center hub
            bool allLanesComplete = true;
            foreach (var lane in generatedLanes)
            {
                if (lane.Count == 0 || !IsPartOfCenterHub(lane[lane.Count - 1]))
                {
                    allLanesComplete = false;
                    break;
                }
            }
            
            if (allLanesComplete || !anyLaneProgressed)
                break;
        }
        
        // Ensure all lanes reach center
        EnsureAllLanesReachCenter();
    }
    
    private void GenerateLanesWithValidation()
    {
        if (!enableValidation)
        {
            // Validation disabled, just generate normally
            GenerateLanes();
            return;
        }
        
        // First attempt - standard generation
        GenerateLanes();
        ApplyLanesToHexGrid();
        
        // Try targeted repairs before full regeneration
        if (!ValidateAndRepairEnvironment())
        {
            // If repair fails, fall back to regeneration attempts
            for (int attempt = 2; attempt <= maxValidationAttempts; attempt++)
            {
                Debug.Log($"[HexEnvironmentManager] Lane generation attempt {attempt}/{maxValidationAttempts}");
                
                // Clear and regenerate
                ClearLaneData();
                Random.InitState(generationSeed + attempt);
                GenerateLanes();
                ApplyLanesToHexGrid();
                
                if (ValidateAndRepairEnvironment())
                {
                    Debug.Log($"[HexEnvironmentManager] Lane generation successful on attempt {attempt}");
                    return;
                }
            }
            
            Debug.LogError($"[HexEnvironmentManager] Failed to generate valid environment after {maxValidationAttempts} attempts! Using last attempt.");
        }
        else
        {
            Debug.Log("[HexEnvironmentManager] Lane generation successful with targeted repairs");
        }
    }
    
    private bool ValidateAndRepairEnvironment()
    {
        bool wasValid = true;
        
        // Repair incomplete lanes first
        if (!ValidateAllLanesReachCenter())
        {
            Debug.Log("[HexEnvironmentManager] Repairing incomplete lanes...");
            RepairIncompleteLanes();
            ApplyLanesToHexGrid(); // Reapply after repair
            wasValid = false;
        }
        
        // Then check and repair clumping
        if (!ValidateDefenderAccessibility())
        {
            Debug.Log("[HexEnvironmentManager] Repairing pathway clumping...");
            if (RepairPathwayClumping())
            {
                ApplyLanesToHexGrid(); // Reapply after repair
                wasValid = false;
            }
            else
            {
                Debug.LogWarning("[HexEnvironmentManager] Could not repair clumping - regeneration required");
                return false;
            }
        }
        
        // Final validation after repairs
        bool finalValid = ValidateAllLanesReachCenter() && ValidateDefenderAccessibility();
        
        if (!wasValid && finalValid)
        {
            Debug.Log("[HexEnvironmentManager] Environment successfully repaired!");
        }
        
        return finalValid;
    }
    
    private void RepairIncompleteLanes()
    {
        for (int laneIndex = 0; laneIndex < generatedLanes.Count; laneIndex++)
        {
            var lane = generatedLanes[laneIndex];
            
            // Check if lane reaches any center hub hex
            bool reachesCenterHub = false;
            if (lane.Count > 0)
            {
                foreach (var centerHex in GetCenterHubCoordinates())
                {
                    if (lane.Contains(centerHex))
                    {
                        reachesCenterHub = true;
                        break;
                    }
                }
            }
            
            if (lane.Count == 0 || !reachesCenterHub)
            {
                Debug.Log($"[HexEnvironmentManager] Repairing lane {laneIndex} - extending to center hub");
                
                if (lane.Count == 0)
                {
                    // Lane is completely empty - this shouldn't happen, but handle it
                    Debug.LogWarning($"[HexEnvironmentManager] Lane {laneIndex} is empty! Skipping repair.");
                    continue;
                }
                
                // Use A* pathfinding to create optimal path to center
                HexCoordinates lastHex = lane[lane.Count - 1];
                List<HexCoordinates> pathToCenter = FindOptimalPathToCenter(lastHex, laneIndex);
                
                if (pathToCenter.Count > 0)
                {
                    lane.AddRange(pathToCenter);
                    Debug.Log($"[HexEnvironmentManager] Added {pathToCenter.Count} hexes to complete lane {laneIndex}");
                }
            }
        }
    }
    
    private bool RepairPathwayClumping()
    {
        var clumpedHexes = FindClumpedPathwayHexes();
        
        if (clumpedHexes.Count == 0)
            return true; // No clumping found
        
        Debug.Log($"[HexEnvironmentManager] Found {clumpedHexes.Count} clumped pathway hexes");
        
        int repairedCount = 0;
        foreach (var clumpedHex in clumpedHexes)
        {
            if (TryCreateDefenderAccess(clumpedHex))
            {
                repairedCount++;
            }
        }
        
        Debug.Log($"[HexEnvironmentManager] Repaired {repairedCount}/{clumpedHexes.Count} clumped hexes");
        
        // Return true if we repaired most of the clumping (80% threshold)
        return repairedCount >= (clumpedHexes.Count * 0.8f);
    }
    
    private List<HexCoordinates> FindClumpedPathwayHexes()
    {
        var clumpedHexes = new List<HexCoordinates>();
        
        foreach (var kvp in hexGrid)
        {
            if (kvp.Value.type == HexType.Pathway)
            {
                bool hasDefenderAccess = false;
                
                foreach (var direction in HEX_DIRECTIONS)
                {
                    HexCoordinates adjacent = new HexCoordinates(
                        kvp.Key.q + direction.q,
                        kvp.Key.r + direction.r
                    );
                    
                    if (hexGrid.ContainsKey(adjacent) && hexGrid[adjacent].type == HexType.Environment)
                    {
                        hasDefenderAccess = true;
                        break;
                    }
                }
                
                if (!hasDefenderAccess)
                {
                    clumpedHexes.Add(kvp.Key);
                }
            }
        }
        
        return clumpedHexes;
    }
    
    private bool TryCreateDefenderAccess(HexCoordinates pathwayHex)
    {
        // Try to convert an adjacent pathway hex to environment if it doesn't break lane connectivity
        foreach (var direction in HEX_DIRECTIONS)
        {
            HexCoordinates adjacent = new HexCoordinates(
                pathwayHex.q + direction.q,
                pathwayHex.r + direction.r
            );
            
            if (hexGrid.ContainsKey(adjacent) && hexGrid[adjacent].type == HexType.Pathway)
            {
                // Check if removing this pathway hex would break lane connectivity
                if (CanSafelyConvertToEnvironment(adjacent))
                {
                    // Convert to environment to create defender access
                    hexGrid[adjacent].type = HexType.Environment;
                    hexGrid[adjacent].laneId = -1;
                    
                    // Remove from all lanes that contain this hex
                    RemoveHexFromAllLanes(adjacent);
                    
                    Debug.Log($"[HexEnvironmentManager] Converted pathway hex {adjacent} to environment to fix clumping at {pathwayHex}");
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private bool CanSafelyConvertToEnvironment(HexCoordinates hex)
    {
        // Check if removing this hex would disconnect any lanes
        if (IsPartOfCenterHub(hex)) return false; // Never remove center hub hexes
        
        // Find which lanes use this hex
        var affectedLanes = new List<int>();
        for (int i = 0; i < generatedLanes.Count; i++)
        {
            if (generatedLanes[i].Contains(hex))
            {
                affectedLanes.Add(i);
            }
        }
        
        // For each affected lane, check if removing this hex would break connectivity
        foreach (int laneIndex in affectedLanes)
        {
            var lane = generatedLanes[laneIndex];
            int hexIndex = lane.IndexOf(hex);
            
            // If it's the start or end of a lane, it's more critical
            if (hexIndex == 0 || hexIndex == lane.Count - 1)
            {
                return false; // Don't remove start/end hexes
            }
            
            // Check if the hex before and after are still connected through other means
            if (hexIndex > 0 && hexIndex < lane.Count - 1)
            {
                HexCoordinates before = lane[hexIndex - 1];
                HexCoordinates after = lane[hexIndex + 1];
                
                // If adjacent hexes are more than 1 hex apart, this hex is needed for connectivity
                if (HexDistance(before, after) > 1.0f)
                {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    private void RemoveHexFromAllLanes(HexCoordinates hex)
    {
        for (int i = 0; i < generatedLanes.Count; i++)
        {
            generatedLanes[i].RemoveAll(h => h.Equals(hex));
        }
    }
    
    private List<HexCoordinates> FindOptimalPathToCenter(HexCoordinates start, int laneIndex)
    {
        // Simple A* pathfinding to center hub, avoiding existing pathways when possible
        var openSet = new List<HexCoordinates> { start };
        var cameFrom = new Dictionary<HexCoordinates, HexCoordinates>();
        var gScore = new Dictionary<HexCoordinates, float> { [start] = 0 };
        var fScore = new Dictionary<HexCoordinates, float> { [start] = GetDistanceToNearestCenterHub(start) };
        
        while (openSet.Count > 0)
        {
            // Find hex with lowest fScore
            HexCoordinates current = openSet.OrderBy(h => fScore.GetValueOrDefault(h, float.MaxValue)).First();
            
            if (IsPartOfCenterHub(current))
            {
                // Reconstruct path
                var path = new List<HexCoordinates>();
                var pathHex = current;
                
                while (cameFrom.ContainsKey(pathHex))
                {
                    path.Add(pathHex);
                    pathHex = cameFrom[pathHex];
                }
                
                path.Reverse();
                path.RemoveAt(0); // Remove start hex (already in lane)
                return path;
            }
            
            openSet.Remove(current);
            
            foreach (var direction in HEX_DIRECTIONS)
            {
                HexCoordinates neighbor = new HexCoordinates(current.q + direction.q, current.r + direction.r);
                
                if (!hexGrid.ContainsKey(neighbor)) continue;
                
                float tentativeGScore = gScore.GetValueOrDefault(current, float.MaxValue) + 1;
                
                // Add penalty for using existing pathways (but allow it)
                if (hexGrid[neighbor].type == HexType.Pathway && !IsPartOfCenterHub(neighbor))
                {
                    tentativeGScore += 0.5f; // Slight penalty
                }
                
                if (tentativeGScore < gScore.GetValueOrDefault(neighbor, float.MaxValue))
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + GetDistanceToNearestCenterHub(neighbor);
                    
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }
        
        // Fallback: direct line to nearest center hub hex
        HexCoordinates nearestCenterHub = GetNearestCenterHubHex(start);
        return new List<HexCoordinates> { nearestCenterHub };
    }
    
    private void ClearLaneData()
    {
        // Clear lane data for regeneration
        generatedLanes.Clear();
        
        // Reset hex types (except center hub)
        foreach (var kvp in hexGrid)
        {
            if (IsPartOfCenterHub(kvp.Key))
            {
                kvp.Value.type = HexType.CenterHub; // Keep center hub as hub
            }
            else
            {
                kvp.Value.type = HexType.Environment; // Reset everything else
            }
        }
    }
    
    private bool ValidateEnvironment()
    {
        // Check 1: All lanes must reach center
        if (!ValidateAllLanesReachCenter())
        {
            Debug.LogWarning("[HexEnvironmentManager] Validation failed: Not all lanes reach center");
            return false;
        }
        
        // Check 2: No clumping (all pathway cells must be accessible to defenders)
        if (!ValidateDefenderAccessibility())
        {
            Debug.LogWarning("[HexEnvironmentManager] Validation failed: Found pathway cells inaccessible to defenders (clumping detected)");
            return false;
        }
        
        Debug.Log("[HexEnvironmentManager] Environment validation passed");
        return true;
    }
    
    private bool ValidateAllLanesReachCenter()
    {
        List<HexCoordinates> centerHubCoords = GetCenterHubCoordinates();
        
        foreach (var lane in generatedLanes)
        {
            if (lane.Count == 0)
                return false;
                
            // Check if lane contains any center hub hex
            bool reachesCenterHub = false;
            foreach (var centerHex in centerHubCoords)
            {
                if (lane.Contains(centerHex))
                {
                    reachesCenterHub = true;
                    break;
                }
            }
            
            if (!reachesCenterHub)
                return false;
        }
        return true;
    }
    
    private bool ValidateDefenderAccessibility()
    {
        // Get all pathway hexes
        var pathwayHexes = new HashSet<HexCoordinates>();
        foreach (var kvp in hexGrid)
        {
            if (kvp.Value.type == HexType.Pathway)
            {
                pathwayHexes.Add(kvp.Key);
            }
        }
        
        // Check each pathway hex for defender accessibility
        foreach (var pathwayHex in pathwayHexes)
        {
            bool hasDefenderAccess = false;
            
            // Check all adjacent hexes
            foreach (var direction in HEX_DIRECTIONS)
            {
                HexCoordinates adjacent = new HexCoordinates(
                    pathwayHex.q + direction.q, 
                    pathwayHex.r + direction.r
                );
                
                // If adjacent hex is within grid and is environment (potential defender spot)
                if (hexGrid.ContainsKey(adjacent) && hexGrid[adjacent].type == HexType.Environment)
                {
                    hasDefenderAccess = true;
                    break;
                }
            }
            
            if (!hasDefenderAccess)
            {
                Debug.LogWarning($"[HexEnvironmentManager] Pathway hex at {pathwayHex} has no adjacent environment hexes for defender placement (clumping)");
                return false;
            }
        }
        
        return true;
    }
    
    [TabGroup("TD", "🏰 Tower Defense")]
    [Button("🔍 Validate Current Environment")]
    public void ValidateCurrentEnvironment()
    {
        if (hexGrid == null || hexGrid.Count == 0)
        {
            Debug.LogWarning("[HexEnvironmentManager] No environment to validate. Generate environment first.");
            return;
        }
        
        bool isValid = ValidateEnvironment();
        
        if (isValid)
        {
            Debug.Log("[HexEnvironmentManager] ✅ Current environment passed validation!");
        }
        else
        {
            Debug.LogWarning("[HexEnvironmentManager] ❌ Current environment failed validation. Consider regenerating.");
        }
    }
    
    private HexCoordinates FindNextHexForLane(HexCoordinates currentPos, int laneIndex, HashSet<HexCoordinates> globalOccupied, List<HexCoordinates> waveOccupied)
    {
        // Check if any center hub hex is adjacent - if so, go there immediately
        foreach (var direction in HEX_DIRECTIONS)
        {
            HexCoordinates adjacent = new HexCoordinates(currentPos.q + direction.q, currentPos.r + direction.r);
            if (IsPartOfCenterHub(adjacent))
            {
                return adjacent;
            }
        }
        
        // Find best next hex toward center hub
        List<(HexCoordinates hex, float score)> candidates = new List<(HexCoordinates, float)>();
        
        foreach (var direction in HEX_DIRECTIONS)
        {
            HexCoordinates candidate = new HexCoordinates(currentPos.q + direction.q, currentPos.r + direction.r);
            
            // Must be within grid bounds
            if (!hexGrid.ContainsKey(candidate)) continue;
            
            // Calculate score for this candidate
            float score = CalculateWaveHexScore(candidate, currentPos, laneIndex);
            
            if (score > 0)
            {
                candidates.Add((candidate, score));
            }
        }
        
        // Sort by score and try to place the best candidate
        candidates.Sort((a, b) => b.score.CompareTo(a.score));
        
        foreach (var (candidate, score) in candidates)
        {
            // Allow occasional overlap but prefer unoccupied hexes
            bool isOccupied = globalOccupied.Contains(candidate) || waveOccupied.Contains(candidate);
            
            if (!isOccupied)
            {
                return candidate; // Prefer unoccupied hexes
            }
        }
        
        // If all good candidates are occupied, allow overlap for the best one
        if (candidates.Count > 0)
        {
            return candidates[0].hex;
        }
        
        return currentPos; // No valid moves
    }
    
    private float CalculateWaveHexScore(HexCoordinates hex, HexCoordinates from, int laneIndex)
    {
        // Distance to nearest center hub hex (50% weight) - closer is better
        float distanceToCenter = GetDistanceToNearestCenterHub(hex);
        float maxDistance = gridRadius;
        float distanceScore = (1f - (distanceToCenter / maxDistance)) * 0.5f;
        
        // Direction toward nearest center hub hex (30% weight)
        Vector3 hexWorldPos = HexToWorldPosition(hex);
        Vector3 fromWorldPos = HexToWorldPosition(from);
        HexCoordinates nearestCenterHub = GetNearestCenterHubHex(hex);
        Vector3 centerWorldPos = HexToWorldPosition(nearestCenterHub);
        Vector3 moveDirection = (hexWorldPos - fromWorldPos).normalized;
        Vector3 toCenterDirection = (centerWorldPos - fromWorldPos).normalized;
        float directionScore = Vector3.Dot(moveDirection, toCenterDirection) * 0.3f;
        
        // Defender accessibility (20% weight)
        float accessibilityScore = HasAdjacentDefenderSpots(hex) ? 0.2f : 0f;
        
        return distanceScore + directionScore + accessibilityScore;
    }
    
    private bool CanPlaceHexWithSpacing(HexCoordinates hex, List<HexCoordinates> otherNewHexes, int laneIndex)
    {
        // Check spacing against other hexes being placed this wave
        foreach (var otherHex in otherNewHexes)
        {
            float distance = HexDistance(hex, otherHex);
            if (distance < 2f) // At least 1 hex gap between lanes
            {
                return false;
            }
        }
        
        return true;
    }
    
    private void EnsureAllLanesReachCenter()
    {
        for (int laneIndex = 0; laneIndex < generatedLanes.Count; laneIndex++)
        {
            var lane = generatedLanes[laneIndex];
            
            // Check if lane reaches any center hub hex
            bool reachesCenterHub = false;
            if (lane.Count > 0)
            {
                foreach (var centerHex in GetCenterHubCoordinates())
                {
                    if (lane.Contains(centerHex))
                    {
                        reachesCenterHub = true;
                        break;
                    }
                }
            }
            
            // If lane doesn't reach center hub, force a connection
            if (lane.Count == 0 || !reachesCenterHub)
            {
                // Find nearest center hub hex and add it directly if we're close enough
                HexCoordinates lastHex = lane[lane.Count - 1];
                HexCoordinates nearestCenterHub = GetNearestCenterHubHex(lastHex);
                if (GetDistanceToNearestCenterHub(lastHex) <= 2f)
                {
                    lane.Add(nearestCenterHub);
                }
                else
                {
                    // Force a path to center
                    List<HexCoordinates> pathToCenter = ForcePathToCenter(lastHex);
                    lane.AddRange(pathToCenter);
                }
            }
        }
    }
    
    private List<HexCoordinates> ForcePathToCenter(HexCoordinates from)
    {
        List<HexCoordinates> path = new List<HexCoordinates>();
        HexCoordinates current = from;
        
        while (current != HexCoordinates.Zero && path.Count < gridRadius)
        {
            HexCoordinates next = GetClosestHexToCenter(current);
            if (next == current) break; // Can't move closer
            
            path.Add(next);
            current = next;
        }
        
        if (current != HexCoordinates.Zero)
        {
            path.Add(HexCoordinates.Zero);
        }
        
        return path;
    }
    
    private void ApplyLanesToHexGrid()
    {
        // Apply each lane to the hex grid
        for (int laneIndex = 0; laneIndex < generatedLanes.Count; laneIndex++)
        {
            var lane = generatedLanes[laneIndex];
            bool reachesCenter = lane.Contains(HexCoordinates.Zero);
            
            foreach (var coord in lane)
            {
                if (hexGrid.ContainsKey(coord))
                {
                    // Mark center as CenterHub, everything else as Pathway
                    if (coord == HexCoordinates.Zero)
                    {
                        hexGrid[coord].type = HexType.CenterHub;
                    }
                    else
                    {
                        hexGrid[coord].type = HexType.Pathway;
                    }
                    
                    // Assign lane ID - allow overlaps but track them
                    if (hexGrid[coord].laneId == -1)
                    {
                        hexGrid[coord].laneId = laneIndex;
                    }
                    else if (hexGrid[coord].laneId != laneIndex)
                    {
                        hexGrid[coord].isJunctionPoint = true;
                    }
                }
            }
            
           // Debug.Log($"[HexEnvironmentManager] Lane {laneIndex} generated with {lane.Count} hexes, reaches center: {reachesCenter}");
        }
    }
    
    private bool HasAdjacentDefenderSpots(HexCoordinates hex)
    {
        foreach (var direction in HEX_DIRECTIONS)
        {
            HexCoordinates adjacent = new HexCoordinates(hex.q + direction.q, hex.r + direction.r);
            
            if (hexGrid.ContainsKey(adjacent))
            {
                var hexData = hexGrid[adjacent];
                // Check if adjacent hex is environment (can become defender spot) or already a defender spot
                if (hexData.type == HexType.Environment || hexData.type == HexType.DefenderSpot)
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    private HexCoordinates GetClosestHexToCenter(HexCoordinates from)
    {
        HexCoordinates best = from;
        float bestDistance = HexDistance(from, HexCoordinates.Zero);
        
        foreach (var direction in HEX_DIRECTIONS)
        {
            HexCoordinates candidate = new HexCoordinates(from.q + direction.q, from.r + direction.r);
            if (hexGrid.ContainsKey(candidate))
            {
                float distance = HexDistance(candidate, HexCoordinates.Zero);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }
        }
        
        return best;
    }
    
    /// <summary>
    /// Calculate the distance between two hex coordinates
    /// </summary>
    public static float GetHexDistance(HexCoordinates a, HexCoordinates b)
    {
        return (Mathf.Abs(a.q - b.q) + Mathf.Abs(a.q + a.r - b.q - b.r) + Mathf.Abs(a.r - b.r)) / 2f;
    }
    
    private float HexDistance(HexCoordinates a, HexCoordinates b)
    {
        return GetHexDistance(a, b);
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
        
        // For each lane, create a new edge spawn hex connected to the first hex of the lane
        for (int laneIndex = 0; laneIndex < generatedLanes.Count; laneIndex++)
        {
            var lane = generatedLanes[laneIndex];
            if (lane.Count == 0) continue;
            
            // Get the first hex of this lane (the starting edge hex)
            HexCoordinates firstHex = lane[0];
            
            // Find the best direction to place the edge spawn (away from center)
            HexCoordinates edgeSpawnCoord = FindBestEdgeSpawnPosition(firstHex);
            
            if (edgeSpawnCoord != HexCoordinates.Zero)
            {
                // Create a new hex data for the edge spawn
                HexData edgeSpawnData = new HexData(edgeSpawnCoord, HexType.EdgeSpawn);
                edgeSpawnData.laneId = laneIndex;
                
                // Add to hex grid (this extends beyond the original grid radius)
                hexGrid[edgeSpawnCoord] = edgeSpawnData;
                laneSpawnPoints.Add(edgeSpawnCoord);
                
                //Debug.Log($"[HexEnvironmentManager] Created edge spawn at {edgeSpawnCoord} for lane {laneIndex}, connected to first hex {firstHex}");
            }
        }
        
        //Debug.Log($"[HexEnvironmentManager] Generated {laneSpawnPoints.Count} edge spawn points");
    }
    
    private HexCoordinates FindBestEdgeSpawnPosition(HexCoordinates firstHex)
    {
        // Calculate direction from center to first hex
        Vector3 centerPos = HexToWorldPosition(HexCoordinates.Zero);
        Vector3 firstHexPos = HexToWorldPosition(firstHex);
        Vector3 awayFromCenter = (firstHexPos - centerPos).normalized;
        
        // Find the direction that best aligns with moving away from center
        float bestAlignment = -1f;
        HexCoordinates bestDirection = HexCoordinates.Zero;
        
        foreach (var direction in HEX_DIRECTIONS)
        {
            HexCoordinates candidatePos = new HexCoordinates(
                firstHex.q + direction.q,
                firstHex.r + direction.r
            );
            
            // Skip if this position is already occupied by grid hexes
            if (hexGrid.ContainsKey(candidatePos)) continue;
            
            // Calculate alignment with away-from-center direction
            Vector3 candidateWorldPos = HexToWorldPosition(candidatePos);
            Vector3 candidateDirection = (candidateWorldPos - firstHexPos).normalized;
            float alignment = Vector3.Dot(candidateDirection, awayFromCenter);
            
            if (alignment > bestAlignment)
            {
                bestAlignment = alignment;
                bestDirection = candidatePos;
            }
        }
        
        return bestDirection;
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
            
            // Apply height displacement for terrain
            worldPos.y += hexData.height;
            
            // Only instantiate the hexPrefab - HexTile component will handle behavior
            GameObject hexGO = Instantiate(hexPrefab, worldPos, Quaternion.identity, hexParent);
            hexGO.name = $"Hex_{coord}_{hexData.type}";
            hexData.gameObject = hexGO;
            
            // Get the HexTile component (should already be on the prefab)
            HexTile hexTile = hexGO.GetComponent<HexTile>();
            if (hexTile != null)
            {
                // Get lane color if this hex is part of a lane
                Color hexColor = Color.white;
                if (hexData.laneId >= 0 && hexData.laneId < laneConfigurations.Count)
                {
                    hexColor = laneConfigurations[hexData.laneId].laneColor;
                }
                
                // Initialize the HexTile component with all the data
                hexTile.Initialize(
                    coord, 
                    hexData.type, 
                    hexData.laneId, 
                    hexData.isJunctionPoint, 
                    hexColor
                );
            }
            else
            {
                Debug.LogWarning($"[HexEnvironmentManager] HexTile component not found on hex prefab for {coord}_{hexData.type}");
            }
        }
    }
    
    private HexCoordinates FindClosestHexInDirection(HexCoordinates from, Vector3 worldDirection)
    {
        HexCoordinates bestHex = from;
        float bestDot = -1f;
        
        // First pass: try to find hex in desired direction that's not already in current lane
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
        
        // If we found a valid direction, return it
        if (bestHex != from && hexGrid.ContainsKey(bestHex))
        {
            return bestHex;
        }
        
        // Fallback: if no good direction found, just pick any valid adjacent hex
        foreach (var direction in HEX_DIRECTIONS)
        {
            HexCoordinates candidate = new HexCoordinates(from.q + direction.q, from.r + direction.r);
            if (hexGrid.ContainsKey(candidate))
            {
                return candidate; // Return first valid neighbor
            }
        }
        
        return from; // No valid moves (shouldn't happen if grid is properly generated)
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
    
    #region Terrain Generation
    
    /// <summary>
    /// Adds additional environment rings around the main grid for expanded terrain
    /// </summary>
    private void AddAdditionalEnvironmentRings()
    {
        int totalRings = gridRadius + additionalEnvironmentRings;
        
        // Generate additional rings beyond the main grid
        for (int ring = gridRadius + 1; ring <= totalRings; ring++)
        {
            List<HexCoordinates> ringCoords = GetHexRing(HexCoordinates.Zero, ring);
            
            foreach (var coord in ringCoords)
            {
                // Only add if not already in grid (avoid duplicates)
                if (!hexGrid.ContainsKey(coord))
                {
                    HexData hexData = new HexData(coord, HexType.Environment);
                    hexGrid[coord] = hexData;
                    generatedCoordinates.Add(coord);
                }
            }
        }
        
        Debug.Log($"[HexEnvironmentManager] Added {additionalEnvironmentRings} additional environment rings, total hexes: {hexGrid.Count}");
    }
    
    /// <summary>
    /// Generates terrain heights using Perlin noise for non-gameplay tiles (excludes pathways, center hub, and edge spawns)
    /// </summary>
    private void GenerateTerrainHeights()
    {
        Debug.Log("[HexEnvironmentManager] Generating terrain heights for non-gameplay hex tiles...");
        
        // Step 1: Generate raw Perlin noise heights for non-gameplay hexes
        GenerateRawHeights();
        
        // Step 2: Apply height smoothing to ensure connected terrain while keeping gameplay elements at ground level
        ApplyHeightSmoothing();
        
        int nonGameplayTileCount = hexGrid.Values.Count(h => h.type != HexType.Pathway && h.type != HexType.CenterHub && h.type != HexType.EdgeSpawn);
        Debug.Log($"[HexEnvironmentManager] Terrain height generation completed for {nonGameplayTileCount} non-gameplay hex tiles");
    }
    
    /// <summary>
    /// Generates initial height values using Perlin noise for all tile types except lanes and spawns
    /// </summary>
    private void GenerateRawHeights()
    {
        foreach (var kvp in hexGrid)
        {
            var hexData = kvp.Value;
            
            // Skip lanes (pathways and center hub) and edge spawns - keep them at ground level
            if (hexData.type == HexType.Pathway || hexData.type == HexType.CenterHub || hexData.type == HexType.EdgeSpawn)
            {
                hexData.height = 0f;
                continue;
            }
            
            Vector3 worldPos = HexToWorldPosition(hexData.coordinates);
            
            // Generate Perlin noise value (0-1 range)
            float noiseValue = Mathf.PerlinNoise(
                worldPos.x * noiseScale, 
                worldPos.z * noiseScale
            );
            
            // Convert to height range 0 to maxHeightVariation (0.5 max)
            float rawHeight = noiseValue * maxHeightVariation;
            
            // Ensure height is clamped between 0 and 0.5
            rawHeight = Mathf.Clamp(rawHeight, 0f, 0.5f);
            
            hexData.height = SnapToHeightContour(rawHeight);
        }
    }
    
    /// <summary>
    /// Applies smoothing passes to ensure hexes are properly connected, while keeping lanes and spawns at ground level
    /// </summary>
    private void ApplyHeightSmoothing()
    {
        for (int pass = 0; pass < heightSmoothingPasses; pass++)
        {
            Dictionary<HexCoordinates, float> newHeights = new Dictionary<HexCoordinates, float>();
            
            foreach (var kvp in hexGrid)
            {
                var hexData = kvp.Value;
                
                // Keep lanes (pathways and center hub) and edge spawns at ground level
                if (hexData.type == HexType.Pathway || hexData.type == HexType.CenterHub || hexData.type == HexType.EdgeSpawn)
                {
                    newHeights[hexData.coordinates] = 0f;
                    continue;
                }
                
                float currentHeight = hexData.height;
                float maxAllowedHeight = currentHeight;
                float minAllowedHeight = currentHeight;
                
                // Check all adjacent hexes
                foreach (var direction in HEX_DIRECTIONS)
                {
                    HexCoordinates adjacent = new HexCoordinates(
                        hexData.coordinates.q + direction.q,
                        hexData.coordinates.r + direction.r
                    );
                    
                    if (hexGrid.ContainsKey(adjacent))
                    {
                        float adjacentHeight = hexGrid[adjacent].height;
                        
                        // Ensure height difference doesn't exceed maximum
                        maxAllowedHeight = Mathf.Min(maxAllowedHeight, adjacentHeight + maxHeightDifference);
                        minAllowedHeight = Mathf.Max(minAllowedHeight, adjacentHeight - maxHeightDifference);
                    }
                }
                
                // Clamp height to allowed range and ensure it stays within 0-0.5
                float smoothedHeight = Mathf.Clamp(currentHeight, minAllowedHeight, maxAllowedHeight);
                smoothedHeight = Mathf.Clamp(smoothedHeight, 0f, 0.5f);
                newHeights[hexData.coordinates] = SnapToHeightContour(smoothedHeight);
            }
            
            // Apply new heights
            foreach (var kvp in newHeights)
            {
                if (hexGrid.ContainsKey(kvp.Key))
                {
                    hexGrid[kvp.Key].height = kvp.Value;
                }
            }
        }
    }
    
    /// <summary>
    /// Snaps a height value to the defined height contour
    /// </summary>
    private float SnapToHeightContour(float height)
    {
        if (heightContour <= 0f) return height;
        
        return Mathf.Round(height / heightContour) * heightContour;
    }
    
    #endregion
    
    private Vector3 HexToWorldPosition(HexCoordinates hex)
    {
        // For flat-top hexagons (as shown in image)
        float size = hexSize * (1f + hexSpacing);
        
        // Correct hexagonal grid positioning for flat-top orientation
        float x = size * (3f/2f * hex.q);
        float z = size * (SQRT_3/2f * hex.q + SQRT_3 * hex.r);
        
        // Center point (HexCoordinates.Zero) will always be at world origin (0,0,0)
        return new Vector3(x, 0, z);
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
                
                // Apply height displacement for terrain visualization
                pos.y += hexData.height;
                
                // Get enhanced color for hex type - ensure solid color
                Color hexColor = GetEnhancedColorForHexType(hexData.type, hexData.laneId, hexData.height);
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
            if (lane.Count < 1) continue;
            
            // Skip if only showing specific lane and this isn't it
            if (showOnlyLane >= 0 && laneIndex != showOnlyLane) continue;
            
            // Get lane color
            Color connectionColor = laneIndex < laneConfigurations.Count ? 
                                  laneConfigurations[laneIndex].laneColor : Color.yellow;
            connectionColor.a = 0.5f;
            Gizmos.color = connectionColor;
            
            // Draw connection from edge spawn to first hex of lane if edge spawn exists
            foreach (var spawnCoord in laneSpawnPoints)
            {
                if (hexGrid.ContainsKey(spawnCoord) && hexGrid[spawnCoord].laneId == laneIndex)
                {
                    Vector3 spawnPos = HexToWorldPosition(spawnCoord) + Vector3.up * (hexSize * 0.1f);
                    Vector3 firstHexPos = HexToWorldPosition(lane[0]) + Vector3.up * (hexSize * 0.1f);
                    
                    // Use a distinct color for spawn connections
                    Color spawnConnectionColor = edgeSpawnColor;
                    spawnConnectionColor.a = 0.8f;
                    Gizmos.color = spawnConnectionColor;
                    
                    // Draw thick line from spawn to first hex
                    DrawThickLine(spawnPos, firstHexPos, hexSize * 0.15f);
                    
                    // Draw arrow pointing toward the first hex
                    Vector3 direction = (firstHexPos - spawnPos).normalized;
                    DrawArrow(firstHexPos - direction * (hexSize * 0.3f), direction, hexSize * 0.4f);
                    
                    // Reset color for lane connections
                    Gizmos.color = connectionColor;
                    break;
                }
            }
            
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
    
    private Color GetEnhancedColorForHexType(HexType type, int laneId = -1, float height = 0f)
    {
        Color baseColor = type switch
        {
            HexType.CenterHub => centerHubColor,
            HexType.Pathway => GetLaneColor(laneId),
            HexType.DefenderSpot => defenderSpotColor,
            HexType.EdgeSpawn => edgeSpawnColor,
            HexType.Environment => environmentColor,
            _ => environmentColor
        };
        
        // Apply height-based color modification to all tile types
        return ApplyHeightColorModification(baseColor, height);
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
    
    /// <summary>
    /// Applies height-based color modification to any tile type for better terrain visualization
    /// </summary>
    private Color ApplyHeightColorModification(Color baseColor, float height)
    {
        if (maxHeightVariation <= 0f)
        {
            return baseColor;
        }
        
        // Normalize height to 0-1 range
        float normalizedHeight = Mathf.Clamp01(height / maxHeightVariation);
        
        // Create height-based color gradient: darken for low areas, brighten for high areas
        Color heightModifiedColor = Color.Lerp(baseColor * 0.7f, baseColor * 1.3f, normalizedHeight);
        
        // Preserve alpha channel
        heightModifiedColor.a = baseColor.a;
        
        return heightModifiedColor;
    }
    
    /// <summary>
    /// Gets environment color based on height for better terrain visualization
    /// </summary>
    private Color GetEnvironmentColorByHeight(float height)
    {
        if (maxHeightVariation <= 0f)
        {
            return environmentColor;
        }
        
        // Normalize height to 0-1 range
        float normalizedHeight = Mathf.Clamp01(height / maxHeightVariation);
        
        // Create height-based color gradient from dark to light
        Color baseColor = environmentColor;
        Color heightColor = Color.Lerp(baseColor * 0.6f, Color.white, normalizedHeight * 0.4f);
        
        return heightColor;
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
                
                // Draw attack path arrows from edge spawn to center and connected pathways
                DrawAttackPathArrows(center, size, laneId, solidColor);
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
    
    private void DrawAttackPathArrows(Vector3 spawnCenter, float hexSize, int spawnLaneId, Color color)
    {
        // Find the current spawn hex coordinates
        HexCoordinates spawnCoord = WorldToHex(spawnCenter);
        
        // Get connected pathway hexes (adjacent hexes that are pathways)
        List<Vector3> attackDirections = new List<Vector3>();
        
        // Check all 6 adjacent hexes for pathway connections
        foreach (var direction in HEX_DIRECTIONS)
        {
            HexCoordinates adjacentCoord = new HexCoordinates(
                spawnCoord.q + direction.q,
                spawnCoord.r + direction.r
            );
            
            if (hexGrid.ContainsKey(adjacentCoord))
            {
                var adjacentHex = hexGrid[adjacentCoord];
                
                // If adjacent hex is a pathway, add arrow direction
                if (adjacentHex.type == HexType.Pathway)
                {
                    Vector3 adjacentPos = HexToWorldPosition(adjacentCoord);
                    Vector3 attackDirection = (adjacentPos - spawnCenter).normalized;
                    attackDirections.Add(attackDirection);
                }
            }
        }
        
        // Always add arrow towards center hub
        Vector3 centerDirection = (HexToWorldPosition(HexCoordinates.Zero) - spawnCenter).normalized;
        attackDirections.Add(centerDirection);
        
        // Draw attack arrows
        Gizmos.color = pathwayConnectionColor;
        float arrowLength = hexSize * 0.8f;
        float arrowHeight = hexSize * 0.3f;
        
        for (int i = 0; i < attackDirections.Count; i++)
        {
            Vector3 direction = attackDirections[i];
            Vector3 startPos = spawnCenter + Vector3.up * arrowHeight;
            
            // Use different colors for different arrow types
            if (i == attackDirections.Count - 1) // Last arrow (to center)
            {
                Gizmos.color = Color.red; // Red for direct path to center
            }
            else
            {
                Gizmos.color = pathwayConnectionColor; // Yellow for pathway connections
            }
            
            // Draw thicker, more prominent arrows
            DrawAttackArrow(startPos, direction, arrowLength, hexSize * 0.15f);
        }
    }
    
    private void DrawAttackArrow(Vector3 startPos, Vector3 direction, float length, float thickness)
    {
        Vector3 endPos = startPos + direction * length;
        
        // Draw main arrow shaft with thickness
        DrawThickLine(startPos, endPos, thickness);
        
        // Draw larger arrowhead
        Vector3 right = Vector3.Cross(direction, Vector3.up) * length * 0.3f;
        Vector3 up = Vector3.up * length * 0.2f;
        
        Vector3 arrowLeft = endPos - direction * length * 0.4f - right + up;
        Vector3 arrowRight = endPos - direction * length * 0.4f + right + up;
        Vector3 arrowDown = endPos - direction * length * 0.4f - up;
        
        // Draw 3D arrowhead
        DrawThickLine(endPos, arrowLeft, thickness * 0.8f);
        DrawThickLine(endPos, arrowRight, thickness * 0.8f);
        DrawThickLine(endPos, arrowDown, thickness * 0.8f);
        DrawThickLine(arrowLeft, arrowRight, thickness * 0.6f);
        DrawThickLine(arrowLeft, arrowDown, thickness * 0.6f);
        DrawThickLine(arrowRight, arrowDown, thickness * 0.6f);
    }
    
    #endregion
}
