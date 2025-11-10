using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Synchronizes a UI bar/label with the PlayerHealth component.
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text valueLabel;
    [SerializeField] private Gradient fillGradient;
    [SerializeField, Tooltip("Si es true, el componente buscará automáticamente al jugador cada vez que el HUD se active.")]
    private bool autoFindPlayer = true;

    private void OnEnable()
    {
        if (playerHealth == null && autoFindPlayer)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        Refresh(true);
    }

    private void Update()
    {
        Refresh(false);
    }

    private void Refresh(bool instant)
    {
        if (playerHealth == null || fillImage == null)
        {
            return;
        }

        float percent = playerHealth.NormalizedHealth;
        fillImage.fillAmount = percent;

        if (fillGradient != null)
        {
            fillImage.color = fillGradient.Evaluate(percent);
        }

        if (valueLabel != null)
        {
            valueLabel.text = $"{Mathf.CeilToInt(playerHealth.CurrentHealth)}/{Mathf.CeilToInt(playerHealth.MaxHealth)}";
        }
    }
}
