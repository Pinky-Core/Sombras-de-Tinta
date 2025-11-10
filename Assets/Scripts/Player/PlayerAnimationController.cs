using UnityEngine;

/// <summary>
/// Simple animator driver for the player. It reads movement from the CharacterController
/// (or world velocity) and exposes helper methods for attack, damage, death and jump triggers.
/// </summary> 
[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController characterController;

    [Header("Parameters")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string walkBool = "IsWalking";
    [SerializeField] private string jumpTrigger = "Jump";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string damageTrigger = "Damage";
    [SerializeField] private string deathTrigger = "Death";

    [Header("Behaviour")]
    [SerializeField, Tooltip("Speed (m/s) from which the controller considers the player walking.")]
    private float walkThreshold = 0.1f;
    [SerializeField, Tooltip("If true, this component will listen to input and fire Attack/Jump triggers automatically.")]
    private bool driveInput = true;

    private void Reset()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (animator == null)
        {
            return;
        }

        float speed = GetCurrentSpeed();
        if (!string.IsNullOrEmpty(speedParam))
        {
            animator.SetFloat(speedParam, speed);
        }
        if (!string.IsNullOrEmpty(walkBool))
        {
            animator.SetBool(walkBool, speed > walkThreshold);
        }

        if (driveInput)
        {
            if (InputProvider.JumpDown())
            {
                TriggerJump();
            }

            if (InputProvider.ShootDown() || InputProvider.LeftMouseDown())
            {
                TriggerAttack();
            }
        }
    }

    private float GetCurrentSpeed()
    {
        if (characterController != null)
        {
            Vector3 velocity = characterController.velocity;
            velocity.y = 0f;
            return velocity.magnitude;
        }

        return 0f;
    }

    public void TriggerAttack()
    {
        if (animator != null && !string.IsNullOrEmpty(attackTrigger))
        {
            animator.SetTrigger(attackTrigger);
        }
    }

    public void TriggerJump()
    {
        if (animator != null && !string.IsNullOrEmpty(jumpTrigger))
        {
            animator.SetTrigger(jumpTrigger);
        }
    }

    public void TriggerDamage()
    {
        if (animator != null && !string.IsNullOrEmpty(damageTrigger))
        {
            animator.SetTrigger(damageTrigger);
        }
    }

    public void TriggerDeath()
    {
        if (animator != null && !string.IsNullOrEmpty(deathTrigger))
        {
            animator.SetTrigger(deathTrigger);
        }
    }
}
