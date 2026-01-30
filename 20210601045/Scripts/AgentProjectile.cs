using UnityEngine;

    public class AgentProjectile : MonoBehaviour
    {
        [HideInInspector] public float damage = 25f;
        [HideInInspector] public GameObject shooter;
    
        public GameObject hitEffectPrefab;
        
        void Start()
        {
       
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.1f;
            col.isTrigger = true;
        }
       
        gameObject.layer = LayerMask.NameToLayer("Default");
    }
    
        void OnTriggerEnter2D(Collider2D collision)
        {
           
            if (collision.gameObject == shooter)
                return;
            
            if (collision.CompareTag("Wall") || collision.name.Contains("Wall"))
            {
                SpawnHitEffect();
                Destroy(gameObject);
                return;
            }
            
            bool isEnemy = collision.CompareTag("Enemy") || 
                          collision.name.ToLower().Contains("enemy") || 
                          collision.name.ToLower().Contains("chaser") || 
                          collision.name.ToLower().Contains("patrol") ||
                          collision.name.ToLower().Contains("stationary");
            
            if (isEnemy)
            {
                EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damage);
                }
                
                SpawnHitEffect();
                Destroy(gameObject);
            }
        }
        
        void SpawnHitEffect()
        {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }
}
