using UnityEngine;
using System.Collections.Generic;

public class AI_Agent : MonoBehaviour
{
    private DungeonGenerator dungeonGenerator;
    private AStarPathfinding pathfinding;
    private Rigidbody2D rb;
    
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float waypointReachDistance = 0.5f;
    public float rotationSpeed = 8f;
    public float maxVelocityChange = 5f;
    public float stuckDetectionTime = 1.5f; 
    public float stuckDistanceThreshold = 0.3f;
    public int waypointLookahead = 1;
    
    private Vector3 lastStuckCheckPosition;
    private float stuckTimer = 0f;
    
    [Header("Pathfinding")]
    public float recalculatePathInterval = 2f;
    private float recalculateTimer = 0f;
    
    [Header("Debug")]
    public bool showPath = true;
    public bool showDebugLogs = true;
    
    private List<Vector2Int> currentPath;
    private int currentWaypointIndex = 0;
    private Vector3 goalPosition;
    private bool isInitialized = false;
    
    void Start()
    {
        dungeonGenerator = FindObjectOfType<DungeonGenerator>();
        pathfinding = FindObjectOfType<AStarPathfinding>();
        rb = GetComponent<Rigidbody2D>();
        
        if (dungeonGenerator == null)
        {
            Debug.LogError("DungeonGenerator bulunamadı!");
            enabled = false;
            return;
        }
        
        if (pathfinding == null)
        {
            Debug.LogError("AStarPathfinding bulunamadı!");
            enabled = false;
            return;
        }
        
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        gameObject.layer = LayerMask.NameToLayer("Player");
        gameObject.tag = "Player";
        
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
        }
        collider.radius = 0.4f;
        collider.isTrigger = false;
        
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.mass = 1f;
        rb.drag = 2f;
        
        PhysicsMaterial2D material = new PhysicsMaterial2D("AgentMaterial");
        material.friction = 0.3f;
        material.bounciness = 0f;
        collider.sharedMaterial = material;
        
        Vector3 startPos = dungeonGenerator.GetStartPosition();
        transform.position = startPos;
        goalPosition = dungeonGenerator.GetGoalPosition();
        
        if (showDebugLogs)
        {
            Debug.Log($"Agent Başlangıç: {startPos}");
            Debug.Log($"Hedef: {goalPosition}");
        }
        
        CalculatePath();
        lastStuckCheckPosition = transform.position;
        isInitialized = true;
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        recalculateTimer += Time.deltaTime;
        if (recalculateTimer >= recalculatePathInterval)
        {
            CalculatePath();
            recalculateTimer = 0f;
        }
        
        if (currentPath == null || currentPath.Count == 0)
        {
            CalculatePath();
            return;
        }
        
        stuckTimer += Time.deltaTime;
        if (stuckTimer >= stuckDetectionTime)
        {
            float movedDistance = Vector3.Distance(transform.position, lastStuckCheckPosition);
            
            if (movedDistance < stuckDistanceThreshold)
            {
                Debug.LogWarning("Agent sıkıştı! Yolu yeniden hesaplıyorum...");
                
                if (currentPath != null && currentPath.Count > 0)
                {
                    currentWaypointIndex = Mathf.Min(currentWaypointIndex + 2, currentPath.Count - 1);
                }
                
                Vector2 randomPush = Random.insideUnitCircle.normalized;
                rb.velocity = randomPush * moveSpeed * 0.5f;
                
                CalculatePath();
                recalculateTimer = 0f;
            }
            
            lastStuckCheckPosition = transform.position;
            stuckTimer = 0f;
        }
    }
    
    void FixedUpdate()
    {
        if (!isInitialized || currentPath == null || currentPath.Count == 0)
        {
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, Time.fixedDeltaTime * 5f);
            return;
        }
        
        float distanceToGoal = Vector3.Distance(transform.position, goalPosition);
        
        if (distanceToGoal < 1.5f)
        {
            Vector2 directionToGoal = (goalPosition - transform.position).normalized;
            rb.velocity = directionToGoal * (moveSpeed * 0.7f);
            
            if (distanceToGoal < 0.4f)
            {
                Debug.Log("HEDEFE ULAŞILDI!");
                
                GameHUD hud = FindObjectOfType<GameHUD>();
                if (hud != null)
                {
                    hud.ShowWinScreen();
                }
                else
                {
                    Debug.LogError("GameHUD bulunamadı!");
                }
                
                rb.velocity = Vector2.zero;
                enabled = false;
            }
            return;
        }
        
        if (currentWaypointIndex >= currentPath.Count)
        {
            CalculatePath();
            return;
        }
        
        Vector3 currentWaypoint = pathfinding.GridToWorld(currentPath[currentWaypointIndex]);
        float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint);
        
        if (distanceToWaypoint < waypointReachDistance)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= currentPath.Count)
            {
                CalculatePath();
                return;
            }
        }
        
        int targetIndex = Mathf.Min(currentWaypointIndex + waypointLookahead, currentPath.Count - 1);
        Vector3 targetWaypoint = pathfinding.GridToWorld(currentPath[targetIndex]);
        
        Vector2 targetDirection = (targetWaypoint - transform.position).normalized;
        Vector2 desiredVelocity = targetDirection * moveSpeed;
        
        rb.velocity = Vector2.Lerp(rb.velocity, desiredVelocity, Time.fixedDeltaTime * 10f);
    }
    
    void CalculatePath()
    {
        currentPath = pathfinding.FindPath(transform.position, goalPosition);
        currentWaypointIndex = 0;
        
        if (currentPath != null && currentPath.Count > 0)
        {
            if (showDebugLogs)
                Debug.Log($"Yol hesaplandı: {currentPath.Count} adım");
            
            if (currentPath.Count > 1)
                currentWaypointIndex = 1;
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning("Yol bulunamadı!");
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showPath || currentPath == null || currentPath.Count < 2) return;
        if (pathfinding == null) return;
        
        Gizmos.color = Color.cyan;
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Vector3 start = pathfinding.GridToWorld(currentPath[i]);
            Vector3 end = pathfinding.GridToWorld(currentPath[i + 1]);
            Gizmos.DrawLine(start, end);
        }
        
        if (currentWaypointIndex < currentPath.Count)
        {
            Gizmos.color = Color.yellow;
            Vector3 currentWaypoint = pathfinding.GridToWorld(currentPath[currentWaypointIndex]);
            Gizmos.DrawWireSphere(currentWaypoint, 0.3f);
        }
        
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, goalPosition);
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (showDebugLogs)
            Debug.Log($"Çarpışma: {collision.gameObject.name}");
        
        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.name.Contains("Wall"))
        {
            Debug.LogWarning("Duvara çarpıldı!");
            currentWaypointIndex = Mathf.Min(currentWaypointIndex + 2, currentPath != null ? currentPath.Count - 1 : 0);
        }
    }
    
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.name.Contains("Wall"))
        {
            Vector2 pushDirection = (transform.position - collision.transform.position).normalized;
            rb.AddForce(pushDirection * 3f, ForceMode2D.Impulse);
        }
    }
}