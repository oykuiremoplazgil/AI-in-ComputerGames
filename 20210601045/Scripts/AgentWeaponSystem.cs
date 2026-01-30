using UnityEngine;

public class AgentWeaponSystem : MonoBehaviour
{
    [Header("Weapon Settings")]
    public float fireRate = 0.5f;           
    public float damage = 20f;              
    public float range = 8f;
    public int maxAmmo = 30;
    public float reloadTime = 2f;
    
    [Header("Aggressive Mode Bonus")]
    public float aggressiveFireRate = 0.25f;  
    public float aggressiveDamage = 30f;      
    
    [Header("Auto-Target Settings")]
    public bool autoTarget = true;
    public float targetUpdateInterval = 0.2f;
    
    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 15f;
    
    [Header("Visual Effects")]
    public LineRenderer shootLine;
    public float lineDisplayTime = 0.1f;
    public Color shootColor = Color.yellow;
    
    [Header("Debug")]
    public bool showDebugLogs = false;  
    
    private float fireTimer = 0f;
    private int currentAmmo;  
    private bool isReloading = false;
    private float reloadTimer = 0f;
    private Transform currentTarget;
    private float targetUpdateTimer = 0f;
    
    void Start()
    {
        currentAmmo = maxAmmo;  
        
        if (shootLine == null)
        {
            GameObject lineObj = new GameObject("ShootLine");
            lineObj.transform.parent = transform;
            shootLine = lineObj.AddComponent<LineRenderer>();
            shootLine.startWidth = 0.05f;
            shootLine.endWidth = 0.05f;
            shootLine.material = new Material(Shader.Find("Sprites/Default"));
            shootLine.startColor = shootColor;
            shootLine.endColor = shootColor;
            shootLine.enabled = false;
        }
        
        Debug.Log($"[WEAPON] Silah aktif - Mermi: {currentAmmo}/{maxAmmo}, Menzil: {range}m");
    }
    
    void Update()
    {
        AgentFSM fsm = GetComponent<AgentFSM>();
        if (fsm == null) return;

        if (fsm.currentState == AgentFSM.AgentState.Dead)
            return;

        if (!fsm.CanKillEnemies)
            return;

        if (fsm.currentState != AgentFSM.AgentState.Evading)
            return;

        if (fsm.currentState == AgentFSM.AgentState.Idle)
            return;
    
        if (isReloading)
        {
            reloadTimer += Time.deltaTime;
            if (reloadTimer >= reloadTime)
            {
                currentAmmo = maxAmmo;
                isReloading = false;
                reloadTimer = 0f;
                
                Debug.Log($"Şarjör dolu! ({currentAmmo}/{maxAmmo})");
            }
            return;
        }
        
        if (currentAmmo <= 0)
        {
            StartReload();
            return;
        }
        
        if (autoTarget)
        {
            targetUpdateTimer += Time.deltaTime;
            if (targetUpdateTimer >= targetUpdateInterval)
            {
                FindNearestEnemy();
                targetUpdateTimer = 0f;
            }
        }
        
        float currentFireRate = fireRate;
        if (fsm != null && fsm.IsAggressiveMode)
        {
            currentFireRate = aggressiveFireRate; 
        }
        
        fireTimer += Time.deltaTime;
        if (fireTimer >= currentFireRate && currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.position);
            
            if (distance <= range)
            {
                Shoot(currentTarget);
                fireTimer = 0f;
            }
        }
    }
    
    void FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
        
        currentTarget = null;
        float closestDistance = Mathf.Infinity;
        
        foreach (Collider2D hit in hits)
        {
            if (hit.transform == transform) continue;
            
            if (hit.name.ToLower().Contains("stationary"))
            {
                continue; 
            }
            
            bool isEnemy = hit.CompareTag("Enemy") || 
                          hit.name.ToLower().Contains("enemy") || 
                          hit.name.ToLower().Contains("chaser") || 
                          hit.name.ToLower().Contains("patrol");
            
            if (isEnemy)
            {
                EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();
                if (enemyHealth != null && enemyHealth.IsDead())
                    continue;
                
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    currentTarget = hit.transform;
                }
            }
        }
    }
    
    void Shoot(Transform target)
    {
        currentAmmo--;
        
        if (showDebugLogs)
            Debug.Log($"[{Time.frameCount}] Ateş! Hedef: {target.name} | Mermi: {currentAmmo}/{maxAmmo}");
        
        if (projectilePrefab != null)
        {
            ShootProjectile(target);
        }
        else
        {
            ShootHitscan(target);
        }
    }
    
    void ShootProjectile(Transform target)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        projectile.name = "AgentBullet";
        
        Vector2 direction = (target.position - transform.position).normalized;
        
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = projectile.AddComponent<Rigidbody2D>();
        
        rb.gravityScale = 0f;
        rb.velocity = direction * projectileSpeed;
        
        AgentProjectile projScript = projectile.GetComponent<AgentProjectile>();
        if (projScript == null)
            projScript = projectile.AddComponent<AgentProjectile>();
        
        projScript.damage = damage;
        projScript.shooter = gameObject;
        
        Destroy(projectile, 5f);
    }
    
    void ShootHitscan(Transform target)
    {
        StartCoroutine(ShowShootLine(transform.position, target.position));
        
        EnemyHealth enemyHealth = target.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
        }
    }
    
    System.Collections.IEnumerator ShowShootLine(Vector3 start, Vector3 end)
    {
        shootLine.enabled = true;
        shootLine.SetPosition(0, start);
        shootLine.SetPosition(1, end);
        
        yield return new WaitForSeconds(lineDisplayTime);
        
        shootLine.enabled = false;
    }
    
    void StartReload()
    {
        if (isReloading) return;
        
        isReloading = true;
        reloadTimer = 0f;
        
        Debug.Log("Şarjör değiştiriliyor...");
    }
    
    public int GetCurrentAmmo()
    {
        return currentAmmo;
    }
    
    public int GetMaxAmmo()
    {
        return maxAmmo;
    }
    
    public bool IsReloading()
    {
        return isReloading;
    }
    
    public float GetReloadProgress()
    {
        return isReloading ? (reloadTimer / reloadTime) : 1f;
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
        
        if (Application.isPlaying && currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
        }
    }
}