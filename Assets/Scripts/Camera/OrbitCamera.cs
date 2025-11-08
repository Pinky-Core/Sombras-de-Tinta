using UnityEngine;

[DefaultExecutionOrder(10)]
public class OrbitCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 6f;
    public Vector2 pitchLimits = new Vector2(-25f, 70f);
    public float mouseSensitivity = 120f; // deg/sec
    public float smooth = 15f;
    public float collisionRadius = 0.25f;

    float _yaw;
    float _pitch = 15f;
    Vector3 _currentPos;
    Quaternion _currentRot;

    void Start()
    {
        if (target == null)
        {
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController3D>();
            if (player) target = player.transform;
        }
        if (Camera.main && Camera.main.transform != transform)
        {
            // Ensure this is the main camera if attached elsewhere
            var cam = GetComponent<Camera>();
            if (cam && !CompareTag("MainCamera")) gameObject.tag = "MainCamera";
        }

        // Initialize orbit angles from current transform
        Vector3 e = transform.eulerAngles;
        _yaw = e.y;
        _pitch = Mathf.Clamp(e.x, pitchLimits.x, pitchLimits.y);
        _currentPos = transform.position;
        _currentRot = transform.rotation;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Mouse input when right button is held
        if (InputProvider.RightMouseHeld())
        {
            Vector2 md = InputProvider.MouseDelta();
            _yaw += md.x * mouseSensitivity;
            _pitch -= md.y * mouseSensitivity;
            _pitch = Mathf.Clamp(_pitch, pitchLimits.x, pitchLimits.y);
        }

        Quaternion desiredRot = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 desiredPos = target.position - desiredRot * Vector3.forward * distance + Vector3.up * 1.5f;

        // Simple collision: spherecast from target to desired position
        if (Physics.SphereCast(target.position + Vector3.up * 1.5f, collisionRadius, (desiredPos - (target.position + Vector3.up * 1.5f)).normalized, out RaycastHit hit, Vector3.Distance(target.position + Vector3.up * 1.5f, desiredPos)))
        {
            desiredPos = hit.point + hit.normal * collisionRadius;
        }

        _currentRot = Quaternion.Slerp(_currentRot, desiredRot, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        _currentPos = Vector3.Lerp(_currentPos, desiredPos, 1f - Mathf.Exp(-smooth * Time.deltaTime));

        transform.SetPositionAndRotation(_currentPos, _currentRot);
    }
}
