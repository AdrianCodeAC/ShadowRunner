using UnityEngine;

public class ShadowStatusUI : MonoBehaviour
{
    [SerializeField] private ShadowExposureDamage exposure;

    private Health health;
    private HealthUI sceneHealthUI;
    private GUIStyle labelStyle;

    private void Awake()
    {
        if (exposure == null)
        {
            exposure = GetComponent<ShadowExposureDamage>();
        }

        health = GetComponent<Health>();
        sceneHealthUI = FindObjectOfType<HealthUI>();
    }

    private void OnGUI()
    {
        if (exposure == null)
        {
            return;
        }

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
        }

        bool inShadow = exposure.IsInShadow;
        labelStyle.normal.textColor = inShadow ? new Color(0.55f, 1f, 0.6f) : new Color(1f, 0.78f, 0.2f);
        string message = inShadow ? "IN SHADOW - SAFE" : "IN LIGHT - TAKING DAMAGE";
        GUI.Box(new Rect(Screen.width * 0.5f - 190f, 15f, 380f, 42f), GUIContent.none);
        GUI.Label(new Rect(Screen.width * 0.5f - 190f, 15f, 380f, 42f), message, labelStyle);

        if (health != null && (sceneHealthUI == null || !sceneHealthUI.IsBoundTo(health)))
        {
            labelStyle.normal.textColor = Color.white;
            GUI.Box(new Rect(15f, 15f, 150f, 42f), GUIContent.none);
            GUI.Label(new Rect(15f, 15f, 150f, 42f), $"HP {health.CurrentHealth}/{health.MaxHealth}", labelStyle);
        }
    }
}
