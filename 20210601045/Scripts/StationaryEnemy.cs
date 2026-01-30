using UnityEngine;

public class StationaryEnemy : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRadius = 6f;     
    
    [Header("Damage Settings - Melee Only")]
    public float damageZone = 1.5f;      
    public float meleeDamage = 8f;       
    public float meleeInterval = 2f;        
    private float meleeTimer = 0f;
    
    [Header("Shooting Settings - KAPALI")]
    public bool canShoot = false;           
    public float shootRange = 6f;
    public float shootDamage = 15f;
    public float shootInterval = 2f;
    private float shootTimer = 0f;
    
    [Header("Visual")]
    public Color idleColor = Color.yellow;
    public Color attackColor = Color.red;
    private SpriteRenderer spriteRenderer;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    private Transform detectedPlayer;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = idleColor;
        }
        

        gameObject.tag = "Enemy";
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        
        Debug.Log($"🟡 {gameObject.name} hazır - Pasif düşman (Agent kaçar)");
    }
    
    void Update()
    {
        DetectPlayer();
        
        if (detectedPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, detectedPlayer.position);
            
  
            if (distance <= damageZone)
            {
                if (spriteRenderer != null)
                    spriteRenderer.color = attackColor;
                
                meleeTimer += Time.deltaTime;
                if (meleeTimer >= meleeInterval)
                {
                    DealMeleeDamage();
                    meleeTimer = 0f;
                }
            }
            else
            {
            
                if (spriteRenderer != null)
                    spriteRenderer.color = idleColor;
                
                meleeTimer = 0f;
            }
        }
        else
        {
     
            if (spriteRenderer != null)
                spriteRenderer.color = idleColor;
            
            meleeTimer = 0f;
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
    
    void DealMeleeDamage()
    {
        if (detectedPlayer == null) return;
        
        AgentFSM agentFSM = detectedPlayer.GetComponent<AgentFSM>();
        if (agentFSM != null)
        {
            agentFSM.TakeDamage(meleeDamage);
            
            if (showDebugLogs)
                Debug.Log($"💀 {gameObject.name} Agent'a melee hasar: {meleeDamage}");
        }
    }
    
    void OnDrawGizmosSelected()
    {
    
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageZone);
        
        if (Application.isPlaying && detectedPlayer != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, detectedPlayer.position);
        }
    }
}