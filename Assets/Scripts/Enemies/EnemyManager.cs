using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Linq;
using TMPro;

[Serializable]
public class WaveSettings
{
    [Title("Threat Progression")]
    [Tooltip("The starting threat budget for wave 1.")]
    public float baseThreat = 100f;
    [Tooltip("How quickly the threat budget increases per wave. (e.g., 1.15 for 15% increase)")]
    public float threatExponent = 1.15f;
    [Tooltip("Time in seconds between waves.")]
    public float timeBetweenWaves = 10f;

    [Title("Performance-Based Adjustment (PBA)")]
    [Range(0, 1), Tooltip("How much player's hub health affects difficulty. 0 = no effect, 1 = full effect.")]
    public float hubHealthModifier = 0.5f;
    [Range(0, 1), Tooltip("How much player's coin balance affects difficulty. 0 = no effect, 1 = full effect.")]
    public float coinBalanceModifier = 0.3f;
    [Range(0.5f, 1.5f), Tooltip("The maximum difficulty increase/decrease from PBA.")]
    public float pbaClamp = 1.3f;

    [Title("Procedural Wave Generation")]
    [Tooltip("A new, more powerful enemy type will be eligible for spawning every this many waves.")]
    public int newEnemyIntroductionInterval = 3;
    [Tooltip("The maximum number of unique enemy types that can appear in a single wave.")]
    public int maxEnemyTypesPerWave = 3;

    [Title("Wave Focus Chances")]
    [Range(0, 1), Tooltip("The chance for a wave to be focused on numerous, cheap enemies.")]
    public float swarmFocusChance = 0.4f;
    [Range(0, 1), Tooltip("The chance for a wave to be focused on a few, expensive enemies.")]
    public float eliteFocusChance = 0.3f;
}

public class EnemyManager : MonoBehaviour
{
    //VARIABLES
    [TitleGroup("Setup")]
    [SerializeField] private HexEnvironmentManager hexEnvironmentManager;
    [TitleGroup("Setup")]
    [SerializeField] private List<SO_Enemy> enemyDataList;

    [TitleGroup("Wave Configuration")]
    [SerializeField] private WaveSettings waveSettings;

    [TitleGroup("UI")]
    [SerializeField] private TextMeshProUGUI waveStatusText;

    [TitleGroup("Runtime State")]
    [ReadOnly, ShowInInspector] private int currentWaveNumber = 0;
    [ReadOnly, ShowInInspector] private bool isSpawning = false;
    [ReadOnly, ShowInInspector] private List<GameObject> activeEnemies = new List<GameObject>();

    private List<List<Transform>> waypoints = new List<List<Transform>>();
    private EventBinding<PathwayTransformsEvent> pathwayTransformsBinding;
    private EventBinding<EnemyDestroyedEvent> enemyDestroyedBinding;
    private EventBinding<UpdateGameStateEvent> gameStateBinding;

    // Sorted list of enemies used for procedural generation
    private List<SO_Enemy> sortedEnemyDataList;

    private void Awake()
    {
        SortEnemyData();
    }


    #region EVENTS
    private void OnEnable()
    {
        pathwayTransformsBinding = new EventBinding<PathwayTransformsEvent>(OnPathwayTransformsCreated);
        EventBus<PathwayTransformsEvent>.Register(pathwayTransformsBinding);

        gameStateBinding = new EventBinding<UpdateGameStateEvent>(OnGameStateUpdated);
        EventBus<UpdateGameStateEvent>.Register(gameStateBinding);

        enemyDestroyedBinding = new EventBinding<EnemyDestroyedEvent>(OnEnemyDestroyed);
        EventBus<EnemyDestroyedEvent>.Register(enemyDestroyedBinding);
    }

    private void OnGameStateUpdated(UpdateGameStateEvent @event)
    {
        if (@event.gameState == GameState.Playing && !isSpawning)
        {
            StartWaves();
        }
    }

    private void OnDisable()
    {
        EventBus<PathwayTransformsEvent>.Deregister(pathwayTransformsBinding);
        EventBus<UpdateGameStateEvent>.Deregister(gameStateBinding);
        EventBus<EnemyDestroyedEvent>.Deregister(enemyDestroyedBinding);
    }

