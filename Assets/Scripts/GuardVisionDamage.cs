using UnityEngine;

public class GuardVisionDamage : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform playerTarget;

    [Header("Flashlight")]
    [SerializeField] private Light flashlight;
    [SerializeField] private Transform eyePoint;
    [SerializeField] private float damagePerSecond = 12f;
    [SerializeField] private float checkInterval = 0.05f;

    [Header("Sight")]
    [SerializeField] private LayerMask occluderMask = ~0;

    private Health playerHealth;
    private ShadowExposureDamage shadowExposure;
    private float checkTimer;
    private float damageBuffer;

    private void Awake()
    {
        if (flashlight == null)
        {
            flashlight = GetComponentInChildren<Light>(true);
        }

        if (eyePoint == null)
        {
            eyePoint = flashlight != null ? flashlight.transform : transform;
        }

        if (flashlight != null && flashlight.type != LightType.Spot)
        {
            flashlight.type = LightType.Spot;
        }

        ResolvePlayer();
    }

    private void ResolvePlayer()
    {
        if (playerTarget == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTarget = playerObject.transform;
            }
        }

        if (playerTarget == null)
        {
            Health foundHealth = FindObjectOfType<Health>();
            if (foundHealth != null)
            {
                playerTarget = foundHealth.transform;
            }
        }

        if (playerTarget != null)
        {
            playerHealth = playerTarget.GetComponent<Health>();
            if (playerHealth == null)
            {
                playerHealth = playerTarget.GetComponentInParent<Health>();
            }
            if (playerHealth == null)
            {
                playerHealth = playerTarget.GetComponentInChildren<Health>();
            }

            shadowExposure = playerTarget.GetComponentInParent<ShadowExposureDamage>();
            if (shadowExposure == null)
            {
                shadowExposure = playerTarget.GetComponentInChildren<ShadowExposureDamage>();
            }
        }
    }

    private void Update()
    {
        if (playerTarget == null || playerHealth == null || flashlight == null)
        {
            return;
        }

        // ShadowExposureDamage owns light damage when present, avoiding duplicate DPS.
        if (shadowExposure != null && shadowExposure.enabled)
        {
            return;
        }

        checkTimer += Time.deltaTime;
        if (checkTimer < checkInterval)
        {
            return;
        }

        checkTimer = 0f;

        if (CanSeePlayer())
        {
            damageBuffer += damagePerSecond * checkInterval;

            int damage = Mathf.FloorToInt(damageBuffer);
            if (damage > 0)
            {
                damageBuffer -= damage;
                playerHealth.TakeDamage(damage);
            }
        }
        else
        {
            damageBuffer = 0f;
        }
    }

    private bool CanSeePlayer()
    {
        Vector3 origin = eyePoint.position;
        Vector3 playerPoint = GetPlayerAimPoint();
        Vector3 toPlayer = playerPoint - origin;
        float distance = toPlayer.magnitude;

        if (distance > flashlight.range)
        {
            return false;
        }

        Vector3 direction = toPlayer.normalized;
        float angleToPlayer = Vector3.Angle(eyePoint.forward, direction);
        if (angleToPlayer > flashlight.spotAngle * 0.5f)
        {
            return false;
        }

        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance, occluderMask, QueryTriggerInteraction.Ignore))
        {
            Transform hitRoot = hit.collider.transform.root;
            Transform playerRoot = playerTarget.root;
            if (hitRoot != playerRoot)
            {
                return false;
            }
        }

        return true;
    }

    private Vector3 GetPlayerAimPoint()
    {
        CapsuleCollider capsule = playerTarget.GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            return capsule.bounds.center;
        }

        Collider collider = playerTarget.GetComponent<Collider>();
        if (collider != null)
        {
            return collider.bounds.center;
        }

        return playerTarget.position + Vector3.up;
    }
}
