using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CapsuleCollider))]
public class EnemyPatrol : MonoBehaviour
{
    // -------------------------
    // CONFIGURACIÓN PRINCIPAL
    // -------------------------
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    public float chaseRange = 6f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.0f;

    [Header("Damage")]
    public float attackDamage = 1f;

    [Header("Patrol Points")]
    public Vector3 localPointA = new Vector3(-3f, 0f, 0f);
    public Vector3 localPointB = new Vector3(3f, 0f, 0f);

    [Header("Health")]
    public float maxHealth = 3f;
    public float currentHealth = 3f;

    [Header("Animation")]
    public Animator animator;
    public string speedParam = "Speed";
    public string chasingParam = "Chasing";
    public string hitTrigger = "Hit";
    public string attackTrigger = "Attack";
    public string dieTrigger = "Die";

    // -------------------------
    // FLAGS DE MEJORAS
    // -------------------------
    [Header("AI Enhancements (ON/OFF)")]
    public bool useLineOfSight = true;
    public bool usePrediction = true;
    public bool useZigZag = true;
    public bool useTelegraphedAttack = true;
    public bool useKnockback = true;
    public bool useLimitedChase = true;
    public bool useRandomPatrol = false;

    [Header("Enhancements Settings")]
    public float zigzagAmount = 0.4f;
    public float zigzagSpeed = 6f;
    public float predictionLead = 0.3f;
    public float knockbackForce = 0.3f;
    public float maxChaseTime = 5f;
    public float randomPatrolRange = 2f;

    // -------------------------
    // INTERNOS
    // -------------------------
    enum State { Patrol, Chase, Attack, Stunned, Dead }
    State state = State.Patrol;

    Transform _player;
    PlayerHealth _playerHealth;
    EnemyRedraw _redraw;

    Vector3 _startPos;
    float _baseZ;
    bool _toB = true;
    float _attackTimer;
    float _chaseTimer;
    bool _dead;

    Vector3 _currentRandomPoint;

    void Start()
    {
        _player = FindAnyObjectByType<PlayerController3D>()?.transform;
        if (!_player) _player = FindAnyObjectByType<PlayerControllerSide3D>()?.transform;

        _redraw = GetComponent<EnemyRedraw>();
        _startPos = transform.position;
        _baseZ = transform.position.z;

        if (!animator) animator = GetComponentInChildren<Animator>();

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        CachePlayerHealth();

        if (useRandomPatrol)
            GenerateRandomPatrolPoint();
    }

    void Update()
    {
        if (_dead) return;

        _attackTimer -= Time.deltaTime;

        switch (state)
        {
            case State.Patrol: PatrolLogic(); break;
            case State.Chase: ChaseLogic(); break;
            case State.Attack: AttackLogic(); break;
            case State.Stunned: break;
        }
    }

    // -------------------------
    //  PATROL
    // -------------------------
    void PatrolLogic()
    {
        Vector3 target = useRandomPatrol ? _currentRandomPoint : GetFixedPatrolPoint();

        MoveTowards(target, walkSpeed);

        if (IsPlayerDetected())
        {
            state = State.Chase;
            _chaseTimer = 0;
        }
    }

    Vector3 GetFixedPatrolPoint()
    {
        Vector3 pA = _startPos + localPointA;
        Vector3 pB = _startPos + localPointB;
        Vector3 target = _toB ? pB : pA;
        target.z = _baseZ;

        if (Mathf.Abs(transform.position.x - target.x) < 0.1f)
            _toB = !_toB;

        return target;
    }

    void GenerateRandomPatrolPoint()
    {
        float offset = Random.Range(-randomPatrolRange, randomPatrolRange);
        _currentRandomPoint = _startPos + new Vector3(offset, 0, 0);
    }

    // -------------------------
    //  CHASE
    // -------------------------
    void ChaseLogic()
    {
        _chaseTimer += Time.deltaTime;

        if (useLimitedChase && _chaseTimer >= maxChaseTime)
        {
            state = State.Patrol;
            return;
        }

        Vector3 target = GetChaseTarget();

        MoveTowards(target, runSpeed);

        float dist = Mathf.Abs(_player.position.x - transform.position.x);
        if (dist <= attackRange)
        {
            state = State.Attack;
        }

        if (!IsPlayerDetected())
        {
            state = State.Patrol;
        }
    }

