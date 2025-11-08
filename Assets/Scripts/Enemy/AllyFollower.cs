using UnityEngine;

public class AllyFollower : MonoBehaviour
{
    public float followDistance = 2.5f;
    public float speed = 3f;

    Transform _player;
    float _baseZ;

    void Start()
    {
        _player = UnityEngine.Object.FindAnyObjectByType<PlayerController3D>()?.transform;
        if (!_player) _player = UnityEngine.Object.FindAnyObjectByType<PlayerControllerSide3D>()?.transform;
        _baseZ = transform.position.z;
    }

    void Update()
    {
        if (!_player) return;
        float targetX = _player.position.x - Mathf.Sign(_player.position.x - transform.position.x) * followDistance;
        Vector3 target = new Vector3(targetX, transform.position.y, _baseZ);
        Vector3 dir = new Vector3(target.x - transform.position.x, 0f, 0f);
        float step = speed * Time.deltaTime;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Vector3 move = Vector3.ClampMagnitude(dir, step);
            transform.position += move;
            transform.position = new Vector3(transform.position.x, transform.position.y, _baseZ);
            Vector3 look = new Vector3(Mathf.Sign(_player.position.x - transform.position.x), 0f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look, Vector3.up), 1f - Mathf.Exp(-10f * Time.deltaTime));
        }
    }
}
