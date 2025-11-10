using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Plays a short camera pan to highlight the exit once all collectibles are gathered.
/// </summary>
public class CollectibleFinalReveal : MonoBehaviour
{
    [SerializeField] private SmoothFollowCamera followCamera;
    [SerializeField] private Transform focusPoint;
    [SerializeField] private Vector3 focusOffset = new Vector3(0f, 6f, -8f);
    [SerializeField, Min(0.1f)] private float travelTime = 1.5f;
    [SerializeField, Min(0f)] private float holdTime = 1.2f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private ParticleSystem arrivalParticles;
    [SerializeField] private Transform particleSpawnPoint;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip revealClip;
    [SerializeField] private UnityEvent onRevealStarted;
    [SerializeField] private UnityEvent onRevealFinished;

    private bool playing;

    public void PlaySequence()
    {
        if (playing || focusPoint == null)
        {
            return;
        }

        StartCoroutine(RevealRoutine());
    }

    private IEnumerator RevealRoutine()
    {
        playing = true;
        Transform camTransform = followCamera != null ? followCamera.transform : Camera.main?.transform;
        if (camTransform == null)
        {
            yield break;
        }

        if (followCamera != null)
        {
            followCamera.enabled = false;
        }

        Vector3 startPos = camTransform.position;
        Quaternion startRot = camTransform.rotation;
        Vector3 targetPos = focusPoint.position + focusOffset;
        Quaternion targetRot = Quaternion.LookRotation((focusPoint.position - targetPos).normalized, Vector3.up);

        onRevealStarted?.Invoke();

        float elapsed = 0f;
        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelTime);
            float eased = ease != null ? ease.Evaluate(t) : t;
            camTransform.position = Vector3.Lerp(startPos, targetPos, eased);
            camTransform.rotation = Quaternion.Slerp(startRot, targetRot, eased);
            yield return null;
        }

        camTransform.position = targetPos;
        camTransform.rotation = targetRot;

        if (arrivalParticles != null)
        {
            if (particleSpawnPoint != null)
            {
                arrivalParticles.transform.position = particleSpawnPoint.position;
                arrivalParticles.transform.rotation = particleSpawnPoint.rotation;
            }
            else
            {
                arrivalParticles.transform.position = focusPoint.position;
            }

            arrivalParticles.Play(true);
        }

        if (audioSource != null && revealClip != null)
        {
            audioSource.PlayOneShot(revealClip);
        }

        if (holdTime > 0f)
        {
            yield return new WaitForSeconds(holdTime);
        }

        if (followCamera != null)
        {
            camTransform.position = followCamera.GetDesiredPosition();
            camTransform.rotation = followCamera.transform.rotation;
            followCamera.enabled = true;
        }
        else
        {
            float backTime = travelTime * 0.6f;
            float timer = 0f;
            while (timer < backTime)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / backTime);
                float eased = ease != null ? ease.Evaluate(t) : t;
                camTransform.position = Vector3.Lerp(targetPos, startPos, eased);
                camTransform.rotation = Quaternion.Slerp(targetRot, startRot, eased);
                yield return null;
            }
            camTransform.position = startPos;
            camTransform.rotation = startRot;
        }

        onRevealFinished?.Invoke();
        playing = false;
    }
}
