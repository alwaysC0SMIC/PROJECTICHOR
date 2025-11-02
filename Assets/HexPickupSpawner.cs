using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HexPickupSpawner : MonoBehaviour
{
    #region VARIABLES

    [Header("REFERENCES")]
    [SerializeField] private HexEnvironmentManager hexEnvironmentManager;

    [Header("SPAWN SETTINGS")]
    #region VARIABLES

    [Header("SPAWN SETTINGS")]
    [SerializeField, Min(0.25f)] private float minSpawnInterval = 1.5f;
    [SerializeField, Min(0.25f)] private float maxSpawnInterval = 4.5f;
    [SerializeField, Min(1)] private int maxActivePickups = 6;
    [SerializeField] private float pickupLifetime = 5f;


    [Header("PREFABS (3 TIERS)")]
    [SerializeField]
    private List<PickupDefinition> pickupDefinitions = new List<PickupDefinition>()
    {
        // YOU CAN TWEAK THESE IN THE INSPECTOR
        new PickupDefinition("Tier 1 (5-20)",   null, 5, 20, 60f),
        new PickupDefinition("Tier 2 (20-50)",  null, 20, 50, 30f),
        new PickupDefinition("Tier 3 (50-100)", null, 50, 100, 10f),
    };

    // TRACK WHICH HEXES CURRENTLY HAVE A PICKUP ON THEM
    private readonly HashSet<HexCoordinates> occupiedTiles = new HashSet<HexCoordinates>();

    // OPTIONAL: KEEP ACTUAL INSTANCES
    private readonly List<CurrencyPickup> activePickups = new List<CurrencyPickup>();

    // SIMPLE TIMER
    private float spawnTimer;
    private float currentSpawnInterval;
    private EventBinding<EnvironmentGeneratedEvent> environmentGeneratedEvent;

    #endregion

    #region UNITY EVENTS

    private void Awake()
    {
        // IF NOT SET IN INSPECTOR, TRY TO GRAB IT
        if (hexEnvironmentManager == null)
            hexEnvironmentManager = GetComponent<HexEnvironmentManager>();
    }

    private void OnEnable()
    {
        environmentGeneratedEvent = new EventBinding<EnvironmentGeneratedEvent>(OnEnvironmentGenerated);
        EventBus<EnvironmentGeneratedEvent>.Register(environmentGeneratedEvent);

        // INITIALIZE FIRST RANDOM INTERVAL
        currentSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private void OnDisable()
    {
        EventBus<EnvironmentGeneratedEvent>.Deregister(environmentGeneratedEvent);
    }

    private void Update()
    {
        if (hexEnvironmentManager == null)
            return;

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= currentSpawnInterval && activePickups.Count < maxActivePickups)
        {
            spawnTimer = 0f;
            TrySpawnPickup();

            // ASSIGN A NEW RANDOM INTERVAL AFTER EACH SPAWN
            currentSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
        }
    }

    #endregion

    #region EVENT HANDLERS

    private void OnEnvironmentGenerated(EnvironmentGeneratedEvent evt)
    {
        // ENVIRONMENT HAS BEEN REBUILT → CLEAR
        occupiedTiles.Clear();

        // CLEAN ANY LEFTOVER PICKUPS IN SCENE
        for (int i = activePickups.Count - 1; i >= 0; i--)
        {
            if (activePickups[i] == null) continue;
            Destroy(activePickups[i].gameObject);
        }
        activePickups.Clear();
    }

    #endregion

    #region SPAWN LOGIC

    private void TrySpawnPickup()
    {
        // 1) GET ALL NON-EXTERIOR ENVIRONMENT HEXES THAT HAVE A GAMEOBJECT (SO WE CAN POSITION)
        var envHexes = hexEnvironmentManager
            .GetHexesOfType(HexType.Environment)
            .Where(h => h.gameObject != null && !h.isExteriorEnvironment) // ✅ FILTER OUT EXTERIOR ENVIRONMENT
            .ToList();

        if (envHexes.Count == 0)
            return;

        // 2) FILTER OUT OCCUPIED TILES
        var freeHexes = envHexes
            .Where(h => !occupiedTiles.Contains(h.coordinates))
            .ToList();

        if (freeHexes.Count == 0)
            return;

        // 3) PICK A RANDOM FREE TILE
        var chosenHex = freeHexes[Random.Range(0, freeHexes.Count)];

        // 4) PICK A RANDOM PICKUP DEFINITION BASED ON WEIGHT
        var def = GetRandomPickupDefinition();
        if (def == null || def.prefab == null)
        {
            // NO PREFAB SET → JUST EXIT
            return;
        }

        // 5) SPAWN IT SLIGHTLY ABOVE TILE
        Vector3 pos = chosenHex.gameObject.transform.position + Vector3.up * 0.25f;
        Quaternion rot = Quaternion.identity;
        var pickupGO = Instantiate(def.prefab, pos, rot);

        // 6) CONFIGURE THE PICKUP
        var pickup = pickupGO.GetComponent<CurrencyPickup>();
        if (pickup != null)
        {
            pickup.Setup(
                chosenHex.coordinates,
                def.minCurrency,
                def.maxCurrency,
                pickupLifetime,
                OnPickupDespawned
            );
        }

        // 7) MARK TILE AS OCCUPIED
        occupiedTiles.Add(chosenHex.coordinates);
        activePickups.Add(pickup);
    }


    private PickupDefinition GetRandomPickupDefinition()
    {
        // WEIGHTED RANDOM
        float totalWeight = 0f;
        for (int i = 0; i < pickupDefinitions.Count; i++)
            totalWeight += Mathf.Max(0f, pickupDefinitions[i].spawnWeight);

        if (totalWeight <= 0f)
            return null;

        float roll = Random.value * totalWeight;
        float accum = 0f;

        for (int i = 0; i < pickupDefinitions.Count; i++)
        {
            accum += Mathf.Max(0f, pickupDefinitions[i].spawnWeight);
            if (roll <= accum)
                return pickupDefinitions[i];
        }

        return pickupDefinitions[pickupDefinitions.Count - 1];
    }

    #endregion

    #region CALLBACKS

    // CALLED BY CurrencyPickup WHEN:
    // - PLAYER CLICKED IT
    // - OR IT TIMED OUT
    private void OnPickupDespawned(CurrencyPickup pickup)
    {
        if (pickup == null)
            return;

        // FREE TILE
        if (occupiedTiles.Contains(pickup.OwningTile))
            occupiedTiles.Remove(pickup.OwningTile);

        // REMOVE FROM ACTIVE LIST
        activePickups.Remove(pickup);
    }

    #endregion

    #region NESTED

    [System.Serializable]
    public class PickupDefinition
    {
        public string name;
        public GameObject prefab;
        public int minCurrency;
        public int maxCurrency;
        public float spawnWeight = 1f;

        public PickupDefinition(string name, GameObject prefab, int min, int max, float weight)
        {
            this.name = name;
            this.prefab = prefab;
            this.minCurrency = min;
            this.maxCurrency = max;
            this.spawnWeight = weight;
        }
    }
    #endregion

}
#endregion