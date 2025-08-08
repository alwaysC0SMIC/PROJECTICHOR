using System;
using Unity.VisualScripting;
using UnityEngine;

public class CentreTower : MonoBehaviour
{
    //VARIABLES
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    private EventBinding<CentreTowerAttackEvent> centreTowerAttackBinding;

    void Start()
    {
        currentHealth = maxHealth;
        centreTowerAttackBinding = new EventBinding<CentreTowerAttackEvent>(OnCentreTowerAttacked);
        EventBus<CentreTowerAttackEvent>.Register(centreTowerAttackBinding);
    }

    private void OnCentreTowerAttacked(CentreTowerAttackEvent @event)
    {
        currentHealth -= @event.damageAmount;
        HealthCheck();
    }

    private void HealthCheck()
    {
        if (currentHealth <= 0)
        {
            EventBus<UpdateGameStateEvent>.Raise(new UpdateGameStateEvent { gameState = GameState.Lose });
        }
    }
}
