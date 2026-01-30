using UnityEngine;
public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("Visual Feedback")]
    public bool changeColorOnDamage = true;
    public Color damageColor = Color.white;
    public float damageFlashDuration = 0.1f;
    
    [Header("Death")]
    public GameObject deathEffectPrefab;
    public float fadeOutDuration = 0.5f;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    [Header("Invincibility")]
public bool isInvincible = false;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isDead = false;
    
    private MonoBehaviour[] enemyScripts;
    
    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
    
        enemyScripts = GetComponents<MonoBehaviour>();
    }
    
    public void TakeDamage(float damage)
    {
         if (isInvincible) return;
        if (isDead) return;
        
        currentHealth -= damage;
        
        if (showDebugLogs)
            Debug.Log($" {gameObject.name} hasar aldı: -{damage} (Kalan: {currentHealth:F1}/{maxHealth})");
        
      
        if (changeColorOnDamage && spriteRenderer != null)
        {
            StartCoroutine(DamageFlash());
        }
        
       
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    System.Collections.IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(damageFlashDuration);
            spriteRenderer.color = originalColor;
        }
    }
    
    void Die()
    {
        if (isDead) return;
        isDead = true;
        
        if (showDebugLogs)
            Debug.Log($"💀 {gameObject.name} öldü!");
        
       
        DisableEnemyScripts();
        
      
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false; 
        }
        

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        

        StartCoroutine(FadeOutAndDestroy());
    }
    
    void DisableEnemyScripts()
    {

        foreach (MonoBehaviour script in enemyScripts)
        {
            if (script != this && script != null)
            {

                if (script.GetType().Name.Contains("Enemy") || 
                    script.GetType().Name.Contains("Chaser") || 
                    script.GetType().Name.Contains("Patrol") ||
                    script.GetType().Name.Contains("Stationary"))
                {
                    script.enabled = false;
                }
            }
        }
    }
    
    System.Collections.IEnumerator FadeOutAndDestroy()
    {
        float elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / fadeOutDuration);
            
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = alpha;
                spriteRenderer.color = c;
            }
            
            yield return null;
        }
        

        Destroy(gameObject);
    }
    
 
    public bool IsDead() => isDead;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    
    void OnDrawGizmosSelected()
    {
  
        if (Application.isPlaying)
        {
            Vector3 barPosition = transform.position + Vector3.up * 1.5f;
            float barWidth = 1f;
            float healthPercentage = currentHealth / maxHealth;
            
     
            Gizmos.color = Color.red;
            Gizmos.DrawLine(barPosition - Vector3.right * barWidth * 0.5f, 
                           barPosition + Vector3.right * barWidth * 0.5f);
            
        
            Gizmos.color = Color.green;
            Vector3 healthEnd = barPosition - Vector3.right * barWidth * 0.5f + 
                               Vector3.right * barWidth * healthPercentage;
            Gizmos.DrawLine(barPosition - Vector3.right * barWidth * 0.5f, healthEnd);
        }
    }
}
