using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class EnemyShooter : MonoBehaviour
{
    public float detectionRange = 12f;
    public float fireCooldown = 1.5f;
    public float projectileSpeed = 12f;
    public float projectileLifetime = 4f;
    public Color projectileColor = new Color(0.2f, 0.2f, 0.2f);

    Transform _player;
    EnemyRedraw _redraw;
    float _cooldown;
    float _baseZ;

    void Start()
    {
        _player = FindFirstObjectByType<PlayerController3D>()?.transform;
        if (!_player) _player = FindFirstObjectByType<PlayerControllerSide3D>()?.transform;
        _redraw = GetComponent<EnemyRedraw>();
        _baseZ = transform.position.z;
    }

    void Update()
    {
        if (_redraw && _redraw.CurrentState != EnemyRedraw.State.Normal) return;
        if (!_player) return;

        _cooldown -= Time.deltaTime;
        float dist = Vector3.Distance(_player.position, transform.position);
        if (dist <= detectionRange)
        {
            // Face player on X axis
            float dir = Mathf.Sign(_player.position.x - transform.position.x);
            Vector3 look = new Vector3(dir, 0f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look, Vector3.up), 1f - Mathf.Exp(-10f * Time.deltaTime));

            if (_cooldown <= 0f)
            {
                Fire(dir);
                _cooldown = fireCooldown;
            }
        }

        // keep Z plane
        if (Mathf.Abs(transform.position.z - _baseZ) > 0.001f)
        {
            var p = transform.position; p.z = _baseZ; transform.position = p;
        }
    }

    void Fire(float dir)
    {
        var proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        proj.name = "EnemyBullet";
        proj.transform.position = transform.position + new Vector3(dir * 0.6f, 0.6f, 0f);
        proj.transform.localScale = Vector3.one * 0.2f;
        var rend = proj.GetComponent<Renderer>();
        if (rend)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = projectileColor;
            rend.material = mat;
        }
        var col = proj.GetComponent<Collider>();
        col.isTrigger = true;
        var rb = proj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearVelocity = new Vector3(dir * projectileSpeed, 0f, 0f);
        Destroy(proj, projectileLifetime);
    }
}
