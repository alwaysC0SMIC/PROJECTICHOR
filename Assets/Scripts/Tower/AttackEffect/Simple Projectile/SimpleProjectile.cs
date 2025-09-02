using UnityEngine;
using System.Collections.Generic;

public class SimpleProjectile : MonoBehaviour, ITarget
{
    // VARIABLES
    public Transform target;

    [Header("FX")]
    [SerializeField] private GameObject explosionFX;

    [Header("Combat")]
    [SerializeField] private float damage = 100f;

    [Header("Motion")]
    [SerializeField] private float speed = 10.0f;
    [SerializeField] private float yOffset = 0.3f; // Height offset for targeting
    [SerializeField] private float arrivalThreshold = 0.05f; // How close is "arrived"

    [Header("Collision")]
    [SerializeField] private float collisionRadius = 0.5f;
    [SerializeField] private LayerMask enemyLayerMask = -1; // All layers by default

    [Header("Flags")]
    public bool attackTravel = false;

    // Internals
    private Vector3 targetPosition;
    private Vector3 moveDirection;

    // Track enemies we've already damaged so we don't double-hit
    private readonly HashSet<EnemyHealth> _alreadyDamaged = new HashSet<EnemyHealth>();

    void Update()
    {
        if (!attackTravel)
            return;

        // If destination (target) is destroyed or missing -> destroy projectile
        if (target == null || !target.gameObject)
        {
            DestroyProjectile();
            return;
        }

        // Continuously update target position (so we chase a moving target)
        targetPosition = target.position + Vector3.up * yOffset;

        // Move towards the current target position using your custom time
        MoveTowardsTarget();

        // While traveling, damage any enemies we overlap
        CheckForEnemyCollisions();
    }

    private void MoveTowardsTarget()
    {
        // Move towards target position
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * GameTime.DeltaTime
        );

        // Face travel direction if desired (optional)
        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        }

        // Check if we've reached (or very close to) the destination
        if (Vector3.Distance(transform.position, targetPosition) <= arrivalThreshold)
        {
            DestroyProjectile();
        }
    }

    private void CheckForEnemyCollisions()
    {
        // Use OverlapSphere to check for enemies within collision radius
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, collisionRadius, enemyLayerMask);

        for (int i = 0; i < enemiesInRange.Length; i++)
        {
            Collider enemyCollider = enemiesInRange[i];

            // You can keep the tag check if your project relies on it
            if (!enemyCollider.CompareTag("Enemy"))
                continue;

            EnemyHealth enemyHealth = enemyCollider.GetComponent<EnemyHealth>();
            if (enemyHealth != null && !_alreadyDamaged.Contains(enemyHealth))
            {
                enemyHealth.TakeDamage(damage);
                _alreadyDamaged.Add(enemyHealth);
                // Note: DO NOT destroy or stop traveling — we keep going and can hit others too
            }
        }
    }

    private void DestroyProjectile()
    {
        if (explosionFX != null)
        {
            Instantiate(explosionFX, transform.position, transform.rotation);
        }
        Destroy(gameObject);
    }

    public void SetTarget(Transform intarget, float damageAmount = 100)
    {
        damage = damageAmount;
        target = intarget;
        attackTravel = true;

        if (target != null)
        {
            // Initialize first target position & direction
            targetPosition = target.position + Vector3.up * yOffset;
            moveDirection = (targetPosition - transform.position).normalized;
        }
        else
        {
            // No valid target -> self-destruct immediately
            DestroyProjectile();
        }
    }

    // Debug gizmos for collision radius
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, collisionRadius);
    }
}
