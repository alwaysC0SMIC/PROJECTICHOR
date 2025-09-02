using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    private Enemy enemy;
    private float attackDamage = 10f;
    [SerializeField] private float attackRange = 2f;
    private float attackRate = 1f;
    private float lastAttackTime = 0f;

    private bool isAttacking = false;
    private bool canAttack = false;
    private bool movingToAttackPosition = false;
    private bool isAttackingCentreHub = false;
    private bool movingToCentreHubPosition = false;
    private Vector3 originalPosition;
    private Vector3 attackPosition;
    private Vector3 centreHubAttackPosition;
    private float moveSpeed = 5f;
    [SerializeField] private float targetOffset = 0.25f;
    [SerializeField] private float angleDeviation = 30f; // Maximum angle deviation in degrees
    private Transform target;
    private int originalWaypointIndex;

    public void Initialize(Enemy inenemy)
    {
        enemy = inenemy;
        moveSpeed = inenemy.enemyData.speed;
        attackDamage = inenemy.enemyData.damage;
        attackRate = inenemy.enemyData.attackRate;

        //FOR NOW
        attackRange = 2F;
    }

    void Update()
    {
        HandleAttackSequence();
    }

    private void HandleAttackSequence()
    {
        if (movingToAttackPosition)
        {
            MoveToAttackPosition();
        }
        else if (movingToCentreHubPosition)
        {
            MoveToCentreHubPosition();
        }
        else if (isAttacking && target != null)
        {
            AttackSequence();
        }
        else if (isAttackingCentreHub)
        {
            CentreHubAttackSequence();
        }
    }

    public bool LookForTowers()
    {
        Collider[] targetsInRange = DetectTargetsInRange();
        Transform nearestTower = null;
        float nearestDistance = float.MaxValue;
        
        foreach (var intarget in targetsInRange)
        {
            Tower tower = intarget.GetComponent<Tower>();
            if (tower != null && tower.currentHealth > 0)
            {
                float distance = Vector3.Distance(transform.position, intarget.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTower = intarget.transform;
                }
            }
        }

        if (nearestTower != null)
        {
            target = nearestTower;
            return true;
        }

        target = null;
        return false;
    }

    public void BeginAttackTower()
    {
        if (target == null) return;

        // Store original position and waypoint for returning later
        originalPosition = transform.position;
        originalWaypointIndex = enemy.GetComponent<FollowWP>().currentWP;
        
        // Calculate attack position (offset from tower, ignore Y axis)
        Vector3 targetPos = new Vector3(target.position.x, originalPosition.y, target.position.z);
        Vector3 directionToTarget = (new Vector3(originalPosition.x, 0, originalPosition.z) - new Vector3(targetPos.x, 0, targetPos.z)).normalized;
        
        // Add randomization to the attack position (angle only, not distance)
        float randomAngle = Random.Range(-angleDeviation, angleDeviation);
        Vector3 randomizedDirection = Quaternion.AngleAxis(randomAngle, Vector3.up) * directionToTarget;
        
        attackPosition = targetPos + randomizedDirection * targetOffset;
        attackPosition.y = originalPosition.y; // Keep original Y position
        
        movingToAttackPosition = true;
        isAttacking = false;
        canAttack = false;
    }

    private void MoveToAttackPosition()
    {
        if (!movingToAttackPosition || target == null) return;

        // Move towards attack position (ignore Y axis)
        Vector3 currentPos = transform.position;
        Vector3 targetPos = new Vector3(attackPosition.x, currentPos.y, attackPosition.z);
        
        transform.position = Vector3.MoveTowards(currentPos, targetPos, moveSpeed * GameTime.DeltaTime);
        
        // Look at the tower
        Vector3 lookDirection = (new Vector3(target.position.x, transform.position.y, target.position.z) - transform.position).normalized;
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
        
        // Check if reached attack position
        if (Vector3.Distance(new Vector3(currentPos.x, 0, currentPos.z), new Vector3(targetPos.x, 0, targetPos.z)) < 0.1f)
        {
            movingToAttackPosition = false;
            isAttacking = true;
            canAttack = true;
            enemy.StartAttacking();
        }
    }

    private void MoveToCentreHubPosition()
    {
        if (!movingToCentreHubPosition) return;

        // Move towards center hub attack position (ignore Y axis)
        Vector3 currentPos = transform.position;
        Vector3 targetPos = new Vector3(centreHubAttackPosition.x, currentPos.y, centreHubAttackPosition.z);
        
        transform.position = Vector3.MoveTowards(currentPos, targetPos, moveSpeed * GameTime.DeltaTime);
        
        // Look at the center position
        Vector3 lookDirection = (new Vector3(0, transform.position.y, 0) - transform.position).normalized;
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
        
        // Check if reached center hub attack position
        if (Vector3.Distance(new Vector3(currentPos.x, 0, currentPos.z), new Vector3(targetPos.x, 0, targetPos.z)) < 0.1f)
        {
            movingToCentreHubPosition = false;
            isAttackingCentreHub = true;
            canAttack = true;
            Debug.Log("Enemy reached center hub attack position and began attacking!");
        }
    }

    private void AttackSequence()
    {
        // Check if tower is still alive
        if (target == null || !IsTowerAlive())
        {
            // Tower is dead or destroyed, return to path
            enemy.ReturnToPath();
            return;
        }

        // Perform attack if ready
        if (canAttack && GameTime.TotalTime >= lastAttackTime + attackRate)
        {
            PerformAttack();
            lastAttackTime = GameTime.TotalTime;
        }
    }

    private bool IsTowerAlive()
    {
        if (target == null) return false;
        
        Tower tower = target.GetComponent<Tower>();
        return tower != null && tower.currentHealth > 0;
    }

    private void PerformAttack()
    {
        if (target == null) return;

        Tower tower = target.GetComponent<Tower>();
        if (tower != null)
        {
            tower.TakeDamage(attackDamage);
        }
    }

    public void StopAttacking()
    {
        isAttacking = false;
        movingToAttackPosition = false;
        canAttack = false;
        target = null;
        
        // Return to original waypoint
        FollowWP followWP = enemy.GetComponent<FollowWP>();
        if (followWP != null)
        {
            followWP.ReturnToWaypoint(originalWaypointIndex);
        }
    }

    public void BeginAttackCentreHub()
    {
        // Store original position for reference
        originalPosition = transform.position;
        
        // Use world center (0,0,0) as the target position
        Vector3 centreWorldPosition = Vector3.zero;
        Vector3 targetPos = new Vector3(centreWorldPosition.x, originalPosition.y, centreWorldPosition.z);
        
        // Calculate direction from center to enemy position (ignore Y axis)
        Vector3 directionFromCenter = (new Vector3(originalPosition.x, 0, originalPosition.z) - new Vector3(targetPos.x, 0, targetPos.z)).normalized;
        
        // Add randomization to the attack position (angle only, not distance)
        float randomAngle = Random.Range(-angleDeviation, angleDeviation);
        Vector3 randomizedDirection = Quaternion.AngleAxis(randomAngle, Vector3.up) * directionFromCenter;
        
        // Position enemy at offset distance from center with randomized angle
        centreHubAttackPosition = targetPos + randomizedDirection * targetOffset;
        centreHubAttackPosition.y = originalPosition.y; // Keep original Y position
        
        movingToCentreHubPosition = true;
        isAttackingCentreHub = false;
        canAttack = false;
        
        Debug.Log("Enemy moving to center hub attack position!");
    }

    public void StopAttackingCentreHub()
    {
        isAttackingCentreHub = false;
        movingToCentreHubPosition = false;
        canAttack = false;
        //Debug.Log("Enemy stopped attacking center hub!");
    }

    private void CentreHubAttackSequence()
    {
        // Perform attack if ready
        if (canAttack && GameTime.TotalTime >= lastAttackTime + attackRate)
        {
            PerformCentreHubAttack();
            lastAttackTime = GameTime.TotalTime;
        }
    }

    private void PerformCentreHubAttack()
    {
        EventBus<CentreTowerAttackEvent>.Raise(new CentreTowerAttackEvent { damageAmount = attackDamage });
    }

    public Collider[] DetectTargetsInRange()
    {
        return Physics.OverlapSphere(transform.position, attackRange, LayerMask.GetMask("Tower"));
    }

    void OnDrawGizmosSelected()
    {
        // Draw attack search range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw attack position if attacking tower
        if (movingToAttackPosition || isAttacking)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(attackPosition, 0.5f);
            
            if (target != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
        
        // Draw center hub attack position if attacking center hub
        if (movingToCentreHubPosition || isAttackingCentreHub)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(centreHubAttackPosition, 0.5f);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, Vector3.zero);
        }
    }
    
    void OnDrawGizmos()
    {
        // Always draw attack search range (even when not selected) for better visibility
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f); // Semi-transparent red
        Gizmos.DrawSphere(transform.position, attackRange);
        
        // Draw wireframe for better definition
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // More opaque red
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
