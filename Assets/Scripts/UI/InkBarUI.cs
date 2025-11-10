using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Updates a UI fill image (and optional label) to reflect the player's ink resource.
/// </summary>
public class InkBarUI : MonoBehaviour
{
    [SerializeField] private InkDrawer inkDrawer;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text valueLabel;
    [SerializeField, Tooltip("How quickly the UI interpolates towards the current ink percentage.")]
    private float fillLerpSpeed = 8f;
    [SerializeField] private Gradient fillGradient;

    private float displayedPercent;

    private void Awake()
    {
        if (inkDrawer == null)
        {
            inkDrawer = FindFirstObjectByType<InkDrawer>();
        }

        if (inkDrawer != null && inkDrawer.maxInk > 0f)
        {
            displayedPercent = Mathf.Clamp01(inkDrawer.currentInk / inkDrawer.maxInk);
        }
        else
        {
            displayedPercent = 0f;
        }

        UpdateVisuals(true);
    }

    private void Update()
    {
        if (inkDrawer == null || fillImage == null)
        {
            return;
        }

        float target = inkDrawer.maxInk > 0f ? Mathf.Clamp01(inkDrawer.currentInk / inkDrawer.maxInk) : 0f;
        displayedPercent = Mathf.Lerp(displayedPercent, target, 1f - Mathf.Exp(-fillLerpSpeed * Time.unscaledDeltaTime));
        UpdateVisuals(false);
    }

    private void UpdateVisuals(bool instant)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = displayedPercent;
            if (fillGradient != null)
            {
                fillImage.color = fillGradient.Evaluate(displayedPercent);
            }
        }

        if (valueLabel != null && inkDrawer != null)
        {
            valueLabel.text = $"{Mathf.CeilToInt(inkDrawer.currentInk)}/{Mathf.CeilToInt(inkDrawer.maxInk)}";
        }
    }
}
