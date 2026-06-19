using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private string spawnPointName = "PlayerSpawn";
    [SerializeField] private float respawnDelaySeconds = 0.05f;

    private Health health;
    private Rigidbody rb;
    private CharacterController characterController;
    private Transform spawnPoint;
    private Vector3 fallbackSpawnPosition;
    private Quaternion fallbackSpawnRotation;
    private bool respawning;

    private void Awake()
    {
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody>();
        characterController = GetComponent<CharacterController>();

        fallbackSpawnPosition = transform.position;
        fallbackSpawnRotation = transform.rotation;
        ResolveSpawnPoint();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDied += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDied -= HandleDeath;
        }
    }

    private void ResolveSpawnPoint()
    {
        GameObject spawnObject = GameObject.Find(spawnPointName);
        if (spawnObject != null)
        {
            spawnPoint = spawnObject.transform;
        }
    }

    private void HandleDeath()
    {
        if (!respawning)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        respawning = true;
        yield return new WaitForSeconds(respawnDelaySeconds);

        Vector3 targetPosition;
        Quaternion targetRotation;

        if (spawnPoint != null)
        {
            targetPosition = spawnPoint.position;
            targetRotation = spawnPoint.rotation;
        }
        else
        {
            targetPosition = fallbackSpawnPosition;
            targetRotation = fallbackSpawnRotation;
        }

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = targetPosition;
            rb.rotation = targetRotation;
        }

        transform.SetPositionAndRotation(targetPosition, targetRotation);

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        health.HealToFull();
        respawning = false;
    }
}
