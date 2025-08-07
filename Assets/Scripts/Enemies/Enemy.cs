using UnityEngine;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{
    [Header("Waypoint Movement")]
    public List<Transform> waypoints;
    private int currentWP = 0;
    
    [Header("Movement Settings")]
    public float speed = 5.0f;
    public float rotSpeed = 10.0f;
    public bool allowMovement = true;
    public float waypointReachDistance = 0.5f;

    
    [Header("Randomization")]
    [SerializeField] private float randomizationRadius = 0.5f;
    [SerializeField] private bool useRandomization = false;
    private Vector3 randomizedTarget;
    
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // State tracking
    private bool isMoving = false;
    private bool hasReachedEnd = false;
    
    void Start()
    {
        // Initialize randomization if enabled
        if (useRandomization)
        {
            randomizationRadius = Random.Range(0.2f, randomizationRadius);
        }
        
        // Set initial target if waypoints are available
        if (waypoints != null && waypoints.Count > 0)
        {
            SetRandomizedTarget();
            isMoving = true;
        }
    }
    
    void Update()
    {
        if (!allowMovement || !isMoving || hasReachedEnd) return;
        
        if (waypoints == null || waypoints.Count == 0)
        {
            if (showDebugInfo)
                Debug.LogWarning("[Enemy] No waypoints assigned!");
            return;
        }
        
        // Get current target position
        Vector3 target = useRandomization ? randomizedTarget : waypoints[currentWP].position;
        
        // Check if we've reached the current waypoint
        if (Vector3.Distance(transform.position, target) < waypointReachDistance)
        {
            OnWaypointReached();
        }
        
        // Move towards target with avoidance
        MoveTowards(target);
    }
    
    private void OnWaypointReached()
    {
        if (showDebugInfo)
            Debug.Log($"[Enemy] Reached waypoint {currentWP}");
        
        // Move to next waypoint
        currentWP++;
        
        // Check if we've reached the end
        if (currentWP >= waypoints.Count)
        {
            OnPathCompleted();
            return;
        }
        
        // Set new randomized target for next waypoint
        if (useRandomization)
        {
            SetRandomizedTarget();
        }
    }
    
    private void OnPathCompleted()
    {
        hasReachedEnd = true;
        isMoving = false;
        
        if (showDebugInfo)
            Debug.Log("[Enemy] Completed pathway!");
        
        // You can add custom logic here for what happens when enemy reaches the end
        // For example: damage player, destroy enemy, etc.
        OnReachedDestination();
    }
    
    private void MoveTowards(Vector3 target)
    {
        // Calculate direction to target
        Vector3 direction = (target - transform.position).normalized;
        
        // Rotate towards final direction
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(transform.rotation, lookRotation, GameTime.DeltaTime * rotSpeed);
        }
        
        // Move forward
        transform.Translate(0.0f, 0.0f, speed * GameTime.DeltaTime);
    }
    
    private void SetRandomizedTarget()
    {
        if (!useRandomization || currentWP >= waypoints.Count) return;
        
        // Add a random offset within the specified radius to the waypoint position
        Vector3 randomOffset = new Vector3(
            Random.Range(-randomizationRadius, randomizationRadius),
            0, // Keep the offset on the horizontal plane  
            Random.Range(-randomizationRadius, randomizationRadius)
        );
        
        randomizedTarget = waypoints[currentWP].position + randomOffset;
    }
    
    /// <summary>
    /// Initialize the enemy with a list of waypoint transforms
    /// </summary>
    public void InitializeWithWaypoints(List<Transform> waypointList)
    {
        waypoints = waypointList;
        currentWP = 0;
        hasReachedEnd = false;
        
        if (waypoints != null && waypoints.Count > 0)
        {
            isMoving = true;
            SetRandomizedTarget();
            
            if (showDebugInfo)
                Debug.Log($"[Enemy] Initialized with {waypoints.Count} waypoints");
        }
        else
        {
            Debug.LogWarning("[Enemy] Initialized with empty or null waypoint list!");
        }
    }
    
    /// <summary>
    /// Stop the enemy movement
    /// </summary>
    public void StopMovement()
    {
        allowMovement = false;
        isMoving = false;
    }
    
    /// <summary>
    /// Resume enemy movement
    /// </summary>
    public void ResumeMovement()
    {
        allowMovement = true;
        if (waypoints != null && waypoints.Count > 0 && !hasReachedEnd)
        {
            isMoving = true;
        }
    }
    
    /// <summary>
    /// Reset the enemy to the first waypoint
    /// </summary>
    public void ResetToStart()
    {
        currentWP = 0;
        hasReachedEnd = false;
        isMoving = waypoints != null && waypoints.Count > 0;
        SetRandomizedTarget();
    }
    
    /// <summary>
    /// Get current progress along the path (0 to 1)
    /// </summary>
    public float GetPathProgress()
    {
        if (waypoints == null || waypoints.Count == 0) return 0f;
        return (float)currentWP / waypoints.Count;
    }
    
    protected virtual void OnReachedDestination()
    {
        // Default behavior - destroy the enemy
        // You can override this for custom end-of-path behavior
        Destroy(gameObject);
    }
    
    #region Debug Gizmos
    
    void OnDrawGizmos()
    {
        if (!showDebugInfo || waypoints == null || waypoints.Count == 0) return;
        
        // Draw current target
        Vector3 target = useRandomization ? randomizedTarget : waypoints[currentWP].position;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(target, waypointReachDistance);
        
        // Draw line to current target
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, target);
        
        // Draw randomization radius if enabled
        if (useRandomization && currentWP < waypoints.Count)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(waypoints[currentWP].position, randomizationRadius);
        }
        
    }
    
    void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Count == 0) return;
        
        // Draw all waypoints and connections
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null) continue;
            
            // Color coding: current = red, completed = green, upcoming = white
            if (i == currentWP)
                Gizmos.color = Color.red;
            else if (i < currentWP)
                Gizmos.color = Color.green;
            else
                Gizmos.color = Color.white;
            
            Gizmos.DrawSphere(waypoints[i].position, 0.3f);
            
            // Draw connections
            if (i < waypoints.Count - 1 && waypoints[i + 1] != null)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }
    }
    
    #endregion
}
