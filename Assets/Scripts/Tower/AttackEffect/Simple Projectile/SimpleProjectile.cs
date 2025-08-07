using UnityEngine;

public class SimpleProjectile : MonoBehaviour, ITarget
{
    //VARIABLES
    public Transform target;
    [SerializeField] private float damage = 100F;
    [SerializeField] private float speed = 10.0F;
    [SerializeField] private float collisionRadius = 0.5f;
    [SerializeField] private float yOffset = 0.3f; // Height offset for targeting
    [SerializeField] private LayerMask enemyLayerMask = -1; // All layers by default
    private bool attackTravel = false;
    private Vector3 targetPosition;
    private Vector3 moveDirection;

    void Start()
    {
        //transform.localScale = Vector3.zero;
    }


    void Update()
    {
        if (attackTravel && target != null)
        {
            MoveTowardsTarget();
        } else if (attackTravel && target == null)
        {
            DestroyProjectile();
        }
        
        if (attackTravel)
        {
            CheckForEnemyCollisions();
        }
    }
    
    private void MoveTowardsTarget()
    {
        // Move towards target position
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * GameTime.DeltaTime);

        // Check if we've reached the target
        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            // Reached target without hitting enemy
            if (attackTravel)
            {
                DestroyProjectile();
            }
        }
    }
    
    private void CheckForEnemyCollisions()
    {
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, collisionRadius, enemyLayerMask);
        
        foreach (Collider enemyCollider in enemiesInRange)
        {
            if (enemyCollider.CompareTag("Enemy"))
            {
                Enemy enemy = enemyCollider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    OnHitEnemy(enemy);
                    return; 
                }
            }
        }
    }
    
    private void OnHitEnemy(Enemy enemy)
    {
        attackTravel = false;
        
        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
        }
        
        DestroyProjectile();
    }
    
    private void DestroyProjectile()
    {     
        Destroy(gameObject); 
    }

    public void SetTarget(Transform intarget)
    {
        target = intarget;
        attackTravel = true;

        if (target != null)
        {
            targetPosition = target.position + Vector3.up * yOffset;
            moveDirection = (targetPosition - transform.position).normalized;
            
        }
    }
}
