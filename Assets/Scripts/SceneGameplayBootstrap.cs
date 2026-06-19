using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.AI.Navigation;
using UnityEngine.AI;

public class SceneGameplayBootstrap : MonoBehaviour
{
    private static SceneGameplayBootstrap runner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadCallback()
    {
        if (runner == null)
        {
            GameObject runnerObject = new GameObject("Scene Gameplay Bootstrap");
            DontDestroyOnLoad(runnerObject);
            runner = runnerObject.AddComponent<SceneGameplayBootstrap>();
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        SetupScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        SetupScene(scene);
        if (runner != null)
        {
            runner.StartCoroutine(SetupSceneAfterAwake(scene));
        }
    }

    private static IEnumerator SetupSceneAfterAwake(Scene scene)
    {
        yield return null;
        SetupScene(scene);
    }

    private static void SetupScene(Scene scene)
    {
        if (!scene.isLoaded)
        {
            return;
        }

        if (string.Equals(scene.name, "menu", System.StringComparison.OrdinalIgnoreCase))
        {
            EnsureMainMenu(scene);
            return;
        }

        ApplyEnvironmentLighting(scene);

        GameObject player = FindInSceneByTag(scene, "Player");
        if (player == null)
        {
            player = FindInSceneByName(scene, "Player");
        }

        if (player != null)
        {
            EnsurePlayerSetup(player);
        }

        EnsureGeneratorSetup(scene);

        if (string.Equals(scene.name, "level 5", System.StringComparison.OrdinalIgnoreCase))
        {
            EnsureLevelFiveEnemiesSpin(scene);
        }

        GameObject exitDoor = FindInSceneByName(scene, "ending");
        if (exitDoor == null && scene.name == "level 1")
        {
            exitDoor = FindInSceneByName(scene, "Cube (4)");
        }

        string nextSceneName = LevelProgress.GetNextSceneName(scene.name);
        if (exitDoor != null)
        {
            LevelExitDoor levelExit = exitDoor.GetComponent<LevelExitDoor>();
            if (levelExit == null)
            {
                levelExit = exitDoor.AddComponent<LevelExitDoor>();
            }

            if (!string.IsNullOrEmpty(nextSceneName))
            {
                levelExit.ConfigureNextScene(nextSceneName);
            }
            else if (string.Equals(scene.name, "level 5", System.StringComparison.OrdinalIgnoreCase))
            {
                levelExit.ConfigureFinalLevel();
            }
            else if (string.Equals(scene.name, "challenge1", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(scene.name, "challenge2", System.StringComparison.OrdinalIgnoreCase))
            {
                levelExit.ConfigureNextScene("menu");
            }
        }

        if (string.Equals(scene.name, "challenge1", System.StringComparison.OrdinalIgnoreCase))
        {
            EnsureChallengeOneSetup(scene, exitDoor);
            EnsureChallengeOneHunter(scene, player);
        }
    }

    private static void EnsureChallengeOneHunter(Scene scene, GameObject player)
    {
        if (player == null)
        {
            return;
        }

        EnsureComponent<ChallengeDeathReset>(player);
        GameObject hunterObject = FindChallengeHunter(scene);
        if (hunterObject == null)
        {
            hunterObject = CreateChallengeHunter(scene, player.transform);
        }

        hunterObject.transform.position = GetGeneratorFourSpawn(scene, player.transform);
        Vector3 lookDirection = player.transform.position - hunterObject.transform.position;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude > 0.01f)
        {
            hunterObject.transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        ChallengeHunterAI hunter = EnsureComponent<ChallengeHunterAI>(hunterObject);
        hunter.Configure(player.transform);

        GameObject navigationObject = FindInSceneByName(scene, "Challenge 1 Runtime NavMesh");
        if (navigationObject == null)
        {
            navigationObject = new GameObject("Challenge 1 Runtime NavMesh");
            SceneManager.MoveGameObjectToScene(navigationObject, scene);
        }

        NavMeshSurface surface = EnsureComponent<NavMeshSurface>(navigationObject);
        if (surface.navMeshData == null)
        {
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            bool playerWasActive = player.activeSelf;
            player.SetActive(false);
            surface.BuildNavMesh();
            player.SetActive(playerWasActive);
        }

        hunter.StartNavigation();

        ShadowExposureDamage exposure = player.GetComponent<ShadowExposureDamage>();
        if (exposure != null)
        {
            exposure.RefreshLightSources();
        }
    }

    private static GameObject CreateChallengeHunter(Scene scene, Transform player)
    {
        GameObject hunter = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hunter.name = "Challenge Hunter";
        SceneManager.MoveGameObjectToScene(hunter, scene);

        Vector3 spawnPosition = player.position + new Vector3(12f, 0f, 12f);
        GeneratorInteraction[] generators = FindObjectsOfType<GeneratorInteraction>(true);
        float farthestDistance = -1f;
        for (int i = 0; i < generators.Length; i++)
        {
            if (generators[i].gameObject.scene != scene)
            {
                continue;
            }

            float distance = (generators[i].transform.position - player.position).sqrMagnitude;
            if (distance > farthestDistance)
            {
                farthestDistance = distance;
                Vector3 awayFromPlayer = generators[i].transform.position - player.position;
                awayFromPlayer.y = 0f;
                spawnPosition = generators[i].transform.position + awayFromPlayer.normalized * 2.5f;
                spawnPosition.y = player.position.y;
            }
        }

        hunter.transform.position = spawnPosition;
        hunter.transform.rotation = Quaternion.LookRotation(player.position - spawnPosition, Vector3.up);

        Rigidbody body = hunter.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        return hunter;
    }

    private static Vector3 GetGeneratorFourSpawn(Scene scene, Transform player)
    {
        GeneratorInteraction[] generators = FindObjectsOfType<GeneratorInteraction>(true);
        for (int i = 0; i < generators.Length; i++)
        {
            GeneratorInteraction generator = generators[i];
            if (generator.gameObject.scene != scene)
            {
                continue;
            }

            string compactName = generator.name.ToLowerInvariant().Replace(" ", string.Empty);
            if (!compactName.Contains("generator4"))
            {
                continue;
            }

            Vector3 awayFromPlayer = generator.transform.position - player.position;
            awayFromPlayer.y = 0f;
            if (awayFromPlayer.sqrMagnitude < 0.01f)
            {
                awayFromPlayer = Vector3.right;
            }

            Vector3 spawn = generator.transform.position + awayFromPlayer.normalized * 2.5f;
            spawn.y = player.position.y;
            return spawn;
        }

        return player.position + new Vector3(12f, 0f, 12f);
    }

    private static GameObject FindChallengeHunter(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject match = FindChallengeHunterInHierarchy(roots[i].transform);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static GameObject FindChallengeHunterInHierarchy(Transform current)
    {
        string loweredName = current.name.ToLowerInvariant();
        bool candidateName = loweredName.Contains("enemy") || loweredName.Contains("hunter") ||
            string.Equals(loweredName, "capsule", System.StringComparison.OrdinalIgnoreCase);
        bool excluded = current.CompareTag("Player") || current.GetComponent<PlayerMovement>() != null ||
            current.GetComponentInParent<GeneratorInteraction>() != null;
        if (candidateName && !excluded && current.GetComponentInChildren<Renderer>(true) != null)
        {
            return current.gameObject;
        }

        for (int i = 0; i < current.childCount; i++)
        {
            GameObject match = FindChallengeHunterInHierarchy(current.GetChild(i));
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static void EnsureChallengeOneSetup(Scene scene, GameObject exitDoor)
    {
        GameObject controllerObject = FindInSceneByName(scene, "Challenge 1 Generator Objective");
        if (controllerObject == null)
        {
            controllerObject = new GameObject("Challenge 1 Generator Objective");
            SceneManager.MoveGameObjectToScene(controllerObject, scene);
        }

        ChallengeGeneratorObjective objective = EnsureComponent<ChallengeGeneratorObjective>(controllerObject);
        objective.Initialize(exitDoor);
    }

    private static void EnsureMainMenu(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].GetComponentInChildren<MainMenuController>(true) != null)
            {
                return;
            }
        }

        GameObject menuController = new GameObject("Main Menu Controller");
        SceneManager.MoveGameObjectToScene(menuController, scene);
        menuController.AddComponent<MainMenuController>();
    }

    private static void ApplyEnvironmentLighting(Scene scene)
    {
        bool isChallengeOne = string.Equals(scene.name, "challenge1", System.StringComparison.OrdinalIgnoreCase);
        RenderSettings.ambientIntensity = isChallengeOne ? 0.45f : 0.25f;
        RenderSettings.reflectionIntensity = isChallengeOne ? 0.35f : 0.2f;

        Light[] lights = FindObjectsOfType<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            Light lightSource = lights[i];
            if (lightSource.gameObject.scene != scene || lightSource.type != LightType.Directional ||
                lightSource.GetComponentInParent<EnemyPatrol>() != null ||
                lightSource.GetComponentInParent<GuardVisionDamage>() != null)
            {
                continue;
            }

            float maximumIntensity = isChallengeOne ? 0.7f : 0.45f;
            lightSource.intensity = Mathf.Min(lightSource.intensity, maximumIntensity);
        }
    }

    private static void EnsurePlayerSetup(GameObject player)
    {
        EnsureComponent<Health>(player);
        EnsureComponent<Rigidbody>(player);
        EnsureComponent<PlayerMovement>(player);
        ShadowExposureDamage exposure = EnsureComponent<ShadowExposureDamage>(player);
        exposure.ConfigureStandardRates();
        exposure.RefreshLightSources();
        EnsureComponent<PlayerRespawn>(player);
        EnsureComponent<PlayerInteractor>(player);
        EnsureComponent<ShadowStatusUI>(player);
        EnsureComponent<PauseMenuController>(player);
        EnsureComponent<PlayerAudioFeedback>(player);

        Camera playerCamera = player.GetComponentInChildren<Camera>(true);
        if (playerCamera != null)
        {
            EnsureComponent<MouseLook>(playerCamera.gameObject);
        }
    }

    private static void EnsureGeneratorSetup(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            EnsureGeneratorsInHierarchy(roots[i].transform, false);
        }
    }

