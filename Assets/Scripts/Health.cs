using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }

    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;

    private bool deathNotified;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public bool IsDead => CurrentHealth <= 0;

    public void TakeDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0 && !deathNotified)
        {
            deathNotified = true;
            OnDied?.Invoke();
        }
    }

    public void HealToFull()
    {
        CurrentHealth = maxHealth;
        deathNotified = false;
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void SetHealth(int amount)
    {
        bool wasDead = deathNotified;
        CurrentHealth = Mathf.Clamp(amount, 0, maxHealth);
        deathNotified = CurrentHealth <= 0;
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0 && !wasDead)
        {
            deathNotified = true;
            OnDied?.Invoke();
        }
    }
}
