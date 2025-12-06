using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class EnemyChaser : MonoBehaviour
{
    public float speed = 3.5f;
    public float detectionRange = 10f;
    public float stopDistance = 1.25f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.0f;
    public float attackDamage = 1f;

    Transform _player;
    PlayerHealth _playerHealth;
    EnemyRedraw _redraw;
    float _baseZ;
    float _attackTimer;

    void Start()
    {
        _player = FindPlayer();
        CachePlayerHealth();
        _redraw = GetComponent<EnemyRedraw>();
        _baseZ = transform.position.z;
    }

    void Update()
    {
        if (_redraw && _redraw.CurrentState != EnemyRedraw.State.Normal) return;
        if (_player == null)
        {
            _player = FindPlayer();
            CachePlayerHealth();
            if (_player == null) return;
        }

        _attackTimer -= Time.deltaTime;

        float dist = Vector3.Distance(_player.position, transform.position);
        if (dist <= detectionRange && dist > stopDistance)
        {
            float dir = Mathf.Sign(_player.position.x - transform.position.x);
            Vector3 move = new Vector3(dir * speed * Time.deltaTime, 0f, 0f);
            transform.position += move;
            transform.position = new Vector3(transform.position.x, transform.position.y, _baseZ);
            Vector3 look = new Vector3(dir, 0f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look, Vector3.up), 1f - Mathf.Exp(-10f * Time.deltaTime));
        }

        if (dist <= attackRange && _attackTimer <= 0f)
        {
            DealDamageToPlayer();
            _attackTimer = attackCooldown;
        }
    }

    Transform FindPlayer()
    {
        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null) return tagged.transform;
        var p = FindFirstObjectByType<PlayerController3D>();
        if (p != null) return p.transform;
        var p2 = FindFirstObjectByType<PlayerControllerSide3D>();
        return p2 != null ? p2.transform : null;
    }

    void CachePlayerHealth()
    {
        if (_player == null) return;
        _playerHealth = _player.GetComponent<PlayerHealth>();
    }

    void DealDamageToPlayer()
    {
        if (_playerHealth == null)
        {
            CachePlayerHealth();
        }
        _playerHealth?.ApplyDamage(attackDamage);
    }
}
