using UnityEngine;

public class SmoothFollowCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 5, -8);  // Offset desde el jugador
    
    [Header("Smooth Movement")]
    public float followSpeed = 2f;  // Velocidad de seguimiento
    public float lookAheadDistance = 3f;  // Distancia de anticipación
    public float lookAheadSpeed = 1f;  // Velocidad de anticipación
    
    [Header("2D Perspective")]
    public bool use2DPerspective = true;
    public float fixedY = 0f;  // Y fijo para perspectiva 2D
    public float cameraAngle = 15f;  // Ángulo de la cámara (grados)
    
    [Header("Bounds")]
    public bool useBounds = false;
    public Vector2 boundsMin = new Vector2(-10, -10);
    public Vector2 boundsMax = new Vector2(10, 10);
    
    private Vector3 _currentVelocity;
    private Vector3 _lookAheadTarget;
    private Vector3 _targetPosition;
    
    void Start()
    {
        if (target == null)
        {
            var player = FindFirstObjectByType<PlayerController3D>();
            if (player != null) target = player.transform;
        }
        
        // Configurar cámara para perspectiva 2D
        if (use2DPerspective)
        {
            transform.rotation = Quaternion.Euler(cameraAngle, 0, 0);
        }
        
        // Posición inicial
        if (target != null)
        {
            _targetPosition = CalculateTargetPosition();
            transform.position = _targetPosition;
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // Calcular posición objetivo
        _targetPosition = CalculateTargetPosition();
        
        // Aplicar límites si están habilitados
        if (useBounds)
        {
            _targetPosition.x = Mathf.Clamp(_targetPosition.x, boundsMin.x, boundsMax.x);
            _targetPosition.z = Mathf.Clamp(_targetPosition.z, boundsMin.y, boundsMax.y);
        }
        
        // Movimiento suave hacia el objetivo
        transform.position = Vector3.SmoothDamp(transform.position, _targetPosition, ref _currentVelocity, 1f / followSpeed);
        
        // Mantener la rotación fija para perspectiva 2D
        if (use2DPerspective)
        {
            transform.rotation = Quaternion.Euler(cameraAngle, 0, 0);
        }
    }
    
    Vector3 CalculateTargetPosition()
    {
        Vector3 basePosition = target.position + offset;
        
        if (use2DPerspective)
        {
            // Para perspectiva 2D, mantener Y fijo y Z fijo
            basePosition.y = fixedY;
            basePosition.z = offset.z;  // Congelar el eje Z
        }
        
        // Calcular anticipación basada en el movimiento del jugador
        Vector3 playerVelocity = target.GetComponent<CharacterController>()?.velocity ?? Vector3.zero;
        Vector3 lookAhead = new Vector3(playerVelocity.x, 0, 0) * lookAheadDistance;  // Solo X para perspectiva 2D
        
        // Suavizar la anticipación
        _lookAheadTarget = Vector3.Lerp(_lookAheadTarget, lookAhead, lookAheadSpeed * Time.deltaTime);
        
        return basePosition + _lookAheadTarget;
    }
    
    // Posición deseada pública (incluye bounds si aplica)
    public Vector3 GetDesiredPosition()
    {
        var p = CalculateTargetPosition();
        if (useBounds)
        {
            p.x = Mathf.Clamp(p.x, boundsMin.x, boundsMax.x);
            p.z = Mathf.Clamp(p.z, boundsMin.y, boundsMax.y);
        }
        return p;
    }
    
    // Método para cambiar el objetivo dinámicamente
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    // Método para ajustar el offset dinámicamente
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
    
    // Método para ajustar la velocidad de seguimiento
    public void SetFollowSpeed(float speed)
    {
        followSpeed = speed;
    }
}
