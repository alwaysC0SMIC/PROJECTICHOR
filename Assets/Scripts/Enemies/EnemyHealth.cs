using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    private Enemy parentEnemy;
    [SerializeField] private float maxHealth = 100.0f;
    [SerializeField] private float currentHealth;

    public void Initialize(float initialHealth, Enemy enemy)
    {
        parentEnemy = enemy;
        maxHealth = initialHealth;
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        DeathCheck();
    }

    private void DeathCheck()
    { 
        if (currentHealth <= 0)
        {
            parentEnemy.UpdateEnemyState(EnemyState.Dying);
        }
    }
}
