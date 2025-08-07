using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100.0f;
    [SerializeField] private float currentHealth;
    
    [Header("Death Effects")]
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private bool destroyOnDeath = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Events
    public System.Action<float, float> OnHealthChanged; // currentHealth, maxHealth
    public System.Action OnDeath;
    
    void Start()
    {
        currentHealth = maxHealth;
    }
    
    /// <summary>
    /// Take damage and check for death
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        if (showDebugInfo)
        {
            Debug.Log($"[EnemyHealth] {gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");
        }
        
        // Trigger health changed event
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Heal the enemy
    /// </summary>
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        if (showDebugInfo)
        {
            Debug.Log($"[EnemyHealth] {gameObject.name} healed for {amount}. Health: {currentHealth}/{maxHealth}");
        }
    }
    
    /// <summary>
    /// Handle enemy death
    /// </summary>
    private void Die()
    {
        if (showDebugInfo)
        {
            Debug.Log($"[EnemyHealth] {gameObject.name} died");
        }
        
        // Spawn death effect
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, transform.rotation);
        }
        
        // Trigger death event
        OnDeath?.Invoke();
        
        // Destroy the enemy
        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Get current health percentage (0-1)
    /// </summary>
    public float GetHealthPercentage()
    {
        return maxHealth > 0 ? currentHealth / maxHealth : 0;
    }
    
    /// <summary>
    /// Get current health
    /// </summary>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    /// <summary>
    /// Get max health
    /// </summary>
    public float GetMaxHealth()
    {
        return maxHealth;
    }
    
    /// <summary>
    /// Set max health and optionally heal to full
    /// </summary>
    public void SetMaxHealth(float newMaxHealth, bool healToFull = false)
    {
        maxHealth = newMaxHealth;
        
        if (healToFull)
        {
            currentHealth = maxHealth;
        }
        else
        {
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    /// <summary>
    /// Check if enemy is alive
    /// </summary>
    public bool IsAlive()
    {
        return currentHealth > 0;
    }
}
