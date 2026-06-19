using UnityEngine;

public class GeneratorInteraction : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] private Light bulbLight;
    [SerializeField] private Renderer bulbRenderer;

    [Header("Prompt")]
    [SerializeField] private string promptText = "Press E to interact";
    [SerializeField] private float interactRadius = 3.5f;

    private bool isDisabled;
    private Transform interactionPoint;
    private Collider interactionCollider;

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

        return GetDistanceTo(player) <= interactRadius && HasClearLineOfSight(player);
    }

    public float GetDistanceTo(Transform player)
    {
        if (player == null)
        {
            return float.PositiveInfinity;
        }

        if (interactionCollider != null)
        {
            Vector3 closestPoint = interactionCollider.ClosestPoint(player.position);
            return Vector3.Distance(closestPoint, player.position);
        }

        Transform point = interactionPoint != null ? interactionPoint : transform;
        return Vector3.Distance(point.position, player.position);
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
        interactionCollider = GetComponent<Collider>();
        if (interactionCollider == null)
        {
            Transform generatorBlock = FindChildRecursive(transform, "generator");
            if (generatorBlock != null)
            {
                interactionCollider = generatorBlock.GetComponent<Collider>();
            }
        }
        if (interactionCollider == null)
        {
            interactionCollider = GetComponentInChildren<Collider>(true);
        }
        interactionPoint = interactionCollider != null ? interactionCollider.transform : transform;

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
            bulbLight = FindNearestAvailableLight();
        }

        if (bulbRenderer == null)
        {
            bulbRenderer = FindNearestAvailableBulbRenderer();
        }

        if (bulbLight == null)
        {
            GameObject lightObject = bulbRenderer != null ? bulbRenderer.gameObject : gameObject;
            bulbLight = lightObject.AddComponent<Light>();
            bulbLight.type = LightType.Point;
            bulbLight.color = new Color(1f, 0.92f, 0.45f, 1f);
            bulbLight.intensity = 2f;
            bulbLight.range = 8f;
        }

        if (bulbRenderer == null)
        {
            bulbRenderer = bulbLight.GetComponent<Renderer>();
        }

        ShadowExposureDamage[] exposureComponents = FindObjectsOfType<ShadowExposureDamage>();
        for (int i = 0; i < exposureComponents.Length; i++)
        {
            exposureComponents[i].RefreshLightSources();
        }
    }

    private bool HasClearLineOfSight(Transform player)
    {
        Vector3 start = player.position;
        Collider playerCollider = player.GetComponent<Collider>();
        if (playerCollider == null)
        {
            playerCollider = player.GetComponentInChildren<Collider>();
        }
        if (playerCollider != null)
        {
            start = playerCollider.bounds.center;
        }

        Vector3 end = interactionCollider != null
            ? interactionCollider.bounds.center
            : interactionPoint.position;
        return HasClearPath(start, end, player);
    }

    private bool HasClearPath(Vector3 start, Vector3 end, Transform player)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        if (distance <= 0.01f)
        {
            return true;
        }

        RaycastHit[] hits = Physics.RaycastAll(
            start,
            direction / distance,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Transform hit = hits[i].collider.transform;
            if (hit == player || hit.IsChildOf(player) || player.IsChildOf(hit) ||
                hit == transform || hit.IsChildOf(transform) || transform.IsChildOf(hit))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private Light FindNearestAvailableLight()
    {
        Light closest = null;
        float closestSqrDistance = float.PositiveInfinity;
        Light[] lights = FindObjectsOfType<Light>(true);

        for (int i = 0; i < lights.Length; i++)
        {
            Light candidate = lights[i];
            if (candidate == null || candidate.gameObject.scene != gameObject.scene ||
                candidate.type == LightType.Directional ||
                IsEnemyLight(candidate) || IsLightClaimedByAnotherGenerator(candidate))
            {
                continue;
            }

            float sqrDistance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closest = candidate;
            }
        }

        return closest;
    }

    private Renderer FindNearestAvailableBulbRenderer()
    {
        Renderer closest = null;
        float closestSqrDistance = float.PositiveInfinity;
        Renderer[] renderers = FindObjectsOfType<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer candidate = renderers[i];
            if (candidate == null || candidate.gameObject.scene != gameObject.scene ||
                !IsBulbName(candidate.name) || candidate.GetComponentInParent<EnemyPatrol>() != null ||
                IsRendererClaimedByAnotherGenerator(candidate))
            {
                continue;
            }

            float sqrDistance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closest = candidate;
            }
        }

        return closest;
    }

    private bool IsLightClaimedByAnotherGenerator(Light candidate)
    {
        GeneratorInteraction[] generators = FindObjectsOfType<GeneratorInteraction>(true);
        for (int i = 0; i < generators.Length; i++)
        {
            if (generators[i] != this && generators[i].bulbLight == candidate)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsRendererClaimedByAnotherGenerator(Renderer candidate)
    {
        GeneratorInteraction[] generators = FindObjectsOfType<GeneratorInteraction>(true);
        for (int i = 0; i < generators.Length; i++)
        {
            if (generators[i] != this && generators[i].bulbRenderer == candidate)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEnemyLight(Light candidate)
    {
        return candidate.GetComponentInParent<EnemyPatrol>() != null ||
            candidate.GetComponentInParent<GuardVisionDamage>() != null;
    }

    private static bool IsBulbName(string objectName)
    {
        string lowered = objectName.ToLowerInvariant();
        return lowered.Contains("bulb") || lowered.Contains("light");
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
