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

    public void Initialize(Enemy inenemy, List<Transform> inwaypoints)
    {
        enemy = inenemy;
        speed = inenemy.enemyData.speed;
        waypoints.Clear();
        waypoints.AddRange(inwaypoints);
        moveNormally = true;
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
                }
                // If target found, enemy will handle state change and stop normal movement
            }
            else
            {
                currentWP++;
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
    }
}
