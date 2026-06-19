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

    private void Awake()
    {
        health = GetComponent<Health>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        BuildFootSamples();

        if (lightSources == null || lightSources.Length == 0)
        {
            lightSources = FindObjectsOfType<Light>();
        }
    }

    private void Update()
    {
        checkTimer += Time.deltaTime;
        if (checkTimer < shadowCheckInterval)
        {
            return;
        }

        checkTimer = 0f;

        bool isLit = IsAnyFootLit();

        if (isLit)
        {
            darkTimer = 0f;
            regenBuffer = 0f;
            damageBuffer += damagePerSecond * lightDamageMultiplier * shadowCheckInterval;

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
            darkTimer += shadowCheckInterval;

            if (darkTimer < darkRegenDelaySeconds)
            {
                return;
            }

            regenBuffer += damagePerSecond * lightDamageMultiplier * shadowCheckInterval;
            int heal = Mathf.FloorToInt(regenBuffer);
            if (heal > 0)
            {
                regenBuffer -= heal;
                health.Heal(heal);
            }
        }
    }

    private bool IsAnyFootLit()
    {
        for (int sampleIndex = 0; sampleIndex < footSamples.Length; sampleIndex++)
        {
            Vector3 samplePoint = transform.TransformPoint(footSamples[sampleIndex]);

            for (int lightIndex = 0; lightIndex < lightSources.Length; lightIndex++)
            {
                if (IsPointLitByLight(samplePoint, lightSources[lightIndex]))
                {
                    return true;
                }
            }
        }

        return false;
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
                return !Physics.Raycast(point, toLight, maxRayDistance, occluderMask, QueryTriggerInteraction.Ignore);
            }

            case LightType.Point:
            {
                Vector3 toLight = lightSource.transform.position - point;
                float distance = toLight.magnitude;

                if (distance > lightSource.range)
                {
                    return false;
                }

                return !Physics.Raycast(point, toLight.normalized, distance, occluderMask, QueryTriggerInteraction.Ignore);
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

                return !Physics.Raycast(lightSource.transform.position, direction, distance, occluderMask, QueryTriggerInteraction.Ignore);
            }

            default:
                return false;
        }
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
