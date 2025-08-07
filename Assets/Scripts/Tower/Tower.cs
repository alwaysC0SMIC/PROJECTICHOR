using UnityEngine;
using System.Collections;
using DG.Tweening;

public class Tower : MonoBehaviour
{
    //VARIABLES
    [SerializeField] private float health = 100.0f;
    [SerializeField] private float damage = 100.0f;
    [SerializeField] private float attackRange = 5.0f;
    [SerializeField] private float attackRate = 1.0f;
    [SerializeField] private GameObject attackEffectPrefab;
    [SerializeField] Transform attackPoint;
    [SerializeField] private float rotationSpeed = 2.0f; // Speed of rotation in seconds

    private float nextAttackTime = 0f;
    private Sequence rotationSequence;
    private Transform currentTarget;
    private bool isRotating = false;

    //FOR OPTOMIZATION MIGHT MAKE SENSE TO USE ACTUAL COLLIDER TRIGGER AND LOOK FOR ONENTER
    void Update()
    {
        // Find and prioritize targets
        FindAndPrioritizeTarget();
        
        // Continuously rotate towards current target
        if (currentTarget != null && !isRotating)
        {
            FaceTarget(currentTarget);
        }
        
        // Attack if ready
        if (Time.time >= nextAttackTime && currentTarget != null)
        {
            Attack(currentTarget);
            nextAttackTime = Time.time + attackRate;
        }
    }
    
    private void FindAndPrioritizeTarget()
    {
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, attackRange);
        
        Transform bestTarget = null;
        float furthestProgress = -1f;
        
        foreach (Collider enemy in enemiesInRange)
        {
            if (enemy.CompareTag("Enemy"))
            {
                // Get the enemy's path progress
                Enemy enemyScript = enemy.GetComponent<Enemy>();
                if (enemyScript != null)
                {
                    float progress = enemyScript.GetPathProgress(); // Assuming this method exists
                    
                    // Prioritize enemy furthest along the path
                    if (progress > furthestProgress)
                    {
                        furthestProgress = progress;
                        bestTarget = enemy.transform;
                    }
                }
                else
                {
                    // Fallback: if no Enemy script, use distance as priority (closer = further along path assumption)
                    float distanceToTower = Vector3.Distance(transform.position, enemy.transform.position);
                    if (bestTarget == null || distanceToTower < Vector3.Distance(transform.position, bestTarget.position))
                    {
                        bestTarget = enemy.transform;
                    }
                }
            }
        }
        
        currentTarget = bestTarget;
    }

    private void Attack(Transform target)
    {
        if (attackEffectPrefab != null)
        {
            // Make tower face the target
            FaceTarget(target);
            
            Debug.Log($"[Tower] Attacking target {target.name} at position {target.position}");
            Debug.Log($"[Tower] Spawning projectile at {attackPoint.position} with rotation {attackPoint.rotation}");
            
            GameObject projectileObj = Instantiate(attackEffectPrefab, attackPoint.position, attackPoint.rotation);
            ITarget attack = projectileObj.GetComponent<ITarget>();
            projectileObj.transform.localScale = Vector3.one; // Ensure projectile is visible    


            if (attack != null)
            {
                attack.SetTarget(target);
                Debug.Log($"[Tower] Projectile spawned successfully: {projectileObj.name}");
            }
            else
            {
                Debug.LogError($"[Tower] Failed to get ITarget component from {projectileObj.name}");
            }
        }
        else
        {
            Debug.LogError("[Tower] attackEffectPrefab is null!");
        }
    }
    
    private void FaceTarget(Transform target)
    {
        // Calculate direction to target
        Vector3 direction = (target.position - transform.position).normalized;
        
        // Only rotate on Y axis to keep tower upright
        direction.y = 0;
        
        // Create rotation looking towards target
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            
            // Calculate the Y rotation angle
            float targetYRotation = lookRotation.eulerAngles.y;
            float currentYRotation = transform.eulerAngles.y;
            
            // Check if rotation is needed (avoid unnecessary rotations)
            float angleDifference = Mathf.DeltaAngle(currentYRotation, targetYRotation);
            
            if (Mathf.Abs(angleDifference) > 1f) // Only rotate if difference is significant
            {
                // Kill any existing rotation sequence
                if (rotationSequence != null)
                {
                    rotationSequence.Kill();
                }
                
                isRotating = true;
                
                // Smooth rotation using DOTween
                rotationSequence = DOTween.Sequence()
                    .Append(transform.DORotate(new Vector3(0, targetYRotation, 0), rotationSpeed))
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => {
                        isRotating = false;
                    });
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw line to current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            
            // Draw a small sphere at the target position
            Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
        }
    }
    
    void OnDestroy()
    {
        // Clean up DOTween sequence when tower is destroyed
        if (rotationSequence != null)
        {
            rotationSequence.Kill();
        }
    }
}
