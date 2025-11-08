using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controls a multi-phase tutorial where each phase waits for the player to reach its trigger.
/// Attach this to a scene manager object and configure the phases from the inspector.
/// </summary>
[DisallowMultipleComponent]
public class TutorialSequence : MonoBehaviour
{
    [SerializeField, Tooltip("Tag that identifies the player object that should progress the tutorial.")]
    private string playerTag = "Player";

    [SerializeField, Tooltip("Ordered list of tutorial phases that will be unlocked step by step.")]
    private List<TutorialPhase> phases = new List<TutorialPhase>();

    [SerializeField, Tooltip("Invoked once all phases have been completed.")]
    private UnityEvent onTutorialFinished;

    private int currentPhaseIndex = -1;

    private void Awake()
    {
        for (int i = 0; i < phases.Count; i++)
        {
            TutorialPhase phase = phases[i];
            if (phase == null)
            {
                continue;
            }

            phase.HideTexts();
            SetupTriggerProxy(phase, i);
        }
    }

    private void OnEnable()
    {
        if (phases.Count == 0)
        {
            return;
        }

        AdvanceToPhase(0);
    }

    private void SetupTriggerProxy(TutorialPhase phase, int phaseIndex)
    {
        if (phase.Trigger == null)
        {
            Debug.LogWarning($"TutorialSequence: Phase '{phase.Name}' is missing a trigger collider.", this);
            return;
        }

        phase.Trigger.isTrigger = true;
        phase.Trigger.enabled = false;

        TutorialTriggerProxy proxy = phase.Trigger.GetComponent<TutorialTriggerProxy>();
        if (proxy == null)
        {
            proxy = phase.Trigger.gameObject.AddComponent<TutorialTriggerProxy>();
        }

        proxy.Configure(this, phaseIndex);
    }

    internal void HandleTriggerActivated(int phaseIndex, Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (phaseIndex != currentPhaseIndex)
        {
            return;
        }

        CompleteCurrentPhase();
    }

    private void AdvanceToPhase(int newIndex)
    {
        if (newIndex < 0 || newIndex >= phases.Count)
        {
            currentPhaseIndex = phases.Count;
            onTutorialFinished?.Invoke();
            return;
        }

        currentPhaseIndex = newIndex;

        TutorialPhase phase = phases[currentPhaseIndex];
        phase.ShowTexts();
        phase.Trigger?.gameObject.SetActive(true);
        if (phase.Trigger != null)
        {
            phase.Trigger.enabled = true;
        }

        phase.OnPhaseStarted?.Invoke();
    }

    private void CompleteCurrentPhase()
    {
        if (currentPhaseIndex < 0 || currentPhaseIndex >= phases.Count)
        {
            return;
        }

        TutorialPhase phase = phases[currentPhaseIndex];

        if (phase.Trigger != null)
        {
            phase.Trigger.enabled = false;
        }

        phase.HideTexts();
        phase.OnPhaseCompleted?.Invoke();

        AdvanceToPhase(currentPhaseIndex + 1);
    }

    [Serializable]
    private class TutorialPhase
    {
        [SerializeField] private string phaseName = "Phase";
        [SerializeField, Tooltip("Trigger collider that unlocks this phase when the player enters it.")]
        private Collider trigger;

        [SerializeField, Tooltip("World-space text objects that should be visible while this phase is active.")]
        private GameObject[] textObjects;

        [SerializeField] private UnityEvent onPhaseStarted;
        [SerializeField] private UnityEvent onPhaseCompleted;

        public string Name => phaseName;
        public Collider Trigger => trigger;
        public UnityEvent OnPhaseStarted => onPhaseStarted;
        public UnityEvent OnPhaseCompleted => onPhaseCompleted;

        public void ShowTexts()
        {
            if (textObjects == null)
            {
                return;
            }

            foreach (GameObject text in textObjects)
            {
                if (text != null)
                {
                    text.SetActive(true);
                }
            }
        }

        public void HideTexts()
        {
            if (textObjects == null)
            {
                return;
            }

            foreach (GameObject text in textObjects)
            {
                if (text != null)
                {
                    text.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Lightweight proxy attached to each trigger to let the manager know when the player enters it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    private class TutorialTriggerProxy : MonoBehaviour
    {
        private TutorialSequence owner;
        private int phaseIndex;

        public void Configure(TutorialSequence sequence, int index)
        {
            owner = sequence;
            phaseIndex = index;
        }

        private void OnTriggerEnter(Collider other)
        {
            owner?.HandleTriggerActivated(phaseIndex, other);
        }
    }
}
