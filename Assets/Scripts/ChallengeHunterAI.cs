using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ChallengeHunterAI : MonoBehaviour
{
    private enum HunterState
    {
        Wander,
        Investigate,
        Chase,
        Search
    }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.8f;
    [SerializeField] private float chaseSpeed = 5.2f;
    [SerializeField] private float wanderRadius = 14f;

    [Header("Sight")]
    [SerializeField] private float sightDistance = 22f;
    [SerializeField] private float fieldOfView = 95f;
    [SerializeField] private float searchSeconds = 8f;

    public float FlashlightDamagePerSecond => 10f;

    private NavMeshAgent agent;
    private Transform player;
    private Light flashlight;
    private HunterState state;
    private Vector3 homePosition;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 investigationPosition;
    private float searchTimer;
    private float destinationTimer;
    private bool configured;
    private bool navigationStarted;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.acceleration = 12f;
        agent.angularSpeed = 420f;
        agent.stoppingDistance = 1.1f;
        agent.autoBraking = true;

        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
        }

        homePosition = transform.position;
        MakeBodyRed();
        CreateFlashlight();
    }

    public void Configure(Transform playerTarget)
    {
        if (configured)
        {
            return;
        }

        configured = true;
        player = playerTarget;
        GeneratorInteraction[] generators = FindObjectsOfType<GeneratorInteraction>(true);
        for (int i = 0; i < generators.Length; i++)
        {
            if (generators[i].gameObject.scene == gameObject.scene)
            {
                generators[i].Disabled += OnGeneratorDisabled;
            }
        }
    }

    public void StartNavigation()
    {
        if (navigationStarted)
        {
            return;
        }

        navigationStarted = true;
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 8f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            homePosition = hit.position;
            state = HunterState.Wander;
            PickDestinationNear(homePosition, wanderRadius);
        }
    }

    private void OnDestroy()
    {
        GeneratorInteraction[] generators = FindObjectsOfType<GeneratorInteraction>(true);
        for (int i = 0; i < generators.Length; i++)
        {
            generators[i].Disabled -= OnGeneratorDisabled;
        }
    }

    private void Update()
    {
        if (!configured || player == null || !agent.isOnNavMesh)
        {
            return;
        }

        bool seesPlayer = CanSeePlayer();
        if (seesPlayer)
        {
            state = HunterState.Chase;
            lastKnownPlayerPosition = player.position;
            agent.speed = chaseSpeed;
            agent.SetDestination(lastKnownPlayerPosition);
            return;
        }

        if (state == HunterState.Chase)
        {
            state = HunterState.Search;
            searchTimer = searchSeconds;
            agent.speed = moveSpeed;
            agent.SetDestination(lastKnownPlayerPosition);
        }

        switch (state)
        {
            case HunterState.Investigate:
                UpdateInvestigation();
                break;
            case HunterState.Search:
                UpdateSearch();
                break;
            default:
                UpdateWander();
                break;
        }
    }

    private void UpdateInvestigation()
    {
        agent.speed = moveSpeed;
        agent.SetDestination(investigationPosition);
        if (!HasReachedDestination())
        {
            return;
        }

        state = HunterState.Search;
        searchTimer = searchSeconds;
        PickDestinationNear(investigationPosition, 7f);
    }

    private void UpdateSearch()
    {
        searchTimer -= Time.deltaTime;
        destinationTimer -= Time.deltaTime;
        if (searchTimer <= 0f)
        {
            state = HunterState.Wander;
            PickDestinationNear(transform.position, wanderRadius);
            return;
        }

        if (HasReachedDestination() || destinationTimer <= 0f)
        {
            PickDestinationNear(lastKnownPlayerPosition, 9f);
        }
    }

    private void UpdateWander()
    {
        destinationTimer -= Time.deltaTime;
        if (HasReachedDestination() || destinationTimer <= 0f)
        {
            PickDestinationNear(homePosition, wanderRadius);
        }
    }

    private void OnGeneratorDisabled(GeneratorInteraction generator)
    {
        if (generator == null || generator.gameObject.scene != gameObject.scene)
        {
            return;
        }

        investigationPosition = generator.transform.position;
        lastKnownPlayerPosition = investigationPosition;
        state = HunterState.Investigate;
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(investigationPosition);
        }
    }

    private bool CanSeePlayer()
    {
        Vector3 origin = flashlight != null ? flashlight.transform.position : transform.position + Vector3.up;
        Vector3 target = GetPlayerAimPoint();
        Vector3 toPlayer = target - origin;
        float distance = toPlayer.magnitude;
        if (distance > sightDistance || distance < 0.01f)
        {
            return false;
        }

        Vector3 direction = toPlayer / distance;
        if (Vector3.Angle(transform.forward, direction) > fieldOfView * 0.5f)
        {
            return false;
        }

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Transform hit = hits[i].collider.transform;
            if (hit == transform || hit.IsChildOf(transform))
            {
                continue;
            }

            return hit == player || hit.IsChildOf(player) || player.IsChildOf(hit);
        }

        return true;
    }

    private Vector3 GetPlayerAimPoint()
    {
        Collider playerCollider = player.GetComponent<Collider>();
        if (playerCollider == null)
        {
            playerCollider = player.GetComponentInChildren<Collider>();
        }

        return playerCollider != null ? playerCollider.bounds.center : player.position + Vector3.up;
    }

    private bool HasReachedDestination()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f;
    }

    private void PickDestinationNear(Vector3 center, float radius)
    {
        destinationTimer = Random.Range(2.5f, 5f);
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector2 random = Random.insideUnitCircle * radius;
            Vector3 candidate = center + new Vector3(random.x, 0f, random.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }

        agent.SetDestination(transform.position);
    }

    private void MakeBodyRed()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = new Color(0.8f, 0.04f, 0.03f, 1f);
        }
    }

    private void CreateFlashlight()
    {
        GameObject holder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        holder.name = "Hunter Flashlight";
        holder.transform.SetParent(transform, false);
        holder.transform.localPosition = new Vector3(0.35f, 0.25f, 0.55f);
        holder.transform.localScale = new Vector3(0.16f, 0.16f, 0.5f);
        holder.GetComponent<Renderer>().material.color = new Color(0.08f, 0.08f, 0.07f, 1f);
        Destroy(holder.GetComponent<Collider>());

        GameObject lightObject = new GameObject("Hunter Spotlight");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localPosition = new Vector3(0.35f, 0.35f, 0.85f);
        flashlight = lightObject.AddComponent<Light>();
        flashlight.type = LightType.Spot;
        flashlight.color = new Color(1f, 0.93f, 0.72f, 1f);
        flashlight.intensity = 500f;
        flashlight.range = sightDistance;
        flashlight.spotAngle = 55f;
        flashlight.shadows = LightShadows.Hard;
    }
}
