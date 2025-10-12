using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using DG.Tweening;

public class Enemy : MonoBehaviour
{
    //VARIABLES
    [SerializeField] public SO_Enemy enemyData;
    [SerializeField] private EnemyState state = EnemyState.Idle;
    [SerializeField] private EnemyHealth health;
    [SerializeField] private FollowWP followWP;
    [SerializeField] private EnemyAttack attack;

    [SerializeField] private GameObject deathEffect;

    void Awake()
    {
        transform.localScale = Vector3.zero;
    }

    public void Initialize(SO_Enemy enemydata, List<Transform> inWaypoints)
    {

        //DATA
        enemyData = enemydata;

        //HEALTH
        health.Initialize(enemyData.maxhealth, this);

        //ATTACK
        attack.Initialize(this);

        //MOVEMENT
        followWP.Initialize(this, inWaypoints);
        UpdateEnemyState(EnemyState.Moving);

        transform.DOScale(Vector3.one, 1f)
        .SetEase(Ease.OutCubic);
    }

    

    public void LookForTarget(out bool foundTarget)
    {
        // First set to checking state to stop movement
        UpdateEnemyState(EnemyState.CheckingForTargets);
        
        if (attack.LookForTowers())
        {
            UpdateEnemyState(EnemyState.MovingToAttackPosition);
            foundTarget = true;
        }
        else
        {
            UpdateEnemyState(EnemyState.Moving);
            foundTarget = false;
        }
    }

    public void ReturnToPath()
    {
        // Return to the current waypoint and resume normal movement
        UpdateEnemyState(EnemyState.MovingBackToPath);
        attack.StopAttacking();
    }

    public void StartAttacking()
    {
        UpdateEnemyState(EnemyState.Attacking);
    }

    public void StartAttackingCentreHub()
    {
        UpdateEnemyState(EnemyState.MovingToCentreHubPosition);
        attack.BeginAttackCentreHub();
    }

    public void TriggerDeath()
    {
        EventBus<AddOrRemoveIchorEvent>.Raise(new AddOrRemoveIchorEvent() { addOrRemove = true, ichorAmount = 10});

        // Notify the EnemyManager that this enemy has been destroyed
        EventBus<EnemyDestroyedEvent>.Raise(new EnemyDestroyedEvent { enemyObject = this.gameObject, enemyData = this.enemyData });

        Instantiate(deathEffect, transform.position, transform.rotation);

        transform.DOScale(Vector3.zero, 0.2f)
    .SetEase(Ease.InBack) // optional, adds a nice shrinking effect
    .OnComplete(() => Destroy(gameObject));

        
    }

    public void UpdateEnemyState(EnemyState newState)
    {
        state = newState;

        switch (state)
        {
            case EnemyState.Idle:
                followWP.moveNormally = false;
                break;

            case EnemyState.Moving:
                followWP.moveNormally = true;
                break;

            case EnemyState.CheckingForTargets:
                followWP.moveNormally = false;
                break;

            case EnemyState.MovingToAttackPosition:
                followWP.moveNormally = false;
                attack.BeginAttackTower();
                break;

            case EnemyState.Attacking:
                followWP.moveNormally = false;
                break;

            case EnemyState.MovingBackToPath:
                followWP.moveNormally = true;
                break;

            case EnemyState.MovingToCentreHubPosition:
                followWP.moveNormally = false;
                break;

            case EnemyState.AttackingCentreHub:
                followWP.moveNormally = false;
                break;

            case EnemyState.Dying:
                TriggerDeath();
                followWP.moveNormally = false;
                break;
        }
    }
    
}

public enum EnemyState
{
    Idle,
    Moving,
    CheckingForTargets,
    MovingToAttackPosition,
    Attacking,
    MovingBackToPath,
    MovingToCentreHubPosition,
    AttackingCentreHub,
    Dying
}
