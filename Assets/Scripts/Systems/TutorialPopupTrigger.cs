using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Shows a tutorial popup when the player enters the trigger.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TutorialPopupTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private TutorialPopupManager popupManager;
    [SerializeField] private TutorialPopupStep popupStep;
    [SerializeField, Tooltip("Once triggered, this collider will be disabled to avoid reopening the popup.")]
    private bool disableAfterUse = true;
    [SerializeField] private UnityEvent onPopupShown;
    [SerializeField] private UnityEvent onPopupClosed;

    private bool used;

    private void Reset()
    {
        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (used || popupManager == null || popupStep == null)
        {
            return;
        }

        if (!other.CompareTag(playerTag))
        {
            return;
        }

        used = true;
        onPopupShown?.Invoke();

        popupManager.ShowStep(popupStep, () =>
        {
            onPopupClosed?.Invoke();
            if (disableAfterUse)
            {
                var collider = GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
        });
    }
}