    #endregion

    private void SortEnemyData()
    {
        if (enemyDataList == null || enemyDataList.Count == 0)
        {
            sortedEnemyDataList = new List<SO_Enemy>();
            return;
        }
        sortedEnemyDataList = enemyDataList.OrderBy(e => e.threatCost).ToList();
    }
    private void OnPathwayTransformsCreated(PathwayTransformsEvent evt)
    {
        waypoints.Clear();
        waypoints = evt.pathwayTransformsByLane;
    }

    private void OnEnemyDestroyed(EnemyDestroyedEvent evt)
    {
        // This is more efficient than iterating the list every frame.
        activeEnemies.Remove(evt.enemyObject);
    }

    [Button("Start Waves")]
    private void StartWaves()
    {
        if (isSpawning) return;
        Debug.Log("Starting enemy waves...");
        isSpawning = true;
        StartCoroutine(WaveLoop());
    }

    private IEnumerator WaveLoop()
    {
        while (isSpawning)
        {
            // --- Pre-Wave Countdown ---
            if (currentWaveNumber > 0)
            {
                if (waveStatusText) waveStatusText.text = $"<color=green>Wave {currentWaveNumber} Cleared!</color>";

                float countdown = waveSettings.timeBetweenWaves;
                while (countdown > 0)
                {
                    if (waveStatusText) waveStatusText.text = $"Next wave in {Mathf.CeilToInt(countdown)}...";
                    yield return new WaitForSeconds(1f);
                    countdown--;
                }
            }

            // --- Wave Start ---
            currentWaveNumber++;
            if (waveStatusText) waveStatusText.text = $"Wave {currentWaveNumber}";
            Debug.Log($"--- Starting Wave {currentWaveNumber} ---");

            yield return StartCoroutine(SpawnWave());

            // Wait for all enemies from the current wave to be defeated
            yield return new WaitUntil(() => activeEnemies.Count == 0);
        }

        isSpawning = false;
    }

    private IEnumerator SpawnWave()
    {
        if (waypoints.Count == 0 || sortedEnemyDataList.Count == 0)
        {
            Debug.LogWarning("Cannot spawn wave: No waypoints or enemies defined.");
            yield break;
        }

        float threatBudget = CalculateThreatBudget();

        // Procedurally determine the composition of this wave
        List<SO_Enemy> waveEnemyPool = DetermineWaveEnemyPool();
        Debug.Log($"Wave {currentWaveNumber}: Threat Budget={threatBudget:F0}. Pool: [{string.Join(", ", waveEnemyPool.Select(e => e.enemyName))}]");

        // This is a simple implementation. A more advanced one would use the "Lane Defense Score"
        // and distribute the budget unevenly. For now, we split it evenly.
        float budgetPerLane = threatBudget / waypoints.Count;

        for (int i = 0; i < waypoints.Count; i++)
        {
            StartCoroutine(SpawnForLane(i, budgetPerLane, waveEnemyPool));
        }

        // The spawning of enemies for this wave is complete.
        // The WaveLoop will now wait for them to be defeated.
        yield return null;
    }

