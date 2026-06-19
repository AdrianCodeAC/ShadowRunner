using UnityEngine;

public class GeneratorInteraction : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] private Light bulbLight;
    [SerializeField] private Renderer bulbRenderer;

    [Header("Prompt")]
    [SerializeField] private string promptText = "Press E to disable";
    [SerializeField] private float interactRadius = 2.5f;

    private bool isDisabled;

    private void Awake()
    {
        ResolveBulb();
    }

    public bool CanInteract(Transform player)
    {
        if (isDisabled || player == null)
        {
            return false;
        }

        return Vector3.Distance(transform.position, player.position) <= interactRadius;
    }

    public string GetPromptText()
    {
        return promptText;
    }

    public void Interact()
    {
        if (isDisabled)
        {
            return;
        }

        isDisabled = true;

        if (bulbLight != null)
        {
            bulbLight.enabled = false;
        }

        if (bulbRenderer != null)
        {
            bulbRenderer.material.DisableKeyword("_EMISSION");
            bulbRenderer.material.color = new Color(0.55f, 0.45f, 0.1f, 1f);
        }
    }

    private void ResolveBulb()
    {
        GameObject bulbObject = GameObject.Find("bulb");
        if (bulbObject != null)
        {
            bulbRenderer = bulbObject.GetComponent<Renderer>();
            if (bulbLight == null)
            {
                bulbLight = bulbObject.GetComponent<Light>();
            }
        }

        if (bulbLight == null)
        {
            bulbLight = GetComponentInChildren<Light>(true);
        }

        if (bulbRenderer == null)
        {
            Transform bulbTransform = FindChildRecursive(transform, "bulb");
            if (bulbTransform != null)
            {
                bulbRenderer = bulbTransform.GetComponent<Renderer>();
                if (bulbLight == null)
                {
                    bulbLight = bulbTransform.GetComponent<Light>();
                }
            }
        }

        if (bulbLight == null)
        {
            GameObject lightObject = bulbRenderer != null ? bulbRenderer.gameObject : gameObject;
            bulbLight = lightObject.AddComponent<Light>();
        }

        bulbLight.type = LightType.Point;
        bulbLight.color = new Color(1f, 0.92f, 0.45f, 1f);
        bulbLight.intensity = 2f;
        bulbLight.range = 8f;

        if (bulbRenderer == null)
        {
            bulbRenderer = bulbLight.GetComponent<Renderer>();
        }
    }

    private static Transform FindChildRecursive(Transform root, string nameContains)
    {
        string lowered = nameContains.ToLowerInvariant();

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name.ToLowerInvariant().Contains(lowered))
            {
                return child;
            }

            Transform nested = FindChildRecursive(child, nameContains);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}