    Vector3 GetChaseTarget()
    {
        if (!_player) return transform.position;

        Vector3 predicted = _player.position;

        if (usePrediction)
            predicted += _player.GetComponent<Rigidbody>()?.velocity * predictionLead ?? Vector3.zero;

        // Zigzag opcional
        if (useZigZag)
            predicted.x += Mathf.Sin(Time.time * zigzagSpeed) * zigzagAmount;

        return new Vector3(predicted.x, transform.position.y, _baseZ);
    }

    // -------------------------
    //  ATTACK
    // -------------------------
    void AttackLogic()
    {
        if (_attackTimer > 0)
        {
            state = State.Chase;
            return;
        }

        if (animator && attackTrigger != "")
            animator.SetTrigger(attackTrigger);

        _attackTimer = attackCooldown;

        if (useTelegraphedAttack)
            StartCoroutine(TelegraphedAttack());
        else
            DealDamageToPlayer();

        state = State.Chase;
    }

    IEnumerator TelegraphedAttack()
    {
        yield return new WaitForSeconds(0.35f);
        DealDamageToPlayer();
    }

    // -------------------------
    // MOVIMIENTO
    // -------------------------
    void MoveTowards(Vector3 target, float speed)
    {
        Vector3 dir = new Vector3(target.x - transform.position.x, 0, 0);
        if (dir.sqrMagnitude < 0.0001f)
        {
            if (useRandomPatrol) GenerateRandomPatrolPoint();
            return;
        }

        float step = speed * Time.deltaTime;
        Vector3 move = Vector3.ClampMagnitude(dir, step);
        transform.position += move;
        transform.position = new Vector3(transform.position.x, transform.position.y, _baseZ);

        // Giro
        Vector3 look = new Vector3(Mathf.Sign(dir.x), 0, 0);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(look, Vector3.up),
            1f - Mathf.Exp(-10f * Time.deltaTime)
        );

        // Animaciones
        if (animator && speedParam != "")
            animator.SetFloat(speedParam, Mathf.Abs(move.x) / Mathf.Max(Time.deltaTime, 0.0001f));
    }

    // -------------------------
    // DETECCIÓN
    // -------------------------
    bool IsPlayerDetected()
    {
        if (!_player) return false;

        float dist = Vector3.Distance(_player.position, transform.position);
        if (dist > chaseRange) return false;

        if (!useLineOfSight) return true;

        return HasLineOfSight();
    }

    bool HasLineOfSight()
    {
        Vector3 origin = transform.position + Vector3.up * 0.8f;
        Vector3 dest = _player.position + Vector3.up * 0.8f;
        Vector3 dir = (dest - origin).normalized;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, chaseRange))
            return hit.collider.CompareTag("Player");

        return false;
    }

    // -------------------------
    // DAMAGE
    // -------------------------
    public bool ApplyDamage(float amount)
    {
        if (_dead) return false;

        currentHealth -= Mathf.Abs(amount);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (animator && hitTrigger != "")
            animator.SetTrigger(hitTrigger);

        if (useKnockback)
            transform.position -= transform.forward * knockbackForce;

        if (currentHealth <= 0)
        {
            Die();
            return true;
        }

        return false;
    }

    void DealDamageToPlayer()
    {
        CachePlayerHealth();
        _playerHealth?.ApplyDamage(attackDamage);
    }

    void CachePlayerHealth()
    {
        if (!_player) return;
        if (_playerHealth == null || _playerHealth.gameObject != _player.gameObject)
            _playerHealth = _player.GetComponent<PlayerHealth>();
    }

    // -------------------------
    // DEATH
    // -------------------------
    void Die()
    {
        _dead = true;
        state = State.Dead;

        if (animator && dieTrigger != "")
            animator.SetTrigger(dieTrigger);

        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        if (_redraw && _redraw.CanKill)
            _redraw.ApplyDeath();
        else
            Destroy(gameObject, 2f);
    }

    public bool IsDead => _dead;
}
