using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private string autoWaypointRootName = "waypoints";
    [SerializeField] private bool loop = true;
    [SerializeField] private float reachDistance = 0.2f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float turnSpeed = 8f;

    [Header("Middle Stop")]
    [SerializeField] private int middleWaypointIndex = 1;
    [SerializeField] private float middleHoldSeconds = 2f;
    [SerializeField] private Transform playerLookTarget;
    [SerializeField] private string autoPlayerLookTargetName = "PlayerSpawn";

    private Rigidbody rb;
    private readonly List<Transform> resolvedWaypoints = new List<Transform>();
    private int currentWaypointIndex;
    private int direction = 1;
    private float holdTimer;
    private bool holdingAtMiddle;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        ResolveWaypoints();
        ResolvePlayerLookTarget();
        currentWaypointIndex = 0;
    }

    private void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 0, waypoints.Length - 1);

        if (holdingAtMiddle)
        {
            FacePlayerLookTarget();
            holdTimer += Time.fixedDeltaTime;

            if (holdTimer >= middleHoldSeconds)
            {
                holdingAtMiddle = false;
                holdTimer = 0f;
                AdvanceWaypointIndex();
            }

            return;
        }

        Transform target = waypoints[currentWaypointIndex];
        if (target == null)
        {
            AdvanceWaypointIndex();
            return;
        }

        Vector3 currentPosition = rb.position;
        Vector3 targetPosition = target.position;
        Vector3 flatTarget = new Vector3(targetPosition.x, currentPosition.y, targetPosition.z);
        Vector3 toTarget = flatTarget - currentPosition;

        if (toTarget.magnitude <= reachDistance)
        {
            if (currentWaypointIndex == middleWaypointIndex)
            {
                holdingAtMiddle = true;
                holdTimer = 0f;
                FacePlayerLookTarget();
                return;
            }

            AdvanceWaypointIndex();
            return;
        }

        Vector3 moveDirection = toTarget.normalized;
        Vector3 nextPosition = Vector3.MoveTowards(currentPosition, flatTarget, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(nextPosition);

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            Quaternion nextRotation = Quaternion.Slerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(nextRotation);
        }
    }

    private void AdvanceWaypointIndex()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        if (waypoints.Length == 1)
        {
            currentWaypointIndex = 0;
            return;
        }

        currentWaypointIndex += direction;

        if (currentWaypointIndex >= waypoints.Length)
        {
            if (!loop)
            {
                currentWaypointIndex = waypoints.Length - 1;
                direction = -1;
                return;
            }

            direction = -1;
            currentWaypointIndex = waypoints.Length - 2;
        }
        else if (currentWaypointIndex < 0)
        {
            if (!loop)
            {
                currentWaypointIndex = 0;
                direction = 1;
                return;
            }

            direction = 1;
            currentWaypointIndex = 1;
        }

        currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 0, waypoints.Length - 1);
    }

    private void FacePlayerLookTarget()
    {
        if (playerLookTarget == null)
        {
            return;
        }

        Vector3 flatLook = playerLookTarget.position;
        flatLook.y = rb.position.y;
        Vector3 toTarget = flatLook - rb.position;
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
    }

    private void ResolvePlayerLookTarget()
    {
        if (playerLookTarget != null)
        {
            return;
        }

        GameObject spawn = GameObject.Find(autoPlayerLookTargetName);
        if (spawn != null)
        {
            playerLookTarget = spawn.transform;
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerLookTarget = playerObject.transform;
        }
    }

    private void ResolveWaypoints()
    {
        resolvedWaypoints.Clear();

        if (waypoints != null)
        {
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    resolvedWaypoints.Add(waypoints[i]);
                }
            }
        }

        if (resolvedWaypoints.Count == 0)
        {
            GameObject waypointRoot = GameObject.Find(autoWaypointRootName);
            if (waypointRoot != null)
            {
                Transform rootTransform = waypointRoot.transform;
                for (int i = 0; i < rootTransform.childCount; i++)
                {
                    Transform child = rootTransform.GetChild(i);
                    if (child != null)
                    {
                        resolvedWaypoints.Add(child);
                    }
                }
            }
        }

        if (resolvedWaypoints.Count > 1)
        {
            resolvedWaypoints.Sort(CompareWaypointPositions);
        }

        waypoints = resolvedWaypoints.ToArray();

        if (waypoints.Length >= 3)
        {
            middleWaypointIndex = Mathf.Clamp(middleWaypointIndex, 1, waypoints.Length - 2);
        }
        else
        {
            middleWaypointIndex = -1;
        }
    }

    private static int CompareWaypointPositions(Transform a, Transform b)
    {
        Vector3 pa = a.position;
        Vector3 pb = b.position;

        int xCompare = pa.x.CompareTo(pb.x);
        if (xCompare != 0)
        {
            return xCompare;
        }

        int zCompare = pa.z.CompareTo(pb.z);
        if (zCompare != 0)
        {
            return zCompare;
        }

        return pa.y.CompareTo(pb.y);
    }
}
