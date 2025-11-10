using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles showing a tutorial popup panel, pausing the game if needed, and resuming when the player continues.
/// </summary>
[DisallowMultipleComponent]
public class TutorialPopupManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text descriptionLabel;
    [SerializeField] private GameObject continueHint;
    [SerializeField] private UnityEvent onPopupOpened;
    [SerializeField] private UnityEvent onPopupClosed;

    private TutorialPopupStep currentStep;
    private Action onStepClosed;
    private bool popupVisible;
    private float cachedTimeScale = 1f;

    private void Awake()
    {
        HidePanelInstant();
    }

    /// <summary>
    /// Shows the popup with the provided step data.
    /// </summary>
    public void ShowStep(TutorialPopupStep step, Action onClosed = null)
    {
        if (step == null)
        {
            Debug.LogWarning("TutorialPopupManager: Tried to show a null step.");
            return;
        }

        currentStep = step;
        onStepClosed = onClosed;

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.gameObject.SetActive(true);
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.blocksRaycasts = true;
            panelCanvasGroup.interactable = true;
        }

        if (titleLabel != null)
        {
            titleLabel.text = string.IsNullOrWhiteSpace(step.Title) ? "Tutorial" : step.Title;
        }

        if (descriptionLabel != null)
        {
            descriptionLabel.text = step.Description ?? string.Empty;
        }

        if (continueHint != null)
        {
            continueHint.SetActive(true);
        }

        if (step.PauseGame)
        {
            cachedTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
            Time.timeScale = 0f;
        }

        popupVisible = true;
        onPopupOpened?.Invoke();
    }

    /// <summary>
    /// Called from the UI button (e.g., "Continuar") to close the popup.
    /// </summary>
    public void ConfirmCurrentStep()
    {
        if (!popupVisible)
        {
            return;
        }

        HidePanelInstant();

        var callback = onStepClosed;
        onStepClosed = null;
        callback?.Invoke();
    }

    private void HidePanelInstant()
    {
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.blocksRaycasts = false;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.gameObject.SetActive(false);
        }

        if (continueHint != null)
        {
            continueHint.SetActive(false);
        }

        if (currentStep != null && currentStep.PauseGame)
        {
            Time.timeScale = cachedTimeScale;
        }

        popupVisible = false;
        currentStep = null;
        onPopupClosed?.Invoke();
    }
}
