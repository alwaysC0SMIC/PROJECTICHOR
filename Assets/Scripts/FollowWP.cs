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

    public void Initialize(Enemy inenemy, List<Transform> inwaypoints)
    {
        enemy = inenemy;
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
        if (Vector3.Distance(transform.position, waypoints[currentWP].transform.position) < waypointReachDistance)
        {
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

        if (currentWP >= waypoints.Count - 1)
        {
            //currentWP = 0;

            //AIM FOR CENTRE
            moveNormally = false;
        }

        Quaternion lookAtWP = Quaternion.LookRotation(waypoints[currentWP].transform.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookAtWP, GameTime.DeltaTime * rotSpeed);
        transform.Translate(0.0f, 0.0f, speed * GameTime.DeltaTime);
    }

    public void ReturnToWaypoint(int waypointIndex)
    {
        currentWP = waypointIndex;
        moveNormally = true;
    }
}
