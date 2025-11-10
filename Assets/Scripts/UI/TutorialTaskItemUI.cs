using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple UI element that displays a tutorial task description and a checkmark when completed.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class TutorialTaskItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text descriptionLabel;
    [SerializeField] private Image checkmarkImage;
    [SerializeField] private float fadeOutSpeed = 6f;

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (checkmarkImage != null)
        {
            checkmarkImage.enabled = false;
        }
    }

    public void Bind(string description)
    {
        if (descriptionLabel != null)
        {
            descriptionLabel.text = description;
        }

        if (checkmarkImage != null)
        {
            checkmarkImage.enabled = false;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }
    }

    public void PlayCompleted(float hideDelay, Action onHidden)
    {
        if (checkmarkImage != null)
        {
            checkmarkImage.enabled = true;
        }

        StopAllCoroutines();
        StartCoroutine(HideRoutine(Mathf.Max(0f, hideDelay), onHidden));
    }

    private IEnumerator HideRoutine(float delay, Action onHidden)
    {
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        while (_canvasGroup.alpha > 0.01f)
        {
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, 0f, fadeOutSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        onHidden?.Invoke();
        Destroy(gameObject);
    }
}
