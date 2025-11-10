using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Basic health system for the player that supports damage, healing, invulnerability windows and UnityEvents.
/// </summary>
[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField, Tooltip("Cantidad máxima de vida del jugador.")]
    private float maxHealth = 3f;

    [SerializeField, Tooltip("Vida inicial (se ajusta automáticamente al máximo).")]
    private float currentHealth = 3f;

    [SerializeField, Tooltip("Tiempo de invulnerabilidad después de recibir daño.")]
    private float hitInvulnerability = 0.75f;

    [SerializeField, Tooltip("Evento (vida actual, vida máxima) cada vez que la vida cambia.")]
    private UnityEvent<float, float> onHealthChanged;

    [SerializeField, Tooltip("Invocado una sola vez cuando la vida llega a cero.")]
    private UnityEvent onPlayerDied;

    private float invulTimer;
    private bool dead;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => dead;
    public float NormalizedHealth => maxHealth > 0f ? currentHealth / maxHealth : 0f;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        RaiseHealthChanged();
    }

    private void Update()
    {
        if (invulTimer > 0f)
        {
            invulTimer -= Time.deltaTime;
        }
    }

    public void ApplyDamage(float damage)
    {
        if (dead || damage <= 0f)
        {
            return;
        }

        if (invulTimer > 0f)
        {
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth - Mathf.Abs(damage), 0f, maxHealth);
        invulTimer = hitInvulnerability;
        RaiseHealthChanged();

        if (currentHealth <= 0f)
        {
            HandleDeath();
        }
    }

    public void Heal(float amount)
    {
        if (dead || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        RaiseHealthChanged();
    }

    private void HandleDeath()
    {
        if (dead)
        {
            return;
        }

        dead = true;
        onPlayerDied?.Invoke();
    }

    private void RaiseHealthChanged()
    {
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
