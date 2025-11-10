using System.Collections;
using UnityEngine;

/// <summary>
/// Provides a lightweight camera shake effect that other systems can trigger globally.
/// </summary>
[DisallowMultipleComponent]
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [SerializeField, Tooltip("Duration used when callers do not specify custom values.")]
    private float defaultDuration = 0.25f;

    [SerializeField, Tooltip("Max positional offset when using defaults.")]
    private float defaultIntensity = 0.4f;

    [SerializeField, Tooltip("Animation curve that controls how the shake fades over time (0-1).")]
    private AnimationCurve falloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private Transform cachedTransform;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        cachedTransform = transform;
        originalLocalPos = cachedTransform.localPosition;
        originalLocalRot = cachedTransform.localRotation;
    }

    private void OnDisable()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        if (cachedTransform != null)
        {
            cachedTransform.localPosition = originalLocalPos;
            cachedTransform.localRotation = originalLocalRot;
        }
    }

    public void Shake()
    {
        Shake(defaultDuration, defaultIntensity);
    }

    public void Shake(float duration, float intensity)
    {
        if (cachedTransform == null)
        {
            return;
        }

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
        }

        shakeRoutine = StartCoroutine(ShakeRoutine(Mathf.Max(0.01f, duration), Mathf.Max(0f, intensity)));
    }

    private IEnumerator ShakeRoutine(float duration, float intensity)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float strength = (falloff != null ? falloff.Evaluate(t) : 1f - t) * intensity;

            Vector3 randomOffset = Random.insideUnitSphere * strength;
            cachedTransform.localPosition = originalLocalPos + randomOffset;

            cachedTransform.localRotation = originalLocalRot * Quaternion.Euler(
                Random.Range(-strength, strength) * 2f,
                Random.Range(-strength, strength) * 2f,
                0f);

            yield return null;
        }

        cachedTransform.localPosition = originalLocalPos;
        cachedTransform.localRotation = originalLocalRot;
        shakeRoutine = null;
    }
}
