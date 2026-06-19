using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private Text healthText;

    private void Awake()
    {
        if (healthText == null)
        {
            healthText = GetComponent<Text>();
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateText;
            UpdateText(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateText;
        }
    }

    private void UpdateText(int currentHealth, int maxHealth)
    {
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }
    }
}
