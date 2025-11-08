using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class EnemyChaser : MonoBehaviour
{
    public float speed = 3.5f;
    public float detectionRange = 10f;
    public float stopDistance = 1.25f;

    Transform _player;
    EnemyRedraw _redraw;
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
    }
}
