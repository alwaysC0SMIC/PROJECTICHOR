using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowWP : MonoBehaviour
{
    //VARIABLES
    public bool moveNormally = false;
    [SerializeField] public List<Transform> waypoints = new List<Transform>();
    public int currentWP = 0;
    private Enemy enemy;
    [SerializeField] float speed = 10.0f;
    [SerializeField] float rotSpeed = 10.0f;
    [SerializeField] public float waypointReachDistance = 0.5f;
    private bool isMovingToFinalWaypoint = false;
    private Vector3 halfwayToFinal;
    private bool hasCheckedHalfway = false; // Track if we've checked for towers at halfway point

    public void Initialize(Enemy inenemy, List<Transform> inwaypoints)
    {
        enemy = inenemy;
        speed = inenemy.enemyData.speed;
        waypoints.Clear();
        waypoints.AddRange(inwaypoints);
        moveNormally = true;
        hasCheckedHalfway = false; // Initialize halfway check flag
    }

    void Update()
    {
        if (moveNormally)
        {
            Traverse();
        } 
    }

    public void Traverse()
    {
        // Check if we're at the final waypoint
        bool isAtFinalWaypoint = (currentWP == waypoints.Count - 1);
        
        Vector3 targetPosition;
        
        if (isAtFinalWaypoint && !isMovingToFinalWaypoint)
        {
            // Calculate halfway point to final waypoint
            Vector3 currentPos = transform.position;
            Vector3 finalPos = waypoints[currentWP].transform.position;
            halfwayToFinal = Vector3.Lerp(currentPos, finalPos, 0.5f);
            isMovingToFinalWaypoint = true;
            targetPosition = halfwayToFinal;
        }
        else if (isAtFinalWaypoint && isMovingToFinalWaypoint)
        {
            targetPosition = halfwayToFinal;
        }
        else
        {
            targetPosition = waypoints[currentWP].transform.position;
        }

        // Check for halfway point to current waypoint (only for non-final waypoints)
        if (!isAtFinalWaypoint && currentWP > 0 && !hasCheckedHalfway)
        {
            Vector3 previousPos = currentWP > 0 ? waypoints[currentWP - 1].transform.position : transform.position;
            Vector3 currentWaypointPos = waypoints[currentWP].transform.position;
            Vector3 halfwayPoint = Vector3.Lerp(previousPos, currentWaypointPos, 0.5f);
            
            // Check if we've reached the halfway point
            float distanceToHalfway = Vector3.Distance(transform.position, halfwayPoint);
            float totalDistance = Vector3.Distance(previousPos, currentWaypointPos);
            
            // If we're past the halfway point (closer to target than halfway)
            if (distanceToHalfway < totalDistance * 0.25f) // Use 25% threshold for better detection
            {
                hasCheckedHalfway = true;
                
                // Check for targets at halfway point
                bool foundTarget = false;
                enemy.LookForTarget(out foundTarget);
                
                // If target found, enemy will handle state change and stop normal movement
                if (foundTarget)
                {
                    return;
                }
            }
        }

        if (Vector3.Distance(transform.position, targetPosition) < waypointReachDistance)
        {
            if (isAtFinalWaypoint && isMovingToFinalWaypoint)
            {
                // Reached halfway to final waypoint - start attacking center hub
                moveNormally = false;
                enemy.StartAttackingCentreHub();
                return;
            }
            
            // Always check for targets when reaching a waypoint (except the first one)
            if (currentWP > 0)
            {
                //LOOK FOR TARGET BEFORE CONTINUING
                bool foundTarget = false;
                enemy.LookForTarget(out foundTarget);

                if (!foundTarget)
                {
                    currentWP++;
                    hasCheckedHalfway = false; // Reset halfway check for next waypoint
                }
                // If target found, enemy will handle state change and stop normal movement
            }
            else
            {
                currentWP++;
                hasCheckedHalfway = false; // Reset halfway check for next waypoint
            }
            
        }

        if (currentWP >= waypoints.Count)
        {
            //currentWP = 0;

            //REACHED CENTRE - STOP MOVING
            moveNormally = false;
        }

        Quaternion lookAtWP = Quaternion.LookRotation(targetPosition - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookAtWP, GameTime.DeltaTime * rotSpeed);
        transform.Translate(0.0f, 0.0f, speed * GameTime.DeltaTime);
    }

    public void ReturnToWaypoint(int waypointIndex)
    {
        currentWP = waypointIndex;
        moveNormally = true;
        isMovingToFinalWaypoint = false; // Reset halfway tracking
        hasCheckedHalfway = false; // Reset halfway check for target detection
    }

    // Method to get path progress for tower targeting priority
    public float GetPathProgress()
    {
        if (waypoints.Count == 0) return 0f;
        
        // Calculate progress as a percentage (0-1)
        float progress = (float)currentWP / (float)(waypoints.Count - 1);
        
        // Add fractional progress based on distance to current waypoint
        if (currentWP < waypoints.Count)
        {
            Vector3 targetPosition = isMovingToFinalWaypoint ? halfwayToFinal : waypoints[currentWP].transform.position;
            
            if (currentWP > 0)
            {
                Vector3 previousPosition = waypoints[currentWP - 1].transform.position;
                float segmentLength = Vector3.Distance(previousPosition, targetPosition);
                float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
                float segmentProgress = Mathf.Clamp01(1f - (distanceToTarget / segmentLength));
                
                progress = ((float)(currentWP - 1) + segmentProgress) / (float)(waypoints.Count - 1);
            }
        }
        
        return Mathf.Clamp01(progress);
    }
}
