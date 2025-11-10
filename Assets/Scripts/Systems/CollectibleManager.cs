using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tracks collectible items in the scene and activates a target object once all have been obtained.
/// </summary>
[DisallowMultipleComponent]
public class CollectibleManager : MonoBehaviour
{
    public static CollectibleManager Instance { get; private set; }

    [Header("Final Action")]
    [SerializeField, Tooltip("Se activará cuando todos los coleccionables hayan sido recogidos.")]
    private GameObject finalObjectToEnable;

    [SerializeField, Tooltip("Si es true, desactiva el objeto objetivo al iniciar la escena.")]
    private bool autoDeactivateFinalObject = true;

    [SerializeField, Tooltip("Secuencia opcional para enfocar la cámara en la salida al completar los coleccionables.")]
    private CollectibleFinalReveal finalRevealSequence;

    [Header("Feedback")]
    [SerializeField, Tooltip("Invocado cada vez que cambia el progreso (coleccionados, total).")]
    private UnityEvent<int, int> onProgressChanged;

    [SerializeField, Tooltip("Invocado una vez cuando se recoge el último coleccionable.")]
    private UnityEvent onAllCollected;

    private readonly HashSet<CollectibleItem> registeredItems = new HashSet<CollectibleItem>();
    private readonly HashSet<CollectibleItem> collectedItems = new HashSet<CollectibleItem>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple CollectibleManager instances detected. Destroying the newest one.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (autoDeactivateFinalObject && finalObjectToEnable != null)
        {
            finalObjectToEnable.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    internal void Register(CollectibleItem item)
    {
        if (item == null)
        {
            return;
        }

        if (registeredItems.Add(item))
        {
            // If the item was already collected before being re-enabled, keep it counted.
            if (item.IsAlreadyCollected && collectedItems.Add(item))
            {
                RaiseProgressChanged();
                TryCompleteRun();
                return;
            }

            RaiseProgressChanged();
        }
    }

    internal void Unregister(CollectibleItem item)
    {
        if (item == null)
        {
            return;
        }

        if (registeredItems.Remove(item))
        {
            collectedItems.Remove(item);
            RaiseProgressChanged();
        }
    }

    internal void ReportCollected(CollectibleItem item)
    {
        if (item == null)
        {
            return;
        }

        registeredItems.Add(item);

        if (collectedItems.Add(item))
        {
            RaiseProgressChanged();
            TryCompleteRun();
        }
    }

    private void RaiseProgressChanged()
    {
        onProgressChanged?.Invoke(collectedItems.Count, registeredItems.Count);
    }

    private void TryCompleteRun()
    {
        if (registeredItems.Count == 0)
        {
            return;
        }

        if (collectedItems.Count < registeredItems.Count)
        {
            return;
        }

        if (finalObjectToEnable != null)
        {
            finalObjectToEnable.SetActive(true);
        }

        if (finalRevealSequence != null)
        {
            finalRevealSequence.PlaySequence();
        }

        onAllCollected?.Invoke();
    }
}
