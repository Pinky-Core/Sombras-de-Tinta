using UnityEngine;

/// <summary>
/// Fuerza al Animator a avanzar a una tasa fija de frames (look 2D/stop-motion).
/// Si el modo manual da problemas, deja useManualStepping en false para solo escalar la velocidad.
/// </summary>
[DefaultExecutionOrder(900)]
public class AnimatorFrameStepper : MonoBehaviour
{
    [Tooltip("Frames por segundo deseados para el avance de las animaciones.")]
    public int targetFps = 24;

    [Tooltip("Limite para evitar saltos muy grandes si cae el framerate (solo en modo manual).")]
    public float maxDeltaClamp = 0.1f;

    [Header("Modo")]
    [Tooltip("Off = modo seguro (solo escala speed). On = stepping manual (efecto stop-motion mas marcado).")]
    public bool useManualStepping = false;

    [Tooltip("Si esta activo, se simulan todos los frames caidos en un unico salto (corta frames). Si esta desactivado, se avanza paso a paso.")]
    public bool batchStepsInsteadOfWhile = true;

    private Animator _animator;
    private float _accum;
    private float _prevSpeed = 1f;
    private AnimatorCullingMode _prevCulling;
    private AnimatorUpdateMode _prevUpdateMode;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            enabled = false;
            return;
        }

        CacheAnimator();
        ApplyMode();
    }

    private void OnEnable()
    {
        if (_animator == null) return;
        ApplyMode();
    }

    private void OnDisable()
    {
        RestoreAnimator();
    }

    private void LateUpdate()
    {
        if (_animator == null || targetFps <= 0 || !useManualStepping)
            return;

        float step = 1f / Mathf.Max(1, targetFps);
        _accum += Mathf.Min(Time.deltaTime, maxDeltaClamp);

        if (batchStepsInsteadOfWhile)
        {
            // Avanza la animacion en un unico salto equivalente a todos los frames acumulados.
            int framesToSimulate = Mathf.FloorToInt(_accum / step);
            if (framesToSimulate > 0)
            {
                float dt = framesToSimulate * step;
                _accum -= dt;
                _animator.Update(dt);
            }
        }
        else
        {
            // Modo clasico: avanza un paso cada vez (mas suave, menos efecto stop-motion).
            while (_accum >= step)
            {
                _animator.Update(step);
                _accum -= step;
            }
        }
    }

    private void CacheAnimator()
    {
        _prevSpeed = _animator.speed;
        _prevCulling = _animator.cullingMode;
        _prevUpdateMode = _animator.updateMode;
    }

    private void ApplyMode()
    {
        _accum = 0f;

        if (useManualStepping)
        {
            _animator.updateMode = AnimatorUpdateMode.Normal;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _animator.speed = 0f;
        }
        else
        {
            // Modo seguro: solo ajustar speed para aproximar FPS objetivo.
            _animator.updateMode = AnimatorUpdateMode.Normal;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            float refRate = Application.targetFrameRate > 0 ? Application.targetFrameRate : 60f;
            _animator.speed = Mathf.Max(0.01f, (float)targetFps / refRate);
        }
    }

    private void RestoreAnimator()
    {
        if (_animator == null) return;
        _animator.speed = _prevSpeed;
        _animator.cullingMode = _prevCulling;
        _animator.updateMode = _prevUpdateMode;
    }
}
