using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;   // <-- added
using NUnit.Framework;

public class Tower : MonoBehaviour
{
    //VARIABLES
    [SerializeField] public SO_Defender defenderData;
    [SerializeField] public int towerLevel = 1;
    [SerializeField] public float maxhealth = 100.0f;
    [SerializeField] public float currentHealth = 100.0f;
    [SerializeField] public RadialHealth radialHealth;

    [SerializeField] public float damage = 100.0f;

    [SerializeField] public float attackRange = 5.0f;
    [SerializeField] private float attackRate = 1.0f;

    //ATTACK PREFABS
    [SerializeField] private GameObject lanternKeeperAttackEffectPrefab;
    [SerializeField] private GameObject eyeTownEffectPrefab;
    [SerializeField] private GameObject witchAttackEffectPrefab;
    [SerializeField] private GameObject heartAttackEffectPrefab; // REQUIRES BEHAVIOUR MODIFICATION
    [SerializeField] private GameObject angelAttackEffectPrefab;

    private GameObject currentAttackEffect;
    private Transform currentAttackPoint;

    [SerializeField] Transform lanternKeeperAttackPoint;
    [SerializeField] Transform eyeTowerAttackPoint;
    [SerializeField] Transform witchAttackPoint;
    [SerializeField] Transform heartAttackPoint;
    [SerializeField] Transform angelAttackPoint;

    private float rotationSpeed = 5.0f; // Speed of rotation in seconds

    [SerializeField] private GameObject uiPrefab;

