using UnityEngine;

public class ChaserEnemy : MonoBehaviour
{
    [Header("Chase Settings")]
    public float detectionRadius = 10f;        
    public float chaseSpeed = 3f;              
    public float stopChaseDistance = 15f;
    
    [Header("Damage Settings - MESAFELİ SİSTEM")]
    public float damageZone = 3f;              
    public float damageAmount = 10f;           
    public float damageInterval = 1.5f;        
    private float damageTimer = 0f;
    
    [Header("Visual")]
    public Color idleColor = Color.magenta;
    public Color chasingColor = Color.red;
    public Color returningColor = Color.yellow;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    private Transform targetPlayer;
    private Vector3 spawnPosition;
    private bool isChasing = false;
    
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    
    private enum EnemyState { Idle, Chasing, Returning }
    private EnemyState currentState = EnemyState.Idle;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        spawnPosition = transform.position;
        
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = idleColor;
        }
    }
    
    void Update()
    {
        DetectPlayer();
        UpdateState();
        
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle();
                break;
            case EnemyState.Chasing:
                HandleChasing();
                break;
            case EnemyState.Returning:
                HandleReturning();
                break;
        }
    }
    
    void UpdateState()
    {
        float distanceToSpawn = Vector3.Distance(transform.position, spawnPosition);
        
        if (targetPlayer != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
            
            if (distanceToPlayer > stopChaseDistance)
            {
                targetPlayer = null;
                isChasing = false;
                currentState = EnemyState.Returning;
            }
            else
            {
                currentState = EnemyState.Chasing;
                isChasing = true;
            }
        }
        else
        {
            isChasing = false;
            
            if (distanceToSpawn > 1f)
            {
                currentState = EnemyState.Returning;
            }
            else
            {
                currentState = EnemyState.Idle;
            }
        }
        
        if (spriteRenderer != null)
        {
            switch (currentState)
            {
                case EnemyState.Idle:
                    spriteRenderer.color = idleColor;
                    break;
                case EnemyState.Chasing:
                    spriteRenderer.color = chasingColor;
                    break;
                case EnemyState.Returning:
                    spriteRenderer.color = returningColor;
                    break;
            }
        }
    }
    
    void HandleIdle()
    {
        rb.velocity = Vector2.zero;
    }
    
    void HandleChasing()
    {
        if (targetPlayer == null || rb == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
        
        if (distanceToPlayer < damageZone)
        {
            Vector2 direction = (targetPlayer.position - transform.position).normalized;
            rb.velocity = direction * (chaseSpeed * 0.5f); 
            
            damageTimer += Time.deltaTime;
            if (damageTimer >= damageInterval)
            {
                DealDamage();
                damageTimer = 0f;
            }
        }
        else
        {
            Vector2 direction = (targetPlayer.position - transform.position).normalized;
            rb.velocity = direction * chaseSpeed;
            
            damageTimer = 0f; 
        }
        
        if (spriteRenderer != null && rb.velocity.x != 0)
        {
            spriteRenderer.flipX = rb.velocity.x < 0;
        }
    }
    
    void HandleReturning()
    {
        if (rb == null) return;
        
        float distanceToSpawn = Vector3.Distance(transform.position, spawnPosition);
        
        if (distanceToSpawn > 0.5f)
        {
            Vector2 direction = (spawnPosition - transform.position).normalized;
            rb.velocity = direction * (chaseSpeed * 0.7f);
            
            if (spriteRenderer != null && direction.x != 0)
            {
                spriteRenderer.flipX = direction.x < 0;
            }
        }
        else
        {
            rb.velocity = Vector2.zero;
            currentState = EnemyState.Idle;
        }
    }
    
    void DetectPlayer()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        
        targetPlayer = null;
        foreach (Collider2D hit in hits)
        {
            bool isPlayer = hit.CompareTag("Player") || 
                           hit.name.ToLower().Contains("agent") || 
                           hit.name.ToLower().Contains("ai");
            
            if (isPlayer)
            {
                targetPlayer = hit.transform;
                break;
            }
        }
    }
    
    void DealDamage()
    {
        if (targetPlayer != null)
        {
            AgentFSM agentFSM = targetPlayer.GetComponent<AgentFSM>();
            if (agentFSM != null)
            {
                agentFSM.TakeDamage(damageAmount);
                
                if (showDebugLogs)
                    Debug.Log($"Damage: {gameObject.name} Agent'a {damageAmount} hasar verdi!");
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Vector3 pos = Application.isPlaying ? spawnPosition : transform.position;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageZone);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stopChaseDistance);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pos, 0.5f);
        Gizmos.DrawLine(transform.position, pos);
        
        if (Application.isPlaying && targetPlayer != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetPlayer.position);
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.name.Contains("Wall"))
        {
            Vector2 pushDirection = (transform.position - collision.transform.position).normalized;
            rb.AddForce(pushDirection * 3f, ForceMode2D.Impulse);
        }
    }
}