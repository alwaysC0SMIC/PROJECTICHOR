using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using System;

public class EnemyManager : MonoBehaviour
{
    //VARIABLES
    [SerializeField] private HexEnvironmentManager hexEnvironmentManager;
    [SerializeField] private List<SO_Enemy> enemyDataList;
    [SerializeField] private GameObject enemyPrefab;
    
    private List<List<Transform>> waypoints = new List<List<Transform>>();
    private EventBinding<PathwayTransformsEvent> pathwayTransformsBinding;

    private EventBinding<UpdateGameStateEvent> gameStateBinding;


    #region EVENTS
    private void OnEnable()
    {
        pathwayTransformsBinding = new EventBinding<PathwayTransformsEvent>(OnPathwayTransformsCreated);
        EventBus<PathwayTransformsEvent>.Register(pathwayTransformsBinding);

        gameStateBinding = new EventBinding<UpdateGameStateEvent>(OnGameStateUpdated);
        EventBus<UpdateGameStateEvent>.Register(gameStateBinding);
    }

    private void OnGameStateUpdated(UpdateGameStateEvent @event)
    {
        if(@event.gameState == GameState.Playing)
        {
            StartWaves();
        }
    }

    private void OnDisable()
    {
        EventBus<PathwayTransformsEvent>.Deregister(pathwayTransformsBinding);
        EventBus<UpdateGameStateEvent>.Deregister(gameStateBinding);
    }

    #endregion

    private void OnPathwayTransformsCreated(PathwayTransformsEvent evt)
    {
        waypoints.Clear();
        waypoints = evt.pathwayTransformsByLane;
    }

    private void StartWaves()
    { 
        StartCoroutine(SpawnEnemyWave());
    }

    private System.Collections.IEnumerator SpawnEnemyWave()
    {
        while (true)
        {
            // Wait 5 seconds before spawning next enemy
            yield return new WaitForSeconds(5f);
            
            // Only spawn if we have waypoints available
            if (waypoints.Count > 0 && enemyDataList.Count > 0)
            {
                // Select a random waypoint list (lane)
                int randomLaneIndex = UnityEngine.Random.Range(0, waypoints.Count);
                List<Transform> selectedLane = waypoints[randomLaneIndex];
                
                // Make sure the selected lane has waypoints
                if (selectedLane.Count > 0)
                {
                    // Select random enemy data
                    int randomEnemyIndex = UnityEngine.Random.Range(0, enemyDataList.Count);
                    SO_Enemy selectedEnemyData = enemyDataList[randomEnemyIndex];
                    
                    // Spawn enemy at the beginning of the selected lane
                    Vector3 spawnPosition = selectedLane[0].position;
                    GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, transform);
                    enemy.GetComponent<Enemy>().Initialize(selectedEnemyData, selectedLane);
                    
                    Debug.Log($"Spawned enemy at lane {randomLaneIndex} with enemy type {selectedEnemyData.enemyName}");
                }
            }
        }
    }


    [Button("Spawn Enemy")]
    public void SpawnEnemy()
    {
        GameObject enemy = Instantiate(enemyPrefab, waypoints[0][0].position, Quaternion.identity, transform);
        enemy.GetComponent<Enemy>().Initialize(enemyDataList[0], waypoints[0]);
    }
}
