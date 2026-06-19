using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(CapsuleCollider))]
public class ShadowExposureDamage : MonoBehaviour
{
    [Header("Light Sources")]
    [SerializeField] private Light[] lightSources;

    [Header("Damage")]
    [SerializeField] private float damagePerSecond = 10f;
    [SerializeField] private float lightDamageMultiplier = 2f;
    [SerializeField] private float darkRegenDelaySeconds = 3f;
    [SerializeField] private float shadowCheckInterval = 0.05f;

    [Header("Detection")]
    [SerializeField] private LayerMask occluderMask = ~0;
    [SerializeField] private float maxRayDistance = 500f;
    [SerializeField] private float footOffsetFromGround = 0.05f;
    [SerializeField] private float footSampleSpread = 0.18f;

    private Health health;
    private CapsuleCollider capsuleCollider;
    private float checkTimer;
    private float damageBuffer;
    private float regenBuffer;
    private float darkTimer;
    private Vector3[] footSamples;

    public bool IsInShadow { get; private set; } = true;

    private void Awake()
    {
        health = GetComponent<Health>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        BuildFootSamples();

        if (!HasValidLightSource())
        {
            lightSources = FindObjectsOfType<Light>();
        }
    }

    private bool HasValidLightSource()
    {
        if (lightSources == null)
        {
            return false;
        }

        for (int i = 0; i < lightSources.Length; i++)
        {
            if (lightSources[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    public void RefreshLightSources()
    {
        lightSources = FindObjectsOfType<Light>();
    }

    public void ConfigureStandardRates()
    {
        damagePerSecond = 35f;
        lightDamageMultiplier = 2f;
        darkRegenDelaySeconds = 3f;
    }

    private void Update()
    {
        checkTimer += Time.deltaTime;
        if (checkTimer < shadowCheckInterval)
        {
            return;
        }

        float elapsed = checkTimer;
        checkTimer = 0f;

        bool isLit = IsAnyFootLit(out float exposureDamagePerSecond);
        IsInShadow = !isLit;

        if (isLit)
        {
            darkTimer = 0f;
            regenBuffer = 0f;
            damageBuffer += exposureDamagePerSecond * elapsed;

            int damage = Mathf.FloorToInt(damageBuffer);
            if (damage > 0)
            {
                damageBuffer -= damage;
                health.TakeDamage(damage);
            }
        }
        else
        {
            damageBuffer = 0f;
            darkTimer += elapsed;

            if (darkTimer < darkRegenDelaySeconds)
            {
                return;
            }

            regenBuffer += damagePerSecond * lightDamageMultiplier * elapsed;
            int heal = Mathf.FloorToInt(regenBuffer);
            if (heal > 0)
            {
                regenBuffer -= heal;
                health.Heal(heal);
            }
        }
    }

    private bool IsAnyFootLit(out float exposureDamagePerSecond)
    {
        exposureDamagePerSecond = 0f;
        bool isLit = false;
        for (int sampleIndex = 0; sampleIndex < footSamples.Length; sampleIndex++)
        {
            Vector3 samplePoint = transform.TransformPoint(footSamples[sampleIndex]);

            for (int lightIndex = 0; lightIndex < lightSources.Length; lightIndex++)
            {
                if (IsPointLitByLight(samplePoint, lightSources[lightIndex]))
                {
                    isLit = true;
                    ChallengeHunterAI hunter = lightSources[lightIndex].GetComponentInParent<ChallengeHunterAI>();
                    float sourceDamage = hunter != null
                        ? hunter.FlashlightDamagePerSecond
                        : damagePerSecond * lightDamageMultiplier;
                    exposureDamagePerSecond = Mathf.Max(exposureDamagePerSecond, sourceDamage);
                }
            }
        }

        return isLit;
    }

    private bool IsPointLitByLight(Vector3 point, Light lightSource)
    {
        if (lightSource == null || !lightSource.enabled || lightSource.intensity <= 0f)
        {
            return false;
        }

        switch (lightSource.type)
        {
            case LightType.Directional:
            {
                Vector3 toLight = -lightSource.transform.forward.normalized;
                return HasClearPath(point, toLight, maxRayDistance, lightSource);
            }

            case LightType.Point:
            {
                Vector3 toLight = lightSource.transform.position - point;
                float distance = toLight.magnitude;

                if (distance > lightSource.range)
                {
                    return false;
                }

                return HasClearPath(point, toLight.normalized, distance, lightSource);
            }

            case LightType.Spot:
            {
                Vector3 fromLightToPoint = point - lightSource.transform.position;
                float distance = fromLightToPoint.magnitude;

                if (distance > lightSource.range)
                {
                    return false;
                }

                Vector3 direction = fromLightToPoint.normalized;
                float angleToPoint = Vector3.Angle(lightSource.transform.forward, direction);

                if (angleToPoint > lightSource.spotAngle * 0.5f)
                {
                    return false;
                }

                return HasClearPath(lightSource.transform.position, direction, distance, lightSource);
            }

            default:
                return false;
        }
    }

    private bool HasClearPath(Vector3 origin, Vector3 direction, float distance, Light lightSource)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, occluderMask, QueryTriggerInteraction.Ignore);
        Transform lightOwner = lightSource.GetComponentInParent<GuardVisionDamage>()?.transform;
        if (lightOwner == null)
        {
            lightOwner = lightSource.GetComponentInParent<ChallengeHunterAI>()?.transform;
        }

        for (int i = 0; i < hits.Length; i++)
        {
            Transform hit = hits[i].collider.transform;
            if (hit.IsChildOf(transform) || transform.IsChildOf(hit))
            {
                continue;
            }

            if (lightOwner != null && (hit.IsChildOf(lightOwner) || hit == lightOwner))
            {
                continue;
            }

            if (hit.IsChildOf(lightSource.transform) || lightSource.transform.IsChildOf(hit))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private Vector3 GetFootOrigin()
    {
        Bounds bounds = capsuleCollider.bounds;
        return new Vector3(bounds.center.x, bounds.min.y + footOffsetFromGround, bounds.center.z);
    }

    private void BuildFootSamples()
    {
        float y = GetFootOrigin().y - transform.position.y;
        footSamples = new[]
        {
            new Vector3(0f, y, 0f),
            new Vector3(footSampleSpread, y, 0f),
            new Vector3(-footSampleSpread, y, 0f),
            new Vector3(0f, y, footSampleSpread),
            new Vector3(0f, y, -footSampleSpread)
        };
    }
}