    private IEnumerator SpawnForLane(int laneIndex, float budget, List<SO_Enemy> enemyPool)
    {
        List<Transform> laneWaypoints = waypoints[laneIndex];
        if (laneWaypoints.Count == 0) yield break;

        Vector3 spawnPosition = laneWaypoints[0].position;

        while (budget > 0)
        {
            var affordableEnemies = enemyPool
                .Where(e => e.threatCost <= budget && e.threatCost > 0)
                .ToList();

            if (affordableEnemies.Count == 0)
            {
                break;
            }

            SO_Enemy enemyToSpawn = affordableEnemies[UnityEngine.Random.Range(0, affordableEnemies.Count)];

            if (enemyToSpawn.prefab == null)
            {
                Debug.LogError($"Enemy type '{enemyToSpawn.name}' is missing a prefab. Skipping spawn.", enemyToSpawn);
                continue;
            }

            GameObject enemy = Instantiate(enemyToSpawn.prefab, spawnPosition, Quaternion.identity, transform);
            enemy.GetComponent<Enemy>().Initialize(enemyToSpawn, laneWaypoints);
            activeEnemies.Add(enemy);

            budget -= enemyToSpawn.threatCost;

            // Stagger spawns within the lane
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.3f, 1.0f));
        }
    }

    private void Update()
    {
        // Clean up destroyed enemies from the list
        // This is a fallback. The OnEnemyDestroyed event is the primary method.
        if (activeEnemies.Any(e => e == null))
        {
            activeEnemies.RemoveAll(item => item == null);
        }
    }

    private float CalculateThreatBudget()
    {
        float baseBudget = waveSettings.baseThreat * Mathf.Pow(currentWaveNumber, waveSettings.threatExponent);

        // Performance-Based Adjustment (PBA)
        float pba = 1.0f;
        // Example: If hub is at 50% health, modifier is 1 - (0.5 * (1 - 0.5)) = 0.75
        // float hubHealthPercent = GetHubHealthPercent(); // You'll need to implement this
        // pba -= (1 - hubHealthPercent) * waveSettings.hubHealthModifier;

        // Example: If player has many coins, increase difficulty
        // float coins = PlayerData.Instance.Coins; // Assuming a PlayerData singleton
        // if (coins > 500) pba += 0.1f * waveSettings.coinBalanceModifier;

        pba = Mathf.Clamp(pba, 1 / waveSettings.pbaClamp, waveSettings.pbaClamp);

        return baseBudget * pba;
    }

    private List<SO_Enemy> DetermineWaveEnemyPool()
    {
        // 1. Determine which enemies are available based on the current wave number
        int maxEnemyIndex = (currentWaveNumber - 1) / waveSettings.newEnemyIntroductionInterval;
        int availableEnemyCount = Mathf.Min(sortedEnemyDataList.Count, maxEnemyIndex + 1);
        var availableEnemies = sortedEnemyDataList.Take(availableEnemyCount).ToList();

        if (availableEnemies.Count == 0) return new List<SO_Enemy>();

        // 2. Determine the focus for this wave (Swarm, Elite, or Mixed)
        float roll = UnityEngine.Random.value;
        List<SO_Enemy> selectedPool = new List<SO_Enemy>();

        if (roll < waveSettings.swarmFocusChance) // Swarm wave
        {
            // Focus on the cheapest 33% of available enemies
            int count = Mathf.Max(1, availableEnemies.Count / 3);
            selectedPool = availableEnemies.Take(count).ToList();
        }
        else if (roll < waveSettings.swarmFocusChance + waveSettings.eliteFocusChance) // Elite wave
        {
            // Focus on the most expensive 33% of available enemies
            int count = Mathf.Max(1, availableEnemies.Count / 3);
            selectedPool = availableEnemies.Skip(availableEnemies.Count - count).ToList();
        }
        else // Mixed wave
        {
            selectedPool = availableEnemies;
        }

        // 3. Select a subset of enemies for this wave's pool to create variety
        List<SO_Enemy> finalPool = new List<SO_Enemy>();
        int typesToSelect = Mathf.Min(selectedPool.Count, waveSettings.maxEnemyTypesPerWave);

        while (finalPool.Count < typesToSelect)
        {
            SO_Enemy candidate = selectedPool[UnityEngine.Random.Range(0, selectedPool.Count)];
            if (!finalPool.Contains(candidate))
            {
                finalPool.Add(candidate);
            }
        }

        return finalPool;
    }

    [Button("Spawn Enemy")]
    public void SpawnEnemy()
    {
        if (sortedEnemyDataList.Count == 0 || sortedEnemyDataList[0].prefab == null)
        {
            Debug.LogError("Cannot spawn test enemy: Enemy data list is empty or the first enemy is missing a prefab.");
            return;
        }

        GameObject enemy = Instantiate(sortedEnemyDataList[0].prefab, waypoints[0][0].position, Quaternion.identity, transform);
        enemy.GetComponent<Enemy>().Initialize(sortedEnemyDataList[0], waypoints[0]);
    }
}
