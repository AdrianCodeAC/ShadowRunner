using UnityEngine;

public class ShadowStatusUI : MonoBehaviour
{
    [SerializeField] private ShadowExposureDamage exposure;

    private GUIStyle labelStyle;

    private void Awake()
    {
        if (exposure == null)
        {
            exposure = GetComponent<ShadowExposureDamage>();
        }
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
    }
}
