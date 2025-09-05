using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
public class Tower : MonoBehaviour
{
    //VARIABLES
    [SerializeField] private SO_Defender defenderData;

    [SerializeField] public float maxhealth = 100.0f;
    [SerializeField] public float currentHealth = 100.0f;
    [SerializeField] public RadialHealth radialHealth;

    [SerializeField] private float damage = 100.0f;

    [SerializeField] private float attackRange = 5.0f;
    [SerializeField] private float attackRate = 1.0f;

    //ATTACK PREFABS
    [SerializeField] private GameObject lanternKeeperAttackEffectPrefab;
    [SerializeField] private GameObject eyeTownEffectPrefab;

    private GameObject currentAttackEffect;
    private Transform currentAttackPoint;



    [SerializeField] Transform lanternKeeperAttackPoint;
    [SerializeField] Transform eyeTowerAttackPoint;

    private float rotationSpeed = 5.0f; // Speed of rotation in seconds

    [SerializeField] private GameObject uiPrefab;

    private float nextAttackTime = 0f;
    private Sequence rotationSequence;
    private Transform currentTarget;
    private bool isRotating = false;
    //private bool isDead = false;

    // Optimization: Track enemies in range
    private List<Transform> enemiesInRange = new List<Transform>();

    [SerializeField] LayerMask enemyLayerMask;

    // Reference to owning HexTile
    private HexTile hexTile;

    [SerializeField] private List<GameObject> towerModels;

    private bool isInitialized = false;

    // Call this to set the owning tile after instantiation
    public void SetOwningTile(HexTile tile)
    {
        hexTile = tile;
    }

    public void Initialize(SO_Defender defender)
    {
        defenderData = defender;



        //DISABLE ALL MODELS
        foreach (GameObject model in towerModels)
        {
            model.SetActive(false);
        }


        switch (defenderData.defenderName)
        {
            case "Lantern Keeper":
                towerModels[0].SetActive(true);
                currentAttackEffect = lanternKeeperAttackEffectPrefab;
                currentAttackPoint = lanternKeeperAttackPoint; // Assuming lantern keeper uses the same attack point
                break;
            case "Eye Tower":
                towerModels[1].SetActive(true);
                currentAttackEffect = eyeTownEffectPrefab;
                currentAttackPoint = eyeTowerAttackPoint; // Assuming eye tower uses the same attack point
                break;

            // case "Arrow Tower":
            //     towerModels[2].SetActive(true);
            //     break;
            // case "Cannon Tower":
            //     towerModels[3].SetActive(true);
            //     break;
            // case "Mage Tower":
            //     towerModels[4].SetActive(true);
            //     break;

            default:
                Debug.LogWarning($"[Tower] No model found for defender name: {defenderData.defenderName}");
                break;
        }

        if (defenderData != null)
        {
            //maxhealth = defenderData; // Example: health scales with cost
            damage = defenderData.defenderDamage; // Example: damage scales with cost

            attackRange = defenderData.range;
            attackRate = defenderData.attackSpeed; // Faster attack speed reduces cooldown

            maxhealth = defenderData.defenderHealth;
            currentHealth = maxhealth;
        }
        else
        {
            Debug.LogWarning("[Tower] Initialize called with null defenderData.");
        }

        isInitialized = true;
    }

    void Start()
    {
        currentHealth = maxhealth;
        radialHealth.Initialize(maxhealth);
    }

    void OnEnable()
    {
        currentHealth = maxhealth;
        radialHealth.Initialize(maxhealth);

        uiPrefab.SetActive(true);
    }

    void OnDisable()
    {
        uiPrefab.SetActive(false);
        isInitialized = false;
    }

    // Using overlap sphere for enemy detection instead of collider triggers for better performance control
    void Update()
    {
        // Skip everything if tower is dead
        //if (isDead) return;

        if (isInitialized)
        {

            // Update enemies in range using overlap sphere
            UpdateEnemiesInRange();

            // Find and prioritize targets from enemies in range
            FindAndPrioritizeTarget();

            // Continuously rotate towards current target
            if (currentTarget != null && !isRotating)
            {
                FaceTarget(currentTarget);
            }

            // ATTACK 
            if (GameTime.TotalTime >= nextAttackTime && currentTarget != null)
            {
                Attack(currentTarget);
                nextAttackTime = GameTime.TotalTime + attackRate;
            }

        }
    }

    private void UpdateEnemiesInRange()
    {
        // Clear the current list
        enemiesInRange.Clear();

        // Use overlap sphere to detect enemies
        Collider[] colliders = Physics.OverlapSphere(transform.position, attackRange, enemyLayerMask);

        // Add valid enemy transforms to the list
        foreach (Collider col in colliders)
        {
            if (col != null && col.transform != null)
            {
                enemiesInRange.Add(col.transform);
            }
        }
    }

    private void FindAndPrioritizeTarget()
    {
        // Clean up null references (destroyed enemies)
        enemiesInRange.RemoveAll(enemy => enemy == null);

        Transform bestTarget = null;
        float furthestProgress = -1f;

        foreach (Transform enemy in enemiesInRange)
        {
            if (enemy != null)
            {
                // Get the enemy's path progress
                FollowWP followWP = enemy.GetComponent<FollowWP>();
                if (followWP != null)
                {
                    float progress = followWP.GetPathProgress(); // Get progress along path

                    // Prioritize enemy furthest along the path
                    if (progress > furthestProgress)
                    {
                        furthestProgress = progress;
                        bestTarget = enemy;
                    }
                }
                else
                {
                    // Fallback: if no FollowWP script, use distance as priority (closer = further along path assumption)
                    if (bestTarget == null)
                    {
                        bestTarget = enemy;
                    }
                }
            }
        }

        currentTarget = bestTarget;
    }

    // Method to take damage
    public void TakeDamage(float damageAmount)
    {
        //if (isDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth);

        // Update radial health bar
        if (radialHealth != null)
        {
            float delta = -damageAmount;
            radialHealth.ChangeHealth(delta);
        }

        //Debug.Log($"Tower took {damageAmount} damage. Health: {currentHealth}/{maxhealth}");

        // Check if tower should die
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Method to handle tower death
    private void Die()
    {
        rotationSequence.Kill();
        currentTarget = null;
        hexTile.OnTowerDestroyed();
        gameObject.SetActive(false);
        isInitialized = false;
    }

    private void Attack(Transform target)
    {
        if (eyeTownEffectPrefab != null)
        {
            // Make tower face the target
            FaceTarget(target);

            //Debug.Log($"[Tower] Attacking target {target.name} at position {target.position}");
            //Debug.Log($"[Tower] Spawning projectile at {attackPoint.position} with rotation {attackPoint.rotation}");

            GameObject projectileObj = Instantiate(currentAttackEffect, currentAttackPoint.position, currentAttackPoint.rotation);
            ITarget attack = projectileObj.GetComponent<ITarget>();
            attack?.SetTarget(target, damage);
            projectileObj.transform.localScale = Vector3.one; // Ensure projectile is visible    

        }
        else
        {
            Debug.LogError("[Tower] attackEffectPrefab is null!");
        }
    }

    private void FaceTarget(Transform target)
    {

        // Calculate direction to target (only Y axis rotation)
        Vector3 direction = (target.position - transform.position);
        direction.y = 0; // Keep tower upright, only rotate on Y axis
        direction = direction.normalized;

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
                    .OnComplete(() =>
                    {
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

        isInitialized = false;
    }
}
