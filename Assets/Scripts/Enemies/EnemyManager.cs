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


    #region EVENTS
    private void OnEnable()
    {
        pathwayTransformsBinding = new EventBinding<PathwayTransformsEvent>(OnPathwayTransformsCreated);
        EventBus<PathwayTransformsEvent>.Register(pathwayTransformsBinding);
    }

    private void OnDisable()
    {
        EventBus<PathwayTransformsEvent>.Deregister(pathwayTransformsBinding);
    }

    #endregion

    private void OnPathwayTransformsCreated(PathwayTransformsEvent evt)
    {
        waypoints.Clear();
        waypoints = evt.pathwayTransformsByLane;
    }


    [Button("Spawn Enemy")]
    public void SpawnEnemy()
    {
        GameObject enemy = Instantiate(enemyPrefab, waypoints[0][0].position, Quaternion.identity, transform);
        enemy.GetComponent<Enemy>().Initialize(enemyDataList[0], waypoints[0]);
    }
}
