using UnityEngine;

public class EnemySpinInPlace : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = -37.5f;

    private Rigidbody rb;
    private Vector3 spinCenter;

    private void Awake()
    {
        EnemyPatrol patrol = GetComponent<EnemyPatrol>();
        if (patrol != null)
        {
            patrol.enabled = false;
        }

        rb = GetComponent<Rigidbody>();
        Collider bodyCollider = GetComponent<Collider>();
        if (bodyCollider == null)
        {
            bodyCollider = GetComponentInChildren<Collider>(true);
        }
        spinCenter = bodyCollider != null ? bodyCollider.bounds.center : transform.position;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;
        }
    }

    private void FixedUpdate()
    {
        float yaw = degreesPerSecond * Time.fixedDeltaTime;
        Quaternion rotationStep = Quaternion.AngleAxis(yaw, Vector3.up);
        if (rb != null)
        {
            Vector3 compensatedPosition = spinCenter + rotationStep * (rb.position - spinCenter);
            rb.MovePosition(compensatedPosition);
            rb.MoveRotation(rotationStep * rb.rotation);
            return;
        }

        transform.RotateAround(spinCenter, Vector3.up, yaw);
    }
}
