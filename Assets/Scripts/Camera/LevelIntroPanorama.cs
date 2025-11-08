using System.Collections;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class LevelIntroPanorama : MonoBehaviour
{
    [Header("Setup")]
    public bool playOnStart = true;
    public Transform player;
    public SmoothFollowCamera followCamera;

    [Header("Path/Bounds")]
    public bool autoBounds = true;
    public LayerMask boundsMask = ~0;
    public float yHeight = 8f;
    public float zOffset = -10f;
    public float holdAtEnds = 0.75f;

    [Header("Timing")]
    public float zoomOutTime = 1.2f;
    public float panSpeed = 6f; // units/sec along X
    public float fullZoomHold = 1.2f;
    public float returnTime = 1.0f;

    [Header("Zoom total del nivel")]
    public float midFOV = 70f;       // zoom out medio
    public float fullFOV = 85f;      // FOV del zoom total (ajustable)
    public float fullZoomBack = 0f;  // Ajuste extra en Z al mostrar el nivel completo (negativo = alejar)

    [Header("Control")]
    public KeyCode skipKey = KeyCode.Space;

    Camera _cam;
    Vector3 _startPos;
    float _startFov;
    bool _playing;

    // Bloqueos durante la intro
    Component _playerMovement;
    InkDrawer _inkDrawer;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        if (!player)
        {
            var p = FindFirstObjectByType<PlayerController3D>();
            if (p) player = p.transform;
        }
        if (!followCamera)
        {
            followCamera = Camera.main ? Camera.main.GetComponent<SmoothFollowCamera>() : GetComponent<SmoothFollowCamera>();
        }
    }

    void Start()
    {
        if (playOnStart) StartIntro();
    }

    public void StartIntro()
    {
        if (_playing) return;
        StartCoroutine(PlayIntroCo());
    }


    IEnumerator PlayIntroCo()
    {
        if (!player) yield break;
        _playing = true;
        _startPos = transform.position;
        _startFov = _cam.fieldOfView;

        // Disable follow during the cinematic
        if (followCamera) followCamera.enabled = false;

        // Bloquear movimiento del jugador y dibujo
        _playerMovement = (Component)(player.GetComponent<PlayerController3D>() ?? (Component)player.GetComponent<PlayerControllerSide3D>());
        if (_playerMovement) ((Behaviour)_playerMovement).enabled = false;
        _inkDrawer = player.GetComponent<InkDrawer>();
        if (_inkDrawer) _inkDrawer.enabled = false;

        // Position camera near player start at configured height/offset
        Vector3 followLike = new Vector3(player.position.x, yHeight, (player.position.z + zOffset));
        transform.position = followLike;

        // Phase 1: zoom out to mid
        yield return TweenFOV(_cam.fieldOfView, midFOV, zoomOutTime);
        yield return new WaitForSeconds(holdAtEnds);

        // Determine bounds (minX/maxX)
        (float minX, float maxX, float centerX) = ComputeLevelBoundsX();
        if (float.IsNaN(minX) || float.IsNaN(maxX) || Mathf.Approximately(minX, maxX))
        {
            // Fallback: small pan around player
            minX = player.position.x - 10f;
            maxX = player.position.x + 10f;
            centerX = player.position.x;
        }

        // Phase 2: pan to the right across level (suavizado)
        Vector3 from = new Vector3(minX, yHeight, player.position.z + zOffset);
        Vector3 to = new Vector3(maxX, yHeight, player.position.z + zOffset);
        transform.position = from;
        float panDuration = Mathf.Max(0.01f, Mathf.Abs(maxX - minX) / Mathf.Max(0.01f, panSpeed));
        yield return TweenPosition(from, to, panDuration);
        yield return new WaitForSeconds(holdAtEnds);

        if (_ShouldSkip()) goto END;

        // Phase 3: zoom out to show whole level (approx) and center camera
        Vector3 center = new Vector3(centerX, yHeight, (player.position.z + zOffset) + fullZoomBack);
        yield return TweenPosition(transform.position, center, 0.9f);
        yield return TweenFOV(_cam.fieldOfView, fullFOV, 0.8f);
        yield return new WaitForSeconds(fullZoomHold);

        if (_ShouldSkip()) goto END;

        // Phase 4: return to player and restore settings (exactamente al destino del seguidor)
        Vector3 back = new Vector3(player.position.x, yHeight, player.position.z + zOffset);
        if (followCamera)
        {
            // Obtener la posición exacta que el seguidor usaría ahora mismo
            back = followCamera.GetDesiredPosition();
        }
        yield return TweenPosition(transform.position, back, returnTime);
        yield return TweenFOV(_cam.fieldOfView, _startFov, 0.6f);

        END:
        if (followCamera)
        {
            // Colocar exactamente en el punto del seguidor antes de habilitar para evitar salto
            transform.position = followCamera.GetDesiredPosition();
            followCamera.enabled = true;
        }

        // Rehabilitar control y dibujo
        if (_playerMovement) ((Behaviour)_playerMovement).enabled = true;
        if (_inkDrawer) _inkDrawer.enabled = true;

        _playing = false;
    }

    (float minX, float maxX, float centerX) ComputeLevelBoundsX()
    {
        if (!autoBounds)
        {
            return (player.position.x - 10f, player.position.x + 10f, player.position.x);
        }

        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        var rends = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        foreach (var r in rends)
        {
            if (!r.enabled) continue;
            var go = r.gameObject;
            // Ignore dynamic characters and temporary ink/indicators
            if (go.GetComponent<EnemyRedraw>() || go.GetComponent<CharacterController>()) continue;
            if (go.name.Contains("InkPiece") || go.name.Contains("AllyIndicator")) continue;
            var b = r.bounds;
            minX = Mathf.Min(minX, b.min.x);
            maxX = Mathf.Max(maxX, b.max.x);
        }
        if (float.IsPositiveInfinity(minX) || float.IsNegativeInfinity(maxX))
        {
            return (float.NaN, float.NaN, float.NaN);
        }
        return (minX, maxX, (minX + maxX) * 0.5f);
    }

    IEnumerator TweenFOV(float from, float to, float time)
    {
        float elapsed = 0f;
        while (elapsed < time && !_ShouldSkip())
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, time));
            float e = t * t * t * (t * (6f * t - 15f) + 10f); // smootherstep (amortigua el final)
            _cam.fieldOfView = Mathf.LerpUnclamped(from, to, e);
            yield return null;
        }
        _cam.fieldOfView = to;
    }

    IEnumerator TweenPosition(Vector3 from, Vector3 to, float time)
    {
        float elapsed = 0f;
        while (elapsed < time && !_ShouldSkip())
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, time));
            float e = t * t * t * (t * (6f * t - 15f) + 10f); // smootherstep (amortigua el final)
            transform.position = Vector3.LerpUnclamped(from, to, e);
            yield return null;
        }
        transform.position = to;
    }

    bool _ShouldSkip()
    {
        // Evitar UnityEngine.Input directo: usar solo InputProvider para compatibilidad con el nuevo Input System
        return InputProvider.JumpDown() || InputProvider.ShootDown() || InputProvider.LeftMouseDown() || InputProvider.PauseDown();
    }
}
