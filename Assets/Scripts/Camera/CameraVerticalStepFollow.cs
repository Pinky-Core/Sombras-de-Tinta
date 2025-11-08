using UnityEngine;

// Este componente actualiza SmoothFollowCamera.fixedY para que la cámara
// suba/baje solo cuando el jugador cambia de plataforma (no por saltos).
// Mantiene un "baseGroundY" aceptado y lo actualiza con umbrales y coyote time.
[DefaultExecutionOrder(5)]
public class CameraVerticalStepFollow : MonoBehaviour
{
    public SmoothFollowCamera followCam;
    public Transform target;

    [Header("Altura y Raycast")]
    public float heightAboveGround = 8f;
    public float rayOriginHeight = 1.5f;
    public float rayDistance = 50f;
    public LayerMask groundMask = ~0;

    [Header("Detección de plataformas")]
    public float minRiseStep = 0.5f;   // subir si el suelo sube al menos esto
    public float minFallStep = 0.5f;   // bajar si el suelo baja al menos esto
    public float coyoteTime = 0.08f;   // tolerancia tras perder grounded

    [Header("Suavizado")]
    public float riseLerp = 6f;        // rapidez al subir
    public float fallLerp = 6f;        // rapidez al bajar

    CharacterController _cc;
    float _acceptedGroundY;
    float _currentCamY;
    float _lastGroundedTime;

    void Awake()
    {
        if (!followCam) followCam = GetComponent<SmoothFollowCamera>();
        if (!target && followCam && followCam.target) target = followCam.target;
        if (target) _cc = target.GetComponent<CharacterController>();
    }

    void Start()
    {
        SampleGround(out _acceptedGroundY);
        _currentCamY = _acceptedGroundY + heightAboveGround;
        ApplyToCamera();
    }

    void Update()
    {
        if (!followCam || !target) return;

        // grounded/coyote
        bool grounded = _cc ? _cc.isGrounded : true;
        if (grounded) _lastGroundedTime = Time.time;
        bool coyote = (Time.time - _lastGroundedTime) <= coyoteTime;

        // muestreo suelo
        if (SampleGround(out float groundY))
        {
            float delta = groundY - _acceptedGroundY;
            if ((grounded || coyote))
            {
                if (delta >= minRiseStep)
                {
                    _acceptedGroundY = groundY;
                }
                else if (delta <= -minFallStep)
                {
                    _acceptedGroundY = groundY;
                }
            }
        }

        float targetCamY = _acceptedGroundY + heightAboveGround;
        float lerpSpeed = targetCamY > _currentCamY ? riseLerp : fallLerp;
        _currentCamY = Mathf.Lerp(_currentCamY, targetCamY, 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime));

        ApplyToCamera();
    }

    void ApplyToCamera()
    {
        followCam.fixedY = _currentCamY;
    }

    bool SampleGround(out float groundY)
    {
        groundY = default;
        Vector3 origin = target.position + Vector3.up * rayOriginHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundY = hit.point.y;
            return true;
        }
        return false;
    }
}
