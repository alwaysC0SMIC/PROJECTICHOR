using UnityEngine;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{
    //VARIABLES
    [SerializeField] public SO_Enemy enemyData;
    [SerializeField] private EnemyState state = EnemyState.Idle;
    [SerializeField] private EnemyHealth health;
    [SerializeField] private FollowWP followWP;
    [SerializeField] private EnemyAttack attack;

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
        gameObject.SetActive(false);
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
