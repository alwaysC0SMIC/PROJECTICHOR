using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Linq;
using TMPro;

[Serializable]
public class WaveArchetype
{
    public string name;
    public List<SO_Enemy> allowedEnemyTypes;
    [Tooltip("Higher weight means this archetype is more likely to be chosen.")]
    public float baseWeight = 1f;
}

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

    [Title("Wave Composition")]
    public List<WaveArchetype> waveArchetypes;
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
        if (waypoints.Count == 0 || waveSettings.waveArchetypes.Count == 0)
        {
            Debug.LogWarning("Cannot spawn wave: No waypoints or archetypes defined.");
            yield break;
        }

        float threatBudget = CalculateThreatBudget();
        WaveArchetype archetype = SelectWaveArchetype();

        Debug.Log($"Wave {currentWaveNumber}: Threat Budget={threatBudget:F0}, Archetype='{archetype.name}'");

        // This is a simple implementation. A more advanced one would use the "Lane Defense Score"
        // and distribute the budget unevenly. For now, we split it evenly.
        float budgetPerLane = threatBudget / waypoints.Count;

        for (int i = 0; i < waypoints.Count; i++)
        {
            StartCoroutine(SpawnForLane(i, budgetPerLane, archetype));
        }

        // The spawning of enemies for this wave is complete.
        // The WaveLoop will now wait for them to be defeated.
        yield return null;
    }

    private IEnumerator SpawnForLane(int laneIndex, float budget, WaveArchetype archetype)
    {
        List<Transform> laneWaypoints = waypoints[laneIndex];
        if (laneWaypoints.Count == 0) yield break;

        Vector3 spawnPosition = laneWaypoints[0].position;

        while (budget > 0)
        {
            // Filter enemies that are allowed by the archetype
            var availableEnemies = archetype.allowedEnemyTypes
                .Where(e => e.threatCost > 0 && e.threatCost <= budget)
                .ToList();

            if (availableEnemies.Count == 0)
            {
                // Cannot afford any more enemies in this archetype
                break;
            }

            SO_Enemy enemyToSpawn = availableEnemies[UnityEngine.Random.Range(0, availableEnemies.Count)];

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

    private WaveArchetype SelectWaveArchetype()
    {
        // This is a simple random selection. A more advanced system would
        // analyze player towers and adjust weights.
        float totalWeight = waveSettings.waveArchetypes.Sum(a => a.baseWeight);
        float randomPoint = UnityEngine.Random.Range(0, totalWeight);

        foreach (var archetype in waveSettings.waveArchetypes)
        {
            if (randomPoint < archetype.baseWeight) return archetype;
            randomPoint -= archetype.baseWeight;
        }

        return waveSettings.waveArchetypes[0]; // Fallback
    }

    [Button("Spawn Enemy")]
    public void SpawnEnemy()
    {
        if (enemyDataList.Count == 0 || enemyDataList[0].prefab == null)
        {
            Debug.LogError("Cannot spawn test enemy: Enemy data list is empty or the first enemy is missing a prefab.");
            return;
        }

        GameObject enemy = Instantiate(enemyDataList[0].prefab, waypoints[0][0].position, Quaternion.identity, transform);
        enemy.GetComponent<Enemy>().Initialize(enemyDataList[0], waypoints[0]);
    }
}
