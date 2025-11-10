using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol/Chase")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    public float chaseRange = 6f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.0f;
    public float attackDamage = 1f;
    public Vector3 localPointA = new Vector3(-3f, 0f, 0f);
    public Vector3 localPointB = new Vector3(3f, 0f, 0f);

    [Header("Health")]
    public float maxHealth = 3f;
    public float currentHealth = 3f;

    [Header("Animation")] 
    public Animator animator;
    public string speedParam = "Speed";    // float
    public string chasingParam = "Chasing"; // bool
    public string hitTrigger = "Hit";       // trigger
    public string attackTrigger = "Attack"; // trigger
    public string dieTrigger = "Die";       // trigger

    Transform _player;
    EnemyRedraw _redraw;
    Vector3 _startPos;
    float _baseZ;
    bool _toB = true;
    float _attackTimer;
    bool _dead;
    PlayerHealth _playerHealth;

    void Start()
    {
        _player = UnityEngine.Object.FindAnyObjectByType<PlayerController3D>()?.transform;
        if (!_player) _player = UnityEngine.Object.FindAnyObjectByType<PlayerControllerSide3D>()?.transform;
        _redraw = GetComponent<EnemyRedraw>();
        _startPos = transform.position;
        _baseZ = transform.position.z;
        if (!animator) animator = GetComponentInChildren<Animator>();
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        CachePlayerHealth();
    }

    void Update()
    {
        if (_dead) return;
        if (_redraw && (_redraw.CurrentState == EnemyRedraw.State.Dead || 
                       _redraw.CurrentState == EnemyRedraw.State.Platform || 
                       _redraw.CurrentState == EnemyRedraw.State.Ally)) return;

        _attackTimer -= Time.deltaTime;

        Vector3 target;
        bool hasPlayer = _player != null;
        bool chasing = hasPlayer && Vector3.Distance(_player.position, transform.position) <= chaseRange;
        if (chasing)
        {
            target = new Vector3(_player.position.x, transform.position.y, _baseZ);
        }
        else
        {
            Vector3 pA = _startPos + localPointA;
            Vector3 pB = _startPos + localPointB;
            target = _toB ? pB : pA;
            target.z = _baseZ;
            if (Vector3.Distance(new Vector3(transform.position.x, 0, 0), new Vector3(target.x, 0, 0)) < 0.1f)
            {
                _toB = !_toB;
            }
        }

        Vector3 dir = new Vector3(target.x - transform.position.x, 0f, 0f);
        float useSpeed = chasing ? runSpeed : walkSpeed;
        float step = useSpeed * Time.deltaTime;
        float moveAmount = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Vector3 move = Vector3.ClampMagnitude(dir, step);
            transform.position += move; moveAmount = Mathf.Abs(move.x) / Mathf.Max(Time.deltaTime, 0.0001f);
            transform.position = new Vector3(transform.position.x, transform.position.y, _baseZ);
            Vector3 look = new Vector3(Mathf.Sign(dir.x), 0f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look, Vector3.up), 1f - Mathf.Exp(-10f * Time.deltaTime));
        }

        // Animación básica
        if (animator)
        {
            if (!string.IsNullOrEmpty(speedParam)) animator.SetFloat(speedParam, moveAmount);
            if (!string.IsNullOrEmpty(chasingParam)) animator.SetBool(chasingParam, chasing);
        }

        // Ataque si está cerca
        if (hasPlayer && chasing)
        {
            float distX = Mathf.Abs(_player.position.x - transform.position.x);
            if (distX <= attackRange && _attackTimer <= 0f)
            {
                if (animator && !string.IsNullOrEmpty(attackTrigger)) animator.SetTrigger(attackTrigger);
                DealDamageToPlayer();
                _attackTimer = attackCooldown;
            }
        }
    }

    public bool ApplyDamage(float amount)
    {
        if (_dead) return false;
        currentHealth -= Mathf.Abs(amount);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (animator && !string.IsNullOrEmpty(hitTrigger)) animator.SetTrigger(hitTrigger);
        if (currentHealth <= 0f)
        {
            Die();
            return true;
        }
        return false;
    }

    void Die()
    {
        _dead = true;
        if (animator && !string.IsNullOrEmpty(dieTrigger)) animator.SetTrigger(dieTrigger);
        var col = GetComponent<Collider>(); if (col) col.enabled = false;
        
        // Si tiene EnemyRedraw, usar su método de muerte para generar partículas
        if (_redraw && _redraw.CanKill)
        {
            _redraw.ApplyDeath();
        }
        else
        {
            // Destruir tras 2s si no tiene redraw
            Destroy(gameObject, 2f);
        }
    }

    void DealDamageToPlayer()
    {
        if (_player == null)
        {
            var playerComp = FindFirstObjectByType<PlayerController3D>();
            if (playerComp != null) _player = playerComp.transform;
        }

        CachePlayerHealth();

        if (_playerHealth != null)
        {
            _playerHealth.ApplyDamage(attackDamage);
        }
    }

    void CachePlayerHealth()
    {
        if (_player == null)
        {
            return;
        }

        if (_playerHealth == null || _playerHealth.gameObject != _player.gameObject)
        {
            _playerHealth = _player.GetComponent<PlayerHealth>();
        }
    }

    public bool IsDead => _dead;
}
