using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Simple collectible that registers with the CollectibleManager and notifies when the player grabs it.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CollectibleItem : MonoBehaviour
{
    [SerializeField, Tooltip("Tag que debe tener el jugador para recoger este objeto.")]
    private string playerTag = "Player";

    [SerializeField, Tooltip("Visual a desactivar al recoger (si se deja vacío se usará este mismo GameObject).")]
    private GameObject visualRoot;

    [SerializeField] private ParticleSystem pickupParticles;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pickupClip;

    [SerializeField, Tooltip("Destruye el objeto tras reproducir efectos.")]
    private bool destroyAfterPickup = true;

    [SerializeField, Tooltip("Retardo antes de destruir el objeto (útil para que termine el sonido).")]
    private float destroyDelay = 0.5f;

    [SerializeField, Tooltip("Eventos adicionales al recoger.")]
    private UnityEvent onCollected;

    private bool collected;

    public bool IsAlreadyCollected => collected;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        if (visualRoot == null)
        {
            visualRoot = gameObject;
        }
    }

    private void OnEnable()
    {
        CollectibleManager.Instance?.Register(this);
    }

    private void OnDisable()
    {
        CollectibleManager.Instance?.Unregister(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
        {
            return;
        }

        if (!other.CompareTag(playerTag))
        {
            return;
        }

        Collect();
    }

    public void Collect()
    {
        if (collected)
        {
            return;
        }

        collected = true;

        if (visualRoot != null)
        {
            visualRoot.SetActive(false);
        }

        if (pickupParticles != null)
        {
            pickupParticles.Play();
        }

        if (audioSource != null && pickupClip != null)
        {
            audioSource.PlayOneShot(pickupClip);
        }

        onCollected?.Invoke();

        CollectibleManager.Instance?.ReportCollected(this);

        if (destroyAfterPickup)
        {
            Destroy(gameObject, destroyDelay);
        }
    }
}
