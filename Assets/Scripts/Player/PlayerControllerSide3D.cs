using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerControllerSide3D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float acceleration = 20f;
    public float airControl = 0.6f;
    public float jumpHeight = 2f;
    public float gravity = -20f;

    [Header("2.5D Settings")]
    public bool lockZ = true;
    public float fixedZ = 0f;
    public bool faceMoveDirection = true;

    private CharacterController _cc;
    private Vector3 _vel;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (lockZ) fixedZ = transform.position.z;
        if (_cc.center == Vector3.zero && Mathf.Approximately(_cc.height, 2f))
        {
            _cc.center = new Vector3(0, 1f, 0);
            _cc.radius = 0.35f;
            _cc.height = 2f;
            _cc.stepOffset = 0.3f;
            _cc.slopeLimit = 45f;
        }
    }

    void Update()
    {
        // Only horizontal (X) input for side-scroller
        float h = Mathf.Clamp(InputProvider.MoveAxis().x, -1f, 1f);
        if (InkDrawer.IsDrawing) h = 0f; // bloquear movimiento en X mientras se dibuja
        float accel = _cc.isGrounded ? acceleration : acceleration * airControl;
        float t = 1f - Mathf.Exp(-accel * Time.deltaTime);
        float desiredX = h * moveSpeed;
        _vel.x = Mathf.Lerp(_vel.x, desiredX, t);

        if (_cc.isGrounded)
        {
            _vel.y = -2f;
            if (InputProvider.JumpDown())
            {
                _vel.y = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
            }
        }
        else
        {
            _vel.y += gravity * Time.deltaTime;
        }

        Vector3 motion = new Vector3(_vel.x, _vel.y, 0f) * Time.deltaTime;
        _cc.Move(motion);

        if (lockZ)
        {
            var p = transform.position; p.z = fixedZ; transform.position = p;
        }

        if (faceMoveDirection && Mathf.Abs(_vel.x) > 0.01f)
        {
            Vector3 look = new Vector3(Mathf.Sign(_vel.x), 0f, 0f);
            Quaternion targetRot = Quaternion.LookRotation(look, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-10f * Time.deltaTime));
        }
    }
}