    private float nextAttackTime = 0f;
    private Sequence rotationSequence;
    private Transform currentTarget;
    private bool isRotating = false;

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
                currentAttackPoint = lanternKeeperAttackPoint;
                break;
            case "Eye Tower":
                towerModels[1].SetActive(true);
                currentAttackEffect = eyeTownEffectPrefab;
                currentAttackPoint = eyeTowerAttackPoint;
                break;
            case "Witch":
                towerModels[2].SetActive(true);
                currentAttackEffect = witchAttackEffectPrefab;
                currentAttackPoint = witchAttackPoint;
                break;
            case "Heart Reliquary":
                towerModels[3].SetActive(true);
                currentAttackEffect = heartAttackEffectPrefab;
                currentAttackPoint = heartAttackPoint;
                break;
            case "The Angel":
                towerModels[4].SetActive(true);
                currentAttackEffect = angelAttackEffectPrefab;
                currentAttackPoint = angelAttackPoint;
                break;
            default:
                Debug.LogWarning($"[Tower] No model found for defender name: {defenderData.defenderName}");
                break;
        }

        if (defenderData != null)
        {
            damage = defenderData.defenderDamage;
            attackRange = defenderData.range;
            attackRate = defenderData.attackSpeed;
            maxhealth = defenderData.defenderHealth;
            currentHealth = maxhealth;
        }
        else
        {
            Debug.LogWarning("[Tower] Initialize called with null defenderData.");
        }

        isInitialized = true;

        // make sure health UI is correct
        if (radialHealth != null)
        {
            radialHealth.Initialize(maxhealth);
        }

        // also tell tower UI if this is the selected one
        RefreshTowerUIIfSelected();
    }

    public void UpgradeTower(int level)
    {
        towerLevel = Mathf.Max(1, level);
        UpdateTowerStats();
    }

    private void UpdateTowerStats()
    {
        if (defenderData != null)
        {
            // use float scaling so it actually changes per level
            float scale = 1f + (towerLevel / 20f);

            damage = defenderData.defenderDamage * scale;
            attackRange = defenderData.range;
            // if higher level should attack faster, you usually want to DIVIDE the cooldown, not multiply
            // but I’ll keep your intent and just make the scaling actually work:
            attackRate = defenderData.attackSpeed / scale;

            maxhealth = defenderData.defenderHealth * scale;
            currentHealth = maxhealth;

            // update health UI
            if (radialHealth != null)
            {
                radialHealth.Initialize(maxhealth);
            }

            // make sure the tower UI shows the new numbers
            RefreshTowerUIIfSelected();
        }
        else
        {
            Debug.LogWarning("[Tower] UpdateTowerStats called with null defenderData.");
        }
    }

    public void ResetTowerLevel()
    {
        towerLevel = 1;
        if (defenderData != null)
        {
            damage = defenderData.defenderDamage;
            attackRange = defenderData.range;
            attackRate = defenderData.attackSpeed;
            maxhealth = defenderData.defenderHealth;
            currentHealth = maxhealth;

            if (radialHealth != null)
            {
                radialHealth.Initialize(maxhealth);
            }
        }

        RefreshTowerUIIfSelected();
    }

    private void RefreshTowerUIIfSelected()
    {
        if (TowerUI.Instance != null && TowerUI.Instance.currentSelectedTower == this)
        {
            TowerUI.Instance.ForceRefresh();
        }
    }

    // =========================
    // ODIN DEBUG BUTTONS
    // =========================
    [Button("DEBUG: Upgrade +1")]
    private void DebugUpgradeOne()
    {
        UpgradeTower(towerLevel + 1);
    }

    [Button("DEBUG: Upgrade +5")]
    private void DebugUpgradeFive()
    {
        UpgradeTower(towerLevel + 5);
    }

    [Button("DEBUG: Reset to Level 1")]
    private void DebugReset()
    {
        ResetTowerLevel();
    }

    [Button("DEBUG: Reapply From DefenderData")]
    private void DebugReapplyFromData()
    {
        UpdateTowerStats();
    }

    void Start()
    {
        currentHealth = maxhealth;
        if (radialHealth != null)
        {
            radialHealth.Initialize(maxhealth);
        }
    }

    void OnEnable()
    {
        currentHealth = maxhealth;
        if (radialHealth != null)
        {
            radialHealth.Initialize(maxhealth);
        }

        if (uiPrefab != null)
            uiPrefab.SetActive(true);
    }

    void OnDisable()
    {
        if (uiPrefab != null)
            uiPrefab.SetActive(false);
        isInitialized = false;
    }

    void Update()
    {
        if (isInitialized)
        {
            UpdateEnemiesInRange();
            FindAndPrioritizeTarget();

            if (currentTarget != null && !isRotating)
            {
                FaceTarget(currentTarget);
            }

            if (GameTime.TotalTime >= nextAttackTime && currentTarget != null)
            {
                Attack(currentTarget);
                nextAttackTime = GameTime.TotalTime + attackRate;
            }
        }
    }

    private void UpdateEnemiesInRange()
    {
        enemiesInRange.Clear();

        Collider[] colliders = Physics.OverlapSphere(transform.position, attackRange, enemyLayerMask);

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
        enemiesInRange.RemoveAll(enemy => enemy == null);

        Transform bestTarget = null;
        float furthestProgress = -1f;

        foreach (Transform enemy in enemiesInRange)
        {
            if (enemy != null)
            {
                FollowWP followWP = enemy.GetComponent<FollowWP>();
                if (followWP != null)
                {
                    float progress = followWP.GetPathProgress();

                    if (progress > furthestProgress)
                    {
                        furthestProgress = progress;
                        bestTarget = enemy;
                    }
                }
                else
                {
                    if (bestTarget == null)
                    {
                        bestTarget = enemy;
                    }
                }
            }
        }

        currentTarget = bestTarget;
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth);

        if (radialHealth != null)
        {
            float delta = -damageAmount;
            radialHealth.ChangeHealth(delta);
        }

        if (currentHealth <= 0)
        {
            Die();
        }

        // if we damage it during debug and UI is open, update it
        RefreshTowerUIIfSelected();
    }

    private void Die()
    {
        if (rotationSequence != null)
            rotationSequence.Kill();

        currentTarget = null;

        if (hexTile != null)
            hexTile.OnTowerDestroyed();

        gameObject.SetActive(false);
        isInitialized = false;
    }

    private void Attack(Transform target)
    {
        if (currentAttackEffect != null)
        {
            FaceTarget(target);
            AudioManager.Instance.PlaySFX("SFX_TowerAttack");
            GameObject projectileObj = Instantiate(currentAttackEffect, currentAttackPoint.position, currentAttackPoint.rotation);
            ITarget attack = projectileObj.GetComponent<ITarget>();
            attack?.SetTarget(target, damage);
            projectileObj.transform.localScale = Vector3.one;
        }
        else
        {
            Debug.LogError("[Tower] attackEffectPrefab is null!");
        }
    }

    private void FaceTarget(Transform target)
    {
        Vector3 direction = (target.position - transform.position);
        direction.y = 0;
        direction = direction.normalized;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            float targetYRotation = lookRotation.eulerAngles.y;
            float currentYRotation = transform.eulerAngles.y;

            float angleDifference = Mathf.DeltaAngle(currentYRotation, targetYRotation);

            if (Mathf.Abs(angleDifference) > 1f)
            {
                if (rotationSequence != null)
                {
                    rotationSequence.Kill();
                }

                isRotating = true;

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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
        }
    }

    void OnDestroy()
    {
        if (rotationSequence != null)
        {
            rotationSequence.Kill();
        }

        isInitialized = false;
    }
}
