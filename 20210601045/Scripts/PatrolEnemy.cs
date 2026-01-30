using UnityEngine;

public class PatrolEnemy : MonoBehaviour
{
    [Header("Patrol Settings")]
    public float patrolSpeed = 2f;
    public float patrolDistance = 5f;
    public bool patrolHorizontal = true;
    
    [Header("Detection")]
    public float detectionRadius = 5f;      
    
    [Header("Damage Settings - DENGELİ")]
    public float damageZone = 2.5f;         
    public float damageAmount = 10f;        
    public float damageInterval = 2f;       
    private float damageTimer = 0f;
    
    [Header("Visual")]
    public Color normalColor = Color.cyan;
    public Color detectedColor = Color.yellow;
    public Color attackingColor = Color.red;
    private SpriteRenderer spriteRenderer;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    private Vector3 spawnPosition;
    private Vector3 targetPosition;
    private bool movingToTarget = true;
    private Transform detectedPlayer;
    
    private Rigidbody2D rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
        
        spawnPosition = transform.position;
        movingToTarget = Random.value > 0.5f;
        SetNewTargetPosition();
        
        gameObject.tag = "Enemy";
        gameObject.layer = LayerMask.NameToLayer("Enemy");
    }
    
    void Update()
    {
        DetectPlayer();
        Patrol();
        
        if (detectedPlayer != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, detectedPlayer.position);
            
            if (distanceToPlayer <= damageZone)
            {
                if (spriteRenderer != null)
                    spriteRenderer.color = attackingColor;
                
                damageTimer += Time.deltaTime;
                if (damageTimer >= damageInterval)
                {
                    DealDamage();
                    damageTimer = 0f;
                }
            }
            else
            {
                if (spriteRenderer != null)
                    spriteRenderer.color = detectedColor;
                damageTimer = 0f;
            }
        }
        else
        {
            if (spriteRenderer != null)
                spriteRenderer.color = normalColor;
            damageTimer = 0f;
        }
    }
    
    void Patrol()
    {
        if (rb == null) return;
        
        Vector2 direction = (targetPosition - transform.position).normalized;
        rb.velocity = direction * patrolSpeed;
        
        if (spriteRenderer != null && patrolHorizontal && direction.x != 0)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget < 0.3f)
        {
            movingToTarget = !movingToTarget;
            SetNewTargetPosition();
        }
    }
    
    void SetNewTargetPosition()
    {
        if (patrolHorizontal)
        {
            targetPosition = movingToTarget ? 
                spawnPosition + Vector3.right * patrolDistance : 
                spawnPosition + Vector3.left * patrolDistance;
        }
        else
        {
            targetPosition = movingToTarget ? 
                spawnPosition + Vector3.up * patrolDistance : 
                spawnPosition + Vector3.down * patrolDistance;
        }
    }
    
    void DetectPlayer()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        
        detectedPlayer = null;
        foreach (Collider2D hit in hits)
        {
            if (hit.transform == transform) continue;
            
            bool isPlayer = hit.CompareTag("Player") || 
                           hit.name.ToLower().Contains("agent") || 
                           hit.name.ToLower().Contains("ai");
            
            if (isPlayer)
            {
                AgentFSM testFSM = hit.GetComponent<AgentFSM>();
                if (testFSM != null)
                {
                    detectedPlayer = hit.transform;
                    break;
                }
            }
        }
    }
    
    void DealDamage()
    {
        if (detectedPlayer == null) return;
        
        AgentFSM agentFSM = detectedPlayer.GetComponent<AgentFSM>();
        if (agentFSM != null)
        {
            agentFSM.TakeDamage(damageAmount);
            
            if (showDebugLogs)
                Debug.Log($"Damage: {gameObject.name} Agent'a {damageAmount} hasar verdi!");
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.name.Contains("Wall"))
        {
            movingToTarget = !movingToTarget;
            SetNewTargetPosition();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Vector3 pos = Application.isPlaying ? spawnPosition : transform.position;
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pos, 0.5f);
        
        Gizmos.color = Color.cyan;
        if (patrolHorizontal)
        {
            Vector3 left = pos + Vector3.left * patrolDistance;
            Vector3 right = pos + Vector3.right * patrolDistance;
            Gizmos.DrawLine(left, right);
            Gizmos.DrawWireSphere(left, 0.3f);
            Gizmos.DrawWireSphere(right, 0.3f);
        }
        else
        {
            Vector3 down = pos + Vector3.down * patrolDistance;
            Vector3 up = pos + Vector3.up * patrolDistance;
            Gizmos.DrawLine(down, up);
            Gizmos.DrawWireSphere(down, 0.3f);
            Gizmos.DrawWireSphere(up, 0.3f);
        }
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageZone);
        
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetPosition, 0.4f);
        }
        
        if (Application.isPlaying && detectedPlayer != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, detectedPlayer.position);
        }
    }
}