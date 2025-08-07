using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    // Event binding for PathwayTransformsEvent
    private EventBinding<PathwayTransformsEvent> pathwayTransformsBinding;
    private EventBinding<EnvironmentGeneratedEvent> environmentGeneratedBinding;
    // Store pathway transforms when received
    public List<List<Transform>> pathwayTransformsByLane;
    
    // Store all spawned enemies for cleanup
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    [SerializeField] private GameObject testEnemyPrefab;
    
    #region DEBUGGING

    [Header("Gizmo Settings")]
    [SerializeField] private bool showPathwayGizmos = true;
    [SerializeField] private bool showPathwayLines = true;
    [SerializeField] private bool showPathwayPoints = true;
    [SerializeField] private float gizmoSphereSize = 0.3f;
    [SerializeField] private float lineWidth = 3f;
    [SerializeField] private Color[] laneColors = new Color[] 
    {
        Color.red,
        Color.blue, 
        Color.green,
        Color.yellow,
        Color.magenta,
        Color.cyan
    };
    [SerializeField] private Color edgeSpawnerColor = Color.white;
    [SerializeField] private float edgeSpawnerSize = 0.5f;
    
    #endregion

    void Start()
    {
        
    }


    public void Initialize()
    {
        // Clear existing enemies when environment is regenerated
        ClearAllEnemies();
        
        // Request pathway transforms from HexEnvironmentManager
        RequestPathwayTransforms();
    }
    
    public GameObject SpawnEnemyOnLane(int laneId)
    {
        return SpawnEnemyOnLane(laneId, testEnemyPrefab);
    }
    
    public GameObject SpawnEnemyOnLane(int laneId, GameObject enemyPrefab)
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning($"[EnemyManager] No enemy prefab provided for lane {laneId}!");
            return null;
        }
        
        List<Transform> pathTransforms = GetPathwayTransformsForLane(laneId);
        
        if (pathTransforms == null || pathTransforms.Count == 0)
        {
            Debug.LogWarning($"[EnemyManager] No pathway transforms available for lane {laneId}!");
            return null;
        }
        
        Transform edgeSpawner = pathTransforms[0];
        if (edgeSpawner == null)
        {
            Debug.LogWarning($"[EnemyManager] Edge spawner not found for lane {laneId}!");
            return null;
        }
        
        Vector3 spawnPosition = edgeSpawner.position;
        Quaternion spawnRotation = edgeSpawner.rotation;
        
        // Optionally offset the spawn position slightly to avoid overlapping with the spawner
        Vector3 spawnOffset = Vector3.up * 0.1f; // Small upward offset
        spawnPosition += spawnOffset;
        
        GameObject enemyObj = Instantiate(enemyPrefab, spawnPosition, spawnRotation, transform);
        
        // Add to spawned enemies list for tracking
        spawnedEnemies.Add(enemyObj);
        
        // Initialize the enemy with the pathway
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.InitializeWithWaypoints(pathTransforms);
            Debug.Log($"[EnemyManager] Spawned enemy on lane {laneId} at edge spawner position {spawnPosition}. Total enemies: {spawnedEnemies.Count}");
        }
        else
        {
            Debug.LogWarning($"[EnemyManager] Enemy prefab does not have an Enemy component!");
        }
        
        return enemyObj;
    }
    
    public List<GameObject> SpawnEnemiesOnAllLanes()
    {
        List<GameObject> spawnedEnemies = new List<GameObject>();
        
        if (pathwayTransformsByLane == null) return spawnedEnemies;
        
        for (int laneId = 0; laneId < pathwayTransformsByLane.Count; laneId++)
        {
            GameObject enemy = SpawnEnemyOnLane(laneId);
            if (enemy != null)
            {
                spawnedEnemies.Add(enemy);
            }
        }
        
        Debug.Log($"[EnemyManager] Spawned {spawnedEnemies.Count} enemies across {pathwayTransformsByLane.Count} lanes. Total active enemies: {GetActiveEnemyCount()}");
        return spawnedEnemies;
    }

    void OnEnable()
    {
        // Register for PathwayTransformsEvent
        pathwayTransformsBinding = new EventBinding<PathwayTransformsEvent>(OnPathwayTransformsReceived);
        EventBus<PathwayTransformsEvent>.Register(pathwayTransformsBinding);

        environmentGeneratedBinding = new EventBinding<EnvironmentGeneratedEvent>(Initialize);
        EventBus<EnvironmentGeneratedEvent>.Register(environmentGeneratedBinding);
    }

    void OnDisable()
    {
        EventBus<PathwayTransformsEvent>.Deregister(pathwayTransformsBinding);
        EventBus<EnvironmentGeneratedEvent>.Deregister(environmentGeneratedBinding);
    }
    
    private void OnPathwayTransformsReceived(PathwayTransformsEvent evt)
    {
        pathwayTransformsByLane = evt.pathwayTransformsByLane;
        
        //Debug.Log($"[EnemyManager] Received pathway transforms for {pathwayTransformsByLane?.Count ?? 0} lanes");
        
        // Log details about each lane
        if (pathwayTransformsByLane != null)
        {
            for (int laneId = 0; laneId < pathwayTransformsByLane.Count; laneId++)
            {
                var transforms = pathwayTransformsByLane[laneId];
                string pathInfo = transforms.Count > 0 ? 
                    $"(Edge Spawner + {transforms.Count - 1} pathway transforms)" : 
                    "(empty)";
                //Debug.Log($"[EnemyManager] Lane {laneId} has {transforms.Count} total transforms {pathInfo}");
            }
        }
        
        // Spawn test enemy now that we have pathway data
        if (testEnemyPrefab != null && pathwayTransformsByLane != null && pathwayTransformsByLane.Count > 0)
        {
            for (int i = 0; i < 4; i++)
            {
                //SpawnEnemyOnLane(0);
                SpawnEnemiesOnAllLanes();
            }
        }
    }
    
    /// <summary>
    /// Public method to request pathway transforms from HexEnvironmentManager
    /// </summary>
    public void RequestPathwayTransforms()
    {
        var hexManager = FindFirstObjectByType<HexEnvironmentManager>();
        if (hexManager != null)
        {
            hexManager.RequestPathwayTransforms();
        }
        else
        {
            //Debug.LogWarning("[EnemyManager] No HexEnvironmentManager found in scene!");
        }
    }
    
    /// <summary>
    /// Get pathway transforms for a specific lane
    /// </summary>
    public List<Transform> GetPathwayTransformsForLane(int laneId)
    {
        if (pathwayTransformsByLane != null && laneId >= 0 && laneId < pathwayTransformsByLane.Count)
        {
            return pathwayTransformsByLane[laneId];
        }
        
        //Debug.LogWarning($"[EnemyManager] No pathway transforms available for lane {laneId}");
        return null;
    }
    
    /// <summary>
    /// Get all pathway transforms organized by lane
    /// </summary>
    public List<List<Transform>> GetAllPathwayTransforms()
    {
        return pathwayTransformsByLane;
    }
    
    /// <summary>
    /// Clear all spawned enemies when environment is regenerated
    /// </summary>
    public void ClearAllEnemies()
    {
        // Destroy all spawned enemies
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (spawnedEnemies[i] != null)
            {
                Destroy(spawnedEnemies[i]);
            }
        }
        
        // Clear the list
        spawnedEnemies.Clear();
        
        Debug.Log("[EnemyManager] Cleared all spawned enemies");
    }
    
    /// <summary>
    /// Remove a specific enemy from the tracking list (called when enemy is destroyed naturally)
    /// </summary>
    public void RemoveEnemyFromList(GameObject enemy)
    {
        spawnedEnemies.Remove(enemy);
    }
    
    /// <summary>
    /// Get the current number of active enemies
    /// </summary>
    public int GetActiveEnemyCount()
    {
        // Clean up null references first
        spawnedEnemies.RemoveAll(enemy => enemy == null);
        return spawnedEnemies.Count;
    }
    
    /// <summary>
    /// Get all currently spawned enemies
    /// </summary>
    public List<GameObject> GetAllSpawnedEnemies()
    {
        // Clean up null references first
        spawnedEnemies.RemoveAll(enemy => enemy == null);
        return new List<GameObject>(spawnedEnemies);
    }

    // Update is called once per frame
    void Update()
    {
        // Periodically clean up null references from destroyed enemies
        if (Time.frameCount % 60 == 0) // Check every 60 frames (~1 second at 60fps)
        {
            int beforeCount = spawnedEnemies.Count;
            spawnedEnemies.RemoveAll(enemy => enemy == null);
            int afterCount = spawnedEnemies.Count;
            
            if (beforeCount != afterCount)
            {
                Debug.Log($"[EnemyManager] Cleaned up {beforeCount - afterCount} destroyed enemies. Active enemies: {afterCount}");
            }
        }
    }
    
    #region Gizmos
    
    void OnDrawGizmos()
    {
        if (!showPathwayGizmos || pathwayTransformsByLane == null) return;
        
        for (int laneId = 0; laneId < pathwayTransformsByLane.Count; laneId++)
        {
            var pathTransforms = pathwayTransformsByLane[laneId];
            if (pathTransforms == null || pathTransforms.Count == 0) continue;
            
            // Get color for this lane
            Color laneColor = GetLaneColor(laneId);
            
            // Draw pathway points and lines
            for (int i = 0; i < pathTransforms.Count; i++)
            {
                var transform = pathTransforms[i];
                if (transform == null) continue;
                
                Vector3 position = transform.position;
                
                // Draw pathway points
                if (showPathwayPoints)
                {
                    if (i == 0) // Edge spawner
                    {
                        Gizmos.color = edgeSpawnerColor;
                        Gizmos.DrawSphere(position, edgeSpawnerSize);
                        
                        // Draw spawner label
                        #if UNITY_EDITOR
                        UnityEditor.Handles.color = edgeSpawnerColor;
                        UnityEditor.Handles.Label(position + Vector3.up * 0.8f, $"Spawn {laneId}");
                        #endif
                    }
                    else // Pathway points
                    {
                        Gizmos.color = laneColor;
                        Gizmos.DrawSphere(position, gizmoSphereSize);
                    }
                }
                
                // Draw lines between consecutive points
                if (showPathwayLines && i > 0)
                {
                    var previousTransform = pathTransforms[i - 1];
                    if (previousTransform != null)
                    {
                        Vector3 startPos = previousTransform.position;
                        Vector3 endPos = position;
                        
                        // Draw the line
                        Gizmos.color = laneColor;
                        Gizmos.DrawLine(startPos, endPos);
                        
                        // Draw direction arrow (optional)
                        DrawDirectionArrow(startPos, endPos, laneColor);
                    }
                }
            }
            
            // Draw lane label at the center of the path
            if (pathTransforms.Count > 1)
            {
                #if UNITY_EDITOR
                Vector3 centerPosition = GetPathCenterPosition(pathTransforms);
                UnityEditor.Handles.color = laneColor;
                UnityEditor.Handles.Label(centerPosition + Vector3.up * 1.2f, $"Lane {laneId}");
                #endif
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw more detailed gizmos when selected
        if (!showPathwayGizmos || pathwayTransformsByLane == null) return;
        
        for (int laneId = 0; laneId < pathwayTransformsByLane.Count; laneId++)
        {
            var pathTransforms = pathwayTransformsByLane[laneId];
            if (pathTransforms == null || pathTransforms.Count == 0) continue;
            
            Color laneColor = GetLaneColor(laneId);
            
            // Draw pathway indices when selected
            for (int i = 0; i < pathTransforms.Count; i++)
            {
                var transform = pathTransforms[i];
                if (transform == null) continue;
                
                #if UNITY_EDITOR
                Vector3 position = transform.position;
                UnityEditor.Handles.color = laneColor;
                UnityEditor.Handles.Label(position + Vector3.up * 0.5f, $"{i}");
                #endif
            }
        }
    }
    
    private Color GetLaneColor(int laneId)
    {
        if (laneColors != null && laneColors.Length > 0)
        {
            return laneColors[laneId % laneColors.Length];
        }
        
        // Fallback to HSV color generation
        return Color.HSVToRGB((float)laneId / Mathf.Max(1, pathwayTransformsByLane.Count), 0.8f, 1f);
    }
    
    private void DrawDirectionArrow(Vector3 start, Vector3 end, Color color)
    {
        Vector3 direction = (end - start).normalized;
        Vector3 arrowHead = end - direction * 0.2f;
        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized * 0.1f;
        Vector3 up = Vector3.up * 0.1f;
        
        Gizmos.color = color;
        
        // Draw arrow wings
        Gizmos.DrawLine(end, arrowHead + right);
        Gizmos.DrawLine(end, arrowHead - right);
        Gizmos.DrawLine(end, arrowHead + up);
        Gizmos.DrawLine(end, arrowHead - up);
    }
    
    private Vector3 GetPathCenterPosition(List<Transform> pathTransforms)
    {
        Vector3 center = Vector3.zero;
        int validCount = 0;
        
        foreach (var transform in pathTransforms)
        {
            if (transform != null)
            {
                center += transform.position;
                validCount++;
            }
        }
        
        return validCount > 0 ? center / validCount : Vector3.zero;
    }
    
    #endregion
}
