using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays a camera intro using a list of fixed points defined in the scene.
/// Similar a un "rail" manual donde eliges cada posición/rotación que se mostrará.
/// </summary>
[RequireComponent(typeof(Camera))]
public class LevelIntroPath : MonoBehaviour
{
    [Header("Playback")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool disablePlayerControls = true;
    [SerializeField] private SmoothFollowCamera followCamera;
    [SerializeField] private List<CameraPathPoint> points = new List<CameraPathPoint>();

    [Header("Skip")]
    [SerializeField] private bool allowSkip = true;

    private Camera _cam;
    private bool _playing;
    private Transform _player;
    private Behaviour _playerMovement;
    private InkDrawer _inkDrawer;
    private Vector3 _startPos;
    private Quaternion _startRot;
    private float _startFov;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (followCamera == null)
        {
            followCamera = Camera.main ? Camera.main.GetComponent<SmoothFollowCamera>() : GetComponent<SmoothFollowCamera>();
        }
        CachePlayer();
    }

    private void Start()
    {
        if (playOnStart)
        {
            PlayIntro();
        }
    }

    public void PlayIntro()
    {
        if (_playing || points.Count == 0)
        {
            return;
        }

        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        _playing = true;
        _startPos = transform.position;
        _startRot = transform.rotation;
        _startFov = _cam.fieldOfView;

        if (followCamera != null)
        {
            followCamera.enabled = false;
        }

        if (disablePlayerControls)
        {
            DisablePlayerControl();
        }

        foreach (CameraPathPoint point in points)
        {
            Vector3 targetPos = point.Point != null ? point.Point.position : transform.position;
            Quaternion targetRot = point.Point != null ? point.Point.rotation : transform.rotation;

            if (point.LookAtTarget && point.LookTarget != null)
            {
                Vector3 lookPos = point.LookTarget.position + point.LookOffset;
                targetRot = Quaternion.LookRotation((lookPos - targetPos).normalized, Vector3.up);
            }

            float startFov = _cam.fieldOfView;
            float targetFov = point.OverrideFov ? point.TargetFov : startFov;

            yield return TweenTo(targetPos, targetRot, startFov, targetFov, point.TravelTime);
            if (point.HoldTime > 0f)
            {
                yield return WaitForSecondsOrSkip(point.HoldTime);
            }
        }

        // Return control to follow cam or original transform
        if (followCamera != null)
        {
            transform.position = followCamera.GetDesiredPosition();
            transform.rotation = followCamera.transform.rotation;
            _cam.fieldOfView = _startFov;
            followCamera.enabled = true;
        }
        else
        {
            yield return TweenTo(_startPos, _startRot, _cam.fieldOfView, _startFov, 0.5f);
        }

        if (disablePlayerControls)
        {
            EnablePlayerControl();
        }

        _playing = false;
    }

    private IEnumerator TweenTo(Vector3 targetPos, Quaternion targetRot, float fromFov, float toFov, float duration)
    {
        duration = Mathf.Max(0.01f, duration);
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (ShouldSkip()) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = SmootherStep(t);
            transform.position = Vector3.LerpUnclamped(startPos, targetPos, eased);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, eased);
            _cam.fieldOfView = Mathf.Lerp(fromFov, toFov, eased);
            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;
        _cam.fieldOfView = toFov;
    }

    private IEnumerator WaitForSecondsOrSkip(float time)
    {
        float timer = 0f;
        while (timer < time)
        {
            if (ShouldSkip()) yield break;
            timer += Time.deltaTime;
            yield return null;
        }
    }

    private bool ShouldSkip()
    {
        if (!allowSkip)
        {
            return false;
        }

        if (InputProvider.JumpDown() || InputProvider.ShootDown() || InputProvider.LeftMouseDown() || InputProvider.PauseDown())
        {
            StopAllCoroutines();
            StartCoroutine(ForceRestore());
            return true;
        }
        return false;
    }

    private IEnumerator ForceRestore()
    {
        if (followCamera != null)
        {
            transform.position = followCamera.GetDesiredPosition();
            transform.rotation = followCamera.transform.rotation;
            _cam.fieldOfView = _startFov;
            followCamera.enabled = true;
        }
        else
        {
            transform.position = _startPos;
            transform.rotation = _startRot;
            _cam.fieldOfView = _startFov;
        }

        EnablePlayerControl();
        _playing = false;
        yield return null;
    }

    private void CachePlayer()
    {
        var player3D = FindFirstObjectByType<PlayerController3D>();
        if (player3D != null)
        {
            _player = player3D.transform;
            _playerMovement = player3D.GetComponent<Behaviour>();
        }
        else
        {
            var playerSide = FindFirstObjectByType<PlayerControllerSide3D>();
            if (playerSide != null)
            {
                _player = playerSide.transform;
                _playerMovement = playerSide.GetComponent<Behaviour>();
            }
        }

        if (_player != null)
        {
            _inkDrawer = _player.GetComponent<InkDrawer>();
        }
    }

    private void DisablePlayerControl()
    {
        CachePlayer();
        if (_playerMovement != null) _playerMovement.enabled = false;
        if (_inkDrawer != null) _inkDrawer.enabled = false;
    }

    private void EnablePlayerControl()
    {
        if (_playerMovement != null) _playerMovement.enabled = true;
        if (_inkDrawer != null) _inkDrawer.enabled = true;
    }

    private static float SmootherStep(float t)
    {
        return t * t * t * (t * (6f * t - 15f) + 10f);
    }

    [System.Serializable]
    public class CameraPathPoint
    {
        [Tooltip("Transform que define la posición y rotación objetivo.")]
        public Transform Point;

        [Tooltip("Tiempo en segundos que tomará llegar a este punto.")]
        public float TravelTime = 1f;

        [Tooltip("Tiempo que se quedará parado una vez alcanzado el punto.")]
        public float HoldTime = 0.5f;

        [Tooltip("Forzar un FOV específico en este punto.")]
        public bool OverrideFov = false;
        public float TargetFov = 60f;

        [Tooltip("Si es true, ignorará la rotación del punto y mirará hacia el target indicado.")]
        public bool LookAtTarget = false;
        public Transform LookTarget;
        public Vector3 LookOffset;
    }
}