    private static void EnsureLevelFiveEnemiesSpin(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            EnsureSpinnersInHierarchy(roots[i].transform, false);
        }
    }

    private static void EnsureSpinnersInHierarchy(Transform current, bool enemyAncestor)
    {
        bool isEnemy = current.name.ToLowerInvariant().Contains("enemy") ||
            current.GetComponent<EnemyPatrol>() != null ||
            current.GetComponent<GuardVisionDamage>() != null;
        if (isEnemy && !enemyAncestor)
        {
            EnemyPatrol patrol = current.GetComponent<EnemyPatrol>();
            if (patrol != null)
            {
                patrol.enabled = false;
            }

            EnsureComponent<EnemySpinInPlace>(current.gameObject);
        }

        bool hasEnemyAncestor = enemyAncestor || isEnemy;
        for (int i = 0; i < current.childCount; i++)
        {
            EnsureSpinnersInHierarchy(current.GetChild(i), hasEnemyAncestor);
        }
    }

    private static void EnsureGeneratorsInHierarchy(Transform current, bool generatorAncestor)
    {
        bool isGenerator = current.name.ToLowerInvariant().Contains("generator");
        if (isGenerator && !generatorAncestor && current.GetComponent<GeneratorInteraction>() == null)
        {
            current.gameObject.AddComponent<GeneratorInteraction>();
        }

        bool hasGeneratorAncestor = generatorAncestor || isGenerator;
        for (int i = 0; i < current.childCount; i++)
        {
            EnsureGeneratorsInHierarchy(current.GetChild(i), hasGeneratorAncestor);
        }
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static GameObject FindInSceneByName(Scene scene, string objectName)
    {
        if (!scene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject match = FindInHierarchy(roots[i].transform, objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static GameObject FindInSceneByTag(Scene scene, string tag)
    {
        if (!scene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform transform = roots[i].transform;
            if (roots[i].CompareTag(tag))
            {
                return roots[i];
            }

            GameObject match = FindTaggedInHierarchy(transform, tag);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static GameObject FindInHierarchy(Transform root, string objectName)
    {
        if (string.Equals(root.name, objectName, System.StringComparison.OrdinalIgnoreCase))
        {
            return root.gameObject;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            GameObject match = FindInHierarchy(root.GetChild(i), objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static GameObject FindTaggedInHierarchy(Transform root, string tag)
    {
        if (root.CompareTag(tag))
        {
            return root.gameObject;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            GameObject match = FindTaggedInHierarchy(root.GetChild(i), tag);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}
