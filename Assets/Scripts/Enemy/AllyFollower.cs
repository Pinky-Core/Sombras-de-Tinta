using UnityEngine;

public class AllyFollower : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Asignar manualmente el objetivo a seguir. Si está vacío, buscará al jugador.")]
    public Transform manualTarget;

    public float followDistance = 2.5f;
    public float speed = 3f;
    public float groundCheckOffset = 0.5f;
    public float groundCheckDistance = 2.5f;
    public LayerMask groundMask = Physics.DefaultRaycastLayers;
    [Header("Failsafe")]
    [Tooltip("Si está más lejos que esto del jugador, se teletransporta cerca.")]
    public float warpDistance = 25f;
    [Tooltip("Offset al teletransportar (al lado del jugador en X).")]
    public float warpSideOffset = 1.5f;
    [Tooltip("Altura al teletransportar.")]
    public float warpHeightOffset = 0.5f;
    [Tooltip("Y mínima; si cae por debajo, se teletransporta al jugador.")]
    public float fallThresholdY = -10f;

    Transform _player;
    float _baseZ;
    Rigidbody _rb;
    Vector3 _pendingTarget;
    bool _hasPendingTarget;

    void Start()
    {
        _baseZ = transform.position.z;
        SetTarget(manualTarget != null ? manualTarget : FindPlayer());

        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody>();
        }
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.useGravity = true;
    }

    void Update()
    {
        if (_player == null)
        {
            SetTarget(manualTarget != null ? manualTarget : FindPlayer());
            if (_player == null) return;
        }

        Vector3 toPlayer = _player.position - transform.position;
        Vector3 flatDir = new Vector3(toPlayer.x, 0f, toPlayer.z);
        float distance = flatDir.magnitude;
        Vector3 desired = _player.position;

        if (distance > 0.001f)
        {
            Vector3 offset = flatDir.normalized * followDistance;
            desired = _player.position - offset;
        }

        desired.z = _baseZ; // bloquear al plano
        desired.y = transform.position.y;

        Vector3 moveDir = desired - transform.position;
        moveDir.y = 0f;
        float step = speed * Time.deltaTime;
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Vector3 targetPos = Vector3.MoveTowards(transform.position, desired, step);
            targetPos.z = _baseZ;
            QueueMovement(targetPos);
            Vector3 look = new Vector3(Mathf.Sign(_player.position.x - transform.position.x), 0f, Mathf.Sign(_player.position.z - transform.position.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look, Vector3.up), 1f - Mathf.Exp(-10f * Time.deltaTime));
        }

        // Failsafe: si está demasiado lejos o cayó, reubicar junto al jugador
        float sqrDist = (_player.position - transform.position).sqrMagnitude;
        if (sqrDist > warpDistance * warpDistance || transform.position.y < fallThresholdY)
        {
            Vector3 side = new Vector3(Mathf.Sign(_player.position.x - transform.position.x), 0f, 0f);
            if (side.sqrMagnitude < 0.001f) side = Vector3.right;
            Vector3 warpPos = _player.position - side.normalized * warpSideOffset;
            warpPos.y = _player.position.y + warpHeightOffset;
            warpPos.z = _baseZ;
            if (_rb) _rb.position = warpPos; else transform.position = warpPos;
            _pendingTarget = warpPos;
        }
    }

    void FixedUpdate()
    {
        if (_hasPendingTarget)
        {
            ApplyMovement(_pendingTarget);
            _hasPendingTarget = false;
        }
    }

    public void SetTarget(Transform target)
    {
        _player = target;
        if (_player != null)
        {
            _baseZ = _player.position.z;
        }
    }

    Transform FindPlayer()
    {
        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null) return tagged.transform;

        var p = UnityEngine.Object.FindAnyObjectByType<PlayerController3D>();
        if (p != null) return p.transform;
        var p2 = UnityEngine.Object.FindAnyObjectByType<PlayerControllerSide3D>();
        return p2 != null ? p2.transform : null;
    }

    void QueueMovement(Vector3 targetPos, bool snapOnly = false)
    {
        _pendingTarget = targetPos;
        _hasPendingTarget = true;
        if (snapOnly && _rb == null)
        {
            // Si no hay rigidbody, aplicar de inmediato para el snap
            ApplyMovement(targetPos, true);
            _hasPendingTarget = false;
        }
    }

    void ApplyMovement(Vector3 targetPos, bool snapOnly = false)
    {
        if (_rb)
        {
            Vector3 next = Vector3.MoveTowards(_rb.position, targetPos, speed * Time.fixedDeltaTime);
            next.z = _baseZ;
            _rb.MovePosition(next);
        }
        else
        {
            transform.position = targetPos;
        }
    }
}
