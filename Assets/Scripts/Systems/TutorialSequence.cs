using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages a phase-based tutorial UI that lives on the HUD. Each phase exposes tasks (checkmarks) that disappear as they are completed.
/// </summary>
[DisallowMultipleComponent]
public class TutorialSequence : MonoBehaviour
{
    [Header("Popup UI")]
    [SerializeField] private CanvasGroup popupCanvas;
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text phaseNumberLabel;
    [SerializeField] private TMP_Text phaseNameLabel;
    [SerializeField] private Transform taskContainer;
    [SerializeField] private TutorialTaskItemUI taskItemPrefab;
    [SerializeField, Min(0f)] private float completedTaskHideDelay = 0.25f;

    [Header("Phases")]
    [SerializeField] private List<TutorialPhase> phases = new List<TutorialPhase>();

    [SerializeField, Tooltip("Invoked once all phases have been completed.")]
    private UnityEvent onTutorialFinished;

    private readonly Dictionary<string, TaskRuntime> activeTasks = new Dictionary<string, TaskRuntime>(StringComparer.OrdinalIgnoreCase);
    private readonly List<TaskRuntime> currentPhaseTasks = new List<TaskRuntime>();
    private int currentPhaseIndex = -1;
    private int tasksInCurrentPhase;
    private int tasksCompletedInCurrentPhase;

    private void OnEnable()
    {
        TutorialEventBus.TaskCompleted += HandleTaskCompleted;

        if (phases.Count == 0)
        {
            ClearCurrentPhaseUI();
            HidePopup();
            return;
        }

        if (currentPhaseIndex < 0)
        {
            AdvanceToPhase(0);
        }
        else
        {
            RebuildPhaseUI();
        }
    }

    private void OnDisable()
    {
        TutorialEventBus.TaskCompleted -= HandleTaskCompleted;
    }

    private void RebuildPhaseUI()
    {
        ClearCurrentPhaseUI();

        if (currentPhaseIndex < 0 || currentPhaseIndex >= phases.Count)
        {
            HidePopup();
            return;
        }

        PopulatePhase(phases[currentPhaseIndex]);
    }

    private void AdvanceToPhase(int nextIndex)
    {
        ClearCurrentPhaseUI();

        if (nextIndex >= phases.Count)
        {
            currentPhaseIndex = phases.Count;
            HidePopup();
            onTutorialFinished?.Invoke();
            return;
        }

        currentPhaseIndex = Mathf.Clamp(nextIndex, 0, phases.Count - 1);
        PopulatePhase(phases[currentPhaseIndex]);
    }

    private void PopulatePhase(TutorialPhase phase)
    {
        if (phase == null)
        {
            return;
        }

        ShowPopup();
        UpdatePhaseLabels(phase);

        tasksCompletedInCurrentPhase = 0;
        tasksInCurrentPhase = 0;

        foreach (var task in phase.Tasks)
        {
            if (task == null || string.IsNullOrEmpty(task.Id))
            {
                continue;
            }

            if (activeTasks.ContainsKey(task.Id))
            {
                Debug.LogWarning($"TutorialSequence: Duplicate task id '{task.Id}' detected. Skipping.", this);
                continue;
            }

            TutorialTaskItemUI widget = null;
            if (taskItemPrefab != null && taskContainer != null)
            {
                widget = Instantiate(taskItemPrefab, taskContainer);
                widget.Bind(task.Description);
            }

            var runtime = new TaskRuntime(task, widget);
            currentPhaseTasks.Add(runtime);
            activeTasks.Add(task.Id, runtime);
            tasksInCurrentPhase++;
        }

        if (tasksInCurrentPhase == 0)
        {
            Debug.LogWarning("TutorialSequence: Phase has no tasks, advancing automatically.", this);
            AdvanceToPhase(currentPhaseIndex + 1);
        }
    }

    private void HandleTaskCompleted(string taskId)
    {
        if (string.IsNullOrEmpty(taskId))
        {
            return;
        }

        if (!activeTasks.TryGetValue(taskId, out var runtime) || runtime.Completed)
        {
            return;
        }

        runtime.Completed = true;
        tasksCompletedInCurrentPhase = Mathf.Clamp(tasksCompletedInCurrentPhase + 1, 0, tasksInCurrentPhase);

        if (runtime.Widget != null)
        {
            runtime.Widget.PlayCompleted(completedTaskHideDelay, () => RemoveRuntime(taskId, runtime));
        }
        else
        {
            RemoveRuntime(taskId, runtime);
        }

        if (tasksInCurrentPhase > 0 && tasksCompletedInCurrentPhase >= tasksInCurrentPhase)
        {
            AdvanceToPhase(currentPhaseIndex + 1);
        }
    }

    private void RemoveRuntime(string taskId, TaskRuntime runtime)
    {
        activeTasks.Remove(taskId);
        currentPhaseTasks.Remove(runtime);
    }

    private void ClearCurrentPhaseUI()
    {
        foreach (var runtime in currentPhaseTasks)
        {
            if (runtime.Widget != null)
            {
                Destroy(runtime.Widget.gameObject);
            }

            if (!string.IsNullOrEmpty(runtime.TaskId))
            {
                activeTasks.Remove(runtime.TaskId);
            }
        }

        currentPhaseTasks.Clear();
        tasksInCurrentPhase = 0;
        tasksCompletedInCurrentPhase = 0;

        if (taskContainer != null)
        {
            for (int i = taskContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(taskContainer.GetChild(i).gameObject);
            }
        }
    }

    private void UpdatePhaseLabels(TutorialPhase phase)
    {
        if (titleLabel != null && string.IsNullOrWhiteSpace(titleLabel.text))
        {
            titleLabel.text = "Tutorial";
        }

        if (phaseNumberLabel != null)
        {
            phaseNumberLabel.text = $"Fase {currentPhaseIndex + 1}";
        }

        if (phaseNameLabel != null)
        {
            phaseNameLabel.text = phase.DisplayName;
        }
    }

    private void ShowPopup()
    {
        if (popupCanvas == null)
        {
            return;
        }

        popupCanvas.alpha = 1f;
        popupCanvas.blocksRaycasts = true;
        popupCanvas.interactable = true;
    }

    private void HidePopup()
    {
        if (popupCanvas != null)
        {
            popupCanvas.alpha = 0f;
            popupCanvas.blocksRaycasts = false;
            popupCanvas.interactable = false;
        }
    }

    [Serializable]
    private class TutorialPhase
    {
        [SerializeField] private string displayName = "Nueva acción";
        [SerializeField] private List<TutorialTaskDefinition> tasks = new List<TutorialTaskDefinition>();

        public string DisplayName => displayName;
        public IReadOnlyList<TutorialTaskDefinition> Tasks => tasks;
    }

    [Serializable]
    private class TutorialTaskDefinition
    {
        [SerializeField, Tooltip("Id que debe coincidir con lo que reportan los scripts de gameplay. Ej: move_horizontal.")]
        private string taskId;

        [SerializeField, Tooltip("Texto que se mostrará junto al checkmark.")]
        private string description;

        public string Id => taskId;
        public string Description => description;
    }

    private sealed class TaskRuntime
    {
        public readonly string TaskId;
        public readonly TutorialTaskItemUI Widget;
        public bool Completed;

        public TaskRuntime(TutorialTaskDefinition definition, TutorialTaskItemUI widget)
        {
            TaskId = definition?.Id ?? string.Empty;
            Widget = widget;
        }
    }
}
