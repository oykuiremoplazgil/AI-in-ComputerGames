using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AgentFSM : MonoBehaviour
{
    public enum AgentState { Idle, MovingToGoal, Evading, Dead }

    [Header("FSM Settings")]
    public AgentState currentState = AgentState.Idle;

    [Header("Detection Settings")]
    public float enemyDetectionRadius = 10f;
    public float dangerThreshold = 5f;
    public float safeDistance = 12f;

    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Damage Protection")]
    public float damageInvincibilityTime = 0.5f;
    private float lastDamageTime = -999f;

    [Header("Combat Behavior")]
    public float aggressiveHealthThreshold = 0.5f; 
    
    public bool IsAggressiveMode => (currentHealth / maxHealth) < aggressiveHealthThreshold;
   
    public bool CanKillEnemies
    {
        get
        {
            return (currentHealth / maxHealth) <= 0.5f;
        }
    }

    [Header("Evading Behavior")]
    public float evadeSpeedMultiplier = 1.6f;
    public float panicSpeedMultiplier = 2.2f;
    public float evadeMinDuration = 2f;
    public float stateChangeCooldown = 0.5f;

    [Header("References")]
    private AI_Agent agentController;
    private Rigidbody2D rb;
    private DungeonGenerator dungeonGenerator;

    [Header("Debug")]
    public bool showDebugLogs = false;
    public bool showGizmos = true;
    
    private bool aggressiveModeAnnounced = false; 

    private List<Transform> nearbyEnemies = new List<Transform>();
    private Transform closestEnemy;
    private float stateTimer = 0f;
    private float lastStateChangeTime = 0f;
    private Vector3 lastEvadeDirection;

    void Start()
    {
        agentController = GetComponent<AI_Agent>();
        rb = GetComponent<Rigidbody2D>();
        dungeonGenerator = FindObjectOfType<DungeonGenerator>();

        currentHealth = maxHealth;
        currentState = AgentState.MovingToGoal;

        if (agentController == null)
            Debug.LogError("AI_Agent component bulunamadı!");
        
        Debug.Log("FSM başlatıldı");
    }

    void Update()
    {
        if (currentState == AgentState.Dead) return;

        stateTimer += Time.deltaTime;
        DetectEnemies();

        switch (currentState)
        {
            case AgentState.Idle:
                HandleIdleState();
                break;
            case AgentState.MovingToGoal:
                HandleMovingToGoal();
                break;
            case AgentState.Evading:
                HandleEvadingState();
                break;
        }
        
        CheckGoalReach();
    }

    void CheckGoalReach()
    {
        if (dungeonGenerator == null) return;

        float distanceToGoal = Vector3.Distance(transform.position, dungeonGenerator.GetGoalPosition());
        
        if (distanceToGoal < 1.5f && currentState != AgentState.Idle)
        {
            Debug.Log("HEDEFE ULAŞILDI!");
            
            if (agentController != null) 
                agentController.enabled = false;
            
            AgentWeaponSystem weapon = GetComponent<AgentWeaponSystem>();
            if (weapon != null)
                weapon.enabled = false;
            
            GameHUD hud = FindObjectOfType<GameHUD>();
            if (hud != null)
            {
                hud.ShowWinScreen();
                Debug.Log("KAZANDINIZ! Hedefe ulaşıldı!");
            }
            
            TransitionToState(AgentState.Idle);
        }
    }

    void HandleIdleState()
    {
        if (ShouldEvade())
            TransitionToState(AgentState.Evading);
        else if (closestEnemy == null)
            TransitionToState(AgentState.MovingToGoal);
    }

    void HandleMovingToGoal()
    {
        if (ShouldEvade())
            TransitionToState(AgentState.Evading);
    }

    void HandleEvadingState()
    {
        if (closestEnemy == null)
        {
            if (stateTimer > evadeMinDuration)
                TransitionToState(AgentState.MovingToGoal);
            return;
        }
        if (rb.velocity.magnitude < 0.5f && stateTimer > 0.5f)
    {
        TransitionToState(AgentState.MovingToGoal);
        return;
   
    }

        float distance = Vector3.Distance(transform.position, closestEnemy.position);
        bool isSafe = distance > safeDistance;
        bool evadedLongEnough = stateTimer > evadeMinDuration;

        if (isSafe && evadedLongEnough)
        {
            EnemyHealth eh = closestEnemy.GetComponent<EnemyHealth>();
            if (eh != null && !eh.IsDead())
            {
                return; 
            }

            TransitionToState(AgentState.MovingToGoal);
            return;
        }

        Vector2 evadeDirection = CalculateSmartEvadeDirection();

        if (agentController != null && agentController.enabled)
            agentController.enabled = false;

        if (rb != null)
        {
            float speedMultiplier = GetEvadeSpeedMultiplier();
            float baseSpeed = agentController != null ? agentController.moveSpeed : 5f;
            rb.velocity = evadeDirection * (baseSpeed * speedMultiplier);
        }
    }

   bool ShouldEvade()
{
    if (closestEnemy == null) return false;

    if (currentHealth < maxHealth * aggressiveHealthThreshold) 
    {
        return false; 
    }

    float distance = Vector3.Distance(transform.position, closestEnemy.position);
    if (Time.time - lastStateChangeTime < stateChangeCooldown) return false;

    return (currentState == AgentState.Evading) ? distance < safeDistance : distance < dangerThreshold;
}
    void DetectEnemies()
    {
        nearbyEnemies.Clear();
        closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, enemyDetectionRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.transform == transform) continue;

            if (hit.name.ToLower().Contains("stationary"))
            {
                continue; 
            }

            if (hit.CompareTag("Enemy") || 
                hit.name.ToLower().Contains("enemy") ||
                hit.name.ToLower().Contains("chaser") ||
                hit.name.ToLower().Contains("patrol"))
            {
                EnemyHealth eHealth = hit.GetComponent<EnemyHealth>();
                if (eHealth != null && eHealth.IsDead()) continue;

                nearbyEnemies.Add(hit.transform);
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestEnemy = hit.transform;
                }
            }
        }
    }

    void TransitionToState(AgentState newState)
    {
        if (currentState == newState) return;
        if (newState != AgentState.Dead && Time.time - lastStateChangeTime < stateChangeCooldown) return;

        if (showDebugLogs) Debug.Log($"State: {currentState} -> {newState}");

        currentState = newState;
        stateTimer = 0f;
        lastStateChangeTime = Time.time;

        AgentWeaponSystem weapon = GetComponent<AgentWeaponSystem>();
        
        switch (newState)
        {
            case AgentState.Dead:
                Debug.Log("Agent öldü!");
                if (agentController != null) agentController.enabled = false;
                if (rb != null) rb.velocity = Vector2.zero;
                if (weapon != null) weapon.enabled = false;
                StartCoroutine(DeathSequence());
                break;

            case AgentState.MovingToGoal:
                if (agentController != null) agentController.enabled = true;
                if (weapon != null) weapon.enabled = true;
                break;

            case AgentState.Evading:
                if (agentController != null) agentController.enabled = false;
                if (weapon != null) weapon.enabled = true;
                break;
        }
    }

    float GetEvadeSpeedMultiplier()
    {
        float healthPercent = currentHealth / maxHealth;
        if (healthPercent < 0.3f) return panicSpeedMultiplier;
        return (healthPercent < 0.7f) ? evadeSpeedMultiplier * 1.2f : evadeSpeedMultiplier;
    }

    Vector2 CalculateSmartEvadeDirection()
    {
        if (closestEnemy == null) return lastEvadeDirection;
        
        Vector2 awayFromEnemy = (transform.position - closestEnemy.position).normalized;
        
        if (nearbyEnemies.Count > 1)
        {
            Vector2 totalThreat = Vector2.zero;
            foreach (Transform enemy in nearbyEnemies)
            {
                if (enemy == null) continue;
                float dist = Vector3.Distance(transform.position, enemy.position);
                totalThreat += (Vector2)(transform.position - enemy.position).normalized * (1f / Mathf.Max(dist, 0.5f));
            }
            awayFromEnemy = totalThreat.normalized;
        }

        if (dungeonGenerator != null)
        {
            Vector2 toGoal = (dungeonGenerator.GetGoalPosition() - transform.position).normalized;
            awayFromEnemy = (awayFromEnemy * 0.7f + toGoal * 0.3f).normalized;
        }

        lastEvadeDirection = awayFromEnemy;
        return awayFromEnemy;
    }

    public void TakeDamage(float damage)
    {
        if (Time.time - lastDamageTime < damageInvincibilityTime)
        {
            if (showDebugLogs)
                Debug.Log("Invincible!");
            return;
        }

        currentHealth -= damage;
        lastDamageTime = Time.time;

        float minHealth = maxHealth * 0.1f;
        if (currentHealth < minHealth)
        {
            currentHealth = minHealth;
            Debug.Log("MİNİMUM CAN! Agent ölmez.");
        }

        Debug.Log($"Hasar: {damage} (Kalan: {currentHealth:F1}/{maxHealth})");

        TransitionToState(AgentState.Evading);
    }

    IEnumerator DamageFlash()
    {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            Color original = sprite.color;
            sprite.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            sprite.color = original;
        }
    }

    IEnumerator DeathSequence()
    {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 1f - elapsed);
                yield return null;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, enemyDetectionRadius);
        
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, dangerThreshold);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, safeDistance);
        
        if (Application.isPlaying && closestEnemy != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, closestEnemy.position);
        }
        
        if (Application.isPlaying && currentState == AgentState.Evading)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, lastEvadeDirection * 3f);
        }
    }
}