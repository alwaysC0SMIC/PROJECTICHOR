using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    private Enemy enemy;
    private float attackDamage = 10f;
    [SerializeField] private float attackRange = 20f;
    private float attackRate = 1f;
    private float lastAttackTime = 0f;

    private bool isAttacking = false;
    private bool canAttack = false;
    private bool movingToAttackPosition = false;
    private Vector3 originalPosition;
    private Vector3 attackPosition;
    private float moveSpeed = 5f;
    [SerializeField] private float targetOffset = 0.25f;
    private Transform target;
    private int originalWaypointIndex;

    public void Initialize(SO_Enemy enemydata, Enemy inenemy)
    {
        enemy = inenemy;
        attackDamage = enemydata != null ? enemydata.damage : attackDamage;
        attackRate = enemydata != null ? enemydata.attackRate : attackRate;
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
        else if (isAttacking && target != null)
        {
            AttackSequence();
        }
    }

    public bool LookForTowers()
    {
        Collider[] targetsInRange = DetectTargetsInRange();
        foreach (var intarget in targetsInRange)
        {
            Tower tower = intarget.GetComponent<Tower>();
            if (tower != null && tower.currentHealth > 0)
            {
                target = intarget.transform;
                return true;
            }
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
        attackPosition = targetPos + directionToTarget * targetOffset;
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
        if (canAttack && Time.time >= lastAttackTime + attackRate)
        {
            PerformAttack();
            lastAttackTime = Time.time;
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
        if (tower != null && tower.currentHealth > 0)
        {
            // Deal damage to tower
            tower.currentHealth -= attackDamage;
            tower.currentHealth = Mathf.Max(0, tower.currentHealth);
            
            Debug.Log($"Enemy attacked tower! Tower health: {tower.currentHealth}");
            
            // Add attack effects here if needed
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

    public Collider[] DetectTargetsInRange()
    {
        return Physics.OverlapSphere(transform.position, attackRange, LayerMask.GetMask("Tower"));
    }

    void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw attack position if attacking
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
    }
}
