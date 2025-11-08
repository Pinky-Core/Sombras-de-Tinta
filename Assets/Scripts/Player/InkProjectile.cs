using UnityEngine;

public class InkProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float lifetime = 3f;
    public float damage = 1f;
    public LayerMask enemyLayer = -1;  // Todos los layers por defecto
    public float impactForce = 5f;
    
    private bool _hasHit = false;
    private Rigidbody _rb;
    
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        // Destruir automáticamente después del tiempo de vida
        Destroy(gameObject, lifetime);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (_hasHit) return;
        
        // Verificar si es un enemigo
        var enemy = other.GetComponent<EnemyRedraw>();
        if (enemy != null && enemy.CurrentState == EnemyRedraw.State.Normal)
        {
            _hasHit = true;
            
            // Aplicar fuerza de impacto
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
            }
            
            // Matar enemigo
            var player = FindFirstObjectByType<PlayerShooting>();
            if (player != null)
            {
                // Dar tinta al jugador por matar enemigo usando el método público
                var inkDrawer = player.GetComponent<InkDrawer>();
                if (inkDrawer != null)
                {
                    inkDrawer.RegenerateInk(5f);  // Bonus de tinta por matar enemigo
                }
            }
            
            // Destruir enemigo
            Destroy(enemy.gameObject);
            
            // Destruir proyectil después del impacto
            Destroy(gameObject);
        }
        else if (!other.CompareTag("Player") && !other.CompareTag("InkPiece"))
        {
            // Si choca con algo que no es el jugador o tinta, destruir el proyectil
            _hasHit = true;
            Destroy(gameObject);
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (_hasHit) return;
        
        // Si choca con algo que no es un enemigo, destruir el proyectil
        var enemy = collision.collider.GetComponent<EnemyRedraw>();
        if (enemy == null)
        {
            _hasHit = true;
            Destroy(gameObject);
        }
    }
}
