using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController3D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float acceleration = 20f;
    public float airControl = 0.5f;
    public float jumpHeight = 2f;
    public float gravity = -20f;

    private CharacterController _cc;
    private Vector3 _velocity;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (_cc.center == Vector3.zero && Mathf.Approximately(_cc.height, 2f))
        {
            _cc.center = new Vector3(0, 0.6f, 0);  // Centrado balanceado
            _cc.radius = 0.8f;  // Radio ligeramente mayor
            _cc.height = 2.8f;  // Altura ajustada
            _cc.stepOffset = 0.3f;
            _cc.slopeLimit = 45f;
        }
    }

    void Update()
    {
        // Get camera-relative input
        Vector2 input = InputProvider.MoveAxis();
        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 camForward = Vector3.forward;
        Vector3 camRight = Vector3.right;
        if (Camera.main != null)
        {
            Vector3 f = Camera.main.transform.forward; f.y = 0f; f.Normalize();
            Vector3 r = Camera.main.transform.right; r.y = 0f; r.Normalize();
            camForward = f; camRight = r;
        }

        Vector3 desired = (camForward * input.y + camRight * input.x) * moveSpeed;

        // Smooth horizontal velocity
        Vector3 horizVel = new Vector3(_velocity.x, 0f, _velocity.z);
        float t = _cc.isGrounded ? 1f - Mathf.Exp(-acceleration * Time.deltaTime) : 1f - Mathf.Exp(-(acceleration * airControl) * Time.deltaTime);
        horizVel = Vector3.Lerp(horizVel, desired, t);

        // Apply gravity
        if (_cc.isGrounded)
        {
            _velocity.y = -2f; // keep grounded
            if (InputProvider.JumpDown())
            {
                _velocity.y = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
            }
        }
        else
        {
            _velocity.y += gravity * Time.deltaTime;
        }

        _velocity.x = horizVel.x; _velocity.z = horizVel.z;

        // Move
        _cc.Move(_velocity * Time.deltaTime);

        // Rotate towards movement
        Vector3 look = new Vector3(_velocity.x, 0, _velocity.z);
        if (look.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(look.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-10f * Time.deltaTime));
        }
    }
}
