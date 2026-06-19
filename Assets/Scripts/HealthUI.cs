using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private Text healthText;

    private Health subscribedHealth;

    public bool IsBoundTo(Health health)
    {
        return subscribedHealth == health && healthText != null;
    }

    private void Awake()
    {
        if (healthText == null)
        {
            healthText = GetComponent<Text>();
        }
    }

    private void OnEnable()
    {
        BindToPlayer();
    }

    private void Start()
    {
        if (FindObjectOfType<ShadowStatusUI>() != null)
        {
            HideLegacyIndicator();
            return;
        }

        BindToPlayer();
    }

    private void HideLegacyIndicator()
    {
        if (healthText != null)
        {
            healthText.enabled = false;
        }

        enabled = false;
    }

    private void OnDisable()
    {
        if (subscribedHealth != null)
        {
            subscribedHealth.OnHealthChanged -= UpdateText;
            subscribedHealth = null;
        }
    }

    private void BindToPlayer()
    {
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<Health>();
            }
        }

        if (playerHealth == null || subscribedHealth == playerHealth)
        {
            return;
        }

        if (subscribedHealth != null)
        {
            subscribedHealth.OnHealthChanged -= UpdateText;
        }

        subscribedHealth = playerHealth;
        subscribedHealth.OnHealthChanged += UpdateText;
        UpdateText(subscribedHealth.CurrentHealth, subscribedHealth.MaxHealth);
    }

    private void UpdateText(int currentHealth, int maxHealth)
    {
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }
    }
}
