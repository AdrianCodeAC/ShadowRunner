using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private string autoWaypointRootName = "waypoints";
    [SerializeField] private float reachDistance = 0.2f;
    [SerializeField] private float fallbackPatrolDistance = 8f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float turnSpeed = 8f;
    [SerializeField] private float holdSecondsAtWaypoint = 5f;

    [Header("Three Point Lookout")]
    [SerializeField] private int middleWaypointIndex = 1;
    [SerializeField] private float endpointHoldSeconds = 2f;
    [SerializeField] private float middleHoldSeconds = 5f;
    [SerializeField] private string playerSpawnName = "PlayerSpawn";
    [SerializeField] private float middleLookYawOffset = 180f;

    private Rigidbody rb;
    private int currentWaypointIndex;
    private int direction = 1;
    private float holdTimer;
    private bool holdingAtWaypoint;
    private Quaternion lookoutRotation;
    private readonly List<GameObject> runtimeWaypointObjects = new List<GameObject>();
    private bool usingFallbackWaypoints;
    private Transform playerSpawn;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        lookoutRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        ResolveWaypoints();
        ResolvePlayerSpawn();
        currentWaypointIndex = usingFallbackWaypoints && waypoints.Length > 1 ? 1 : 0;
    }

    private void OnDestroy()
    {
        for (int i = 0; i < runtimeWaypointObjects.Count; i++)
        {
            if (runtimeWaypointObjects[i] != null)
            {
                Destroy(runtimeWaypointObjects[i]);
            }
        }
    }

    private void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        if (holdingAtWaypoint)
        {
            FaceHoldDirection();
            holdTimer += Time.fixedDeltaTime;
            if (holdTimer >= GetCurrentHoldSeconds())
            {
                holdingAtWaypoint = false;
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
        targetPosition.y = currentPosition.y;
        Vector3 toTarget = targetPosition - currentPosition;

        if (toTarget.magnitude <= reachDistance)
        {
            rb.MovePosition(targetPosition);
            holdingAtWaypoint = true;
            holdTimer = 0f;
            FaceHoldDirection();
            return;
        }

        Vector3 moveDirection = toTarget.normalized;
        rb.MovePosition(Vector3.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.fixedDeltaTime));
        Quaternion movementRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, movementRotation, turnSpeed * Time.fixedDeltaTime));
    }

    private void FaceLookoutDirection()
    {
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookoutRotation, turnSpeed * Time.fixedDeltaTime));
    }

    private void FaceHoldDirection()
    {
        if (!IsMiddleLookout() || playerSpawn == null)
        {
            FaceLookoutDirection();
            return;
        }

        Vector3 toSpawn = playerSpawn.position - rb.position;
        toSpawn.y = 0f;
        if (toSpawn.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion spawnRotation = Quaternion.LookRotation(toSpawn.normalized, Vector3.up);
        spawnRotation *= Quaternion.Euler(0f, middleLookYawOffset, 0f);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, spawnRotation, turnSpeed * Time.fixedDeltaTime));
    }

    private float GetCurrentHoldSeconds()
    {
        if (waypoints.Length != 3)
        {
            return holdSecondsAtWaypoint;
        }

        return IsMiddleLookout() ? middleHoldSeconds : endpointHoldSeconds;
    }

    private bool IsMiddleLookout()
    {
        return waypoints.Length == 3 && currentWaypointIndex == middleWaypointIndex;
    }

    private void ResolvePlayerSpawn()
    {
        if (waypoints == null || waypoints.Length != 3)
        {
            return;
        }

        GameObject spawnObject = GameObject.Find(playerSpawnName);
        if (spawnObject != null)
        {
            playerSpawn = spawnObject.transform;
        }
    }

    private void AdvanceWaypointIndex()
    {
        if (waypoints.Length <= 1)
        {
            currentWaypointIndex = 0;
            return;
        }

        currentWaypointIndex += direction;
        if (currentWaypointIndex >= waypoints.Length)
        {
            direction = -1;
            currentWaypointIndex = waypoints.Length - 2;
        }
        else if (currentWaypointIndex < 0)
        {
            direction = 1;
            currentWaypointIndex = 1;
        }
    }

    private void ResolveWaypoints()
    {
        List<Transform> resolved = new List<Transform>();
        if (waypoints != null)
        {
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    resolved.Add(waypoints[i]);
                }
            }
        }

        if (resolved.Count == 0)
        {
            GameObject root = GameObject.Find(autoWaypointRootName);
            if (root != null)
            {
                for (int i = 0; i < root.transform.childCount; i++)
                {
                    resolved.Add(root.transform.GetChild(i));
                }
            }
        }

        if (resolved.Count == 0)
        {
            CreateFallbackWaypoints(resolved);
        }

        waypoints = resolved.ToArray();
    }

    private void CreateFallbackWaypoints(List<Transform> resolved)
    {
        usingFallbackWaypoints = true;
        Vector3 start = transform.position;
        Vector3 patrolDirection = transform.forward;
        patrolDirection.y = 0f;
        if (patrolDirection.sqrMagnitude < 0.001f)
        {
            patrolDirection = Vector3.forward;
        }
        patrolDirection.Normalize();

        CreateRuntimeWaypoint($"{name}_Waypoint_A", start, resolved);
        CreateRuntimeWaypoint($"{name}_Waypoint_B", start + patrolDirection * fallbackPatrolDistance, resolved);
    }

    private void CreateRuntimeWaypoint(string waypointName, Vector3 position, List<Transform> resolved)
    {
        GameObject waypoint = new GameObject(waypointName);
        waypoint.transform.SetPositionAndRotation(position, lookoutRotation);
        SceneManager.MoveGameObjectToScene(waypoint, gameObject.scene);
        runtimeWaypointObjects.Add(waypoint);
        resolved.Add(waypoint.transform);
    }
}
