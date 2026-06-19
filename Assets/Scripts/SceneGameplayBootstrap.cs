using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections;

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

        GameObject exitDoor = FindInSceneByName(scene, "ending");
        if (exitDoor == null && scene.name == "level 1")
        {
            exitDoor = FindInSceneByName(scene, "Cube (4)");
        }

        string nextSceneName = GetNextSceneName(scene);
        if (exitDoor != null && !string.IsNullOrEmpty(nextSceneName))
        {
            LevelExitDoor levelExit = exitDoor.GetComponent<LevelExitDoor>();
            if (levelExit == null)
            {
                levelExit = exitDoor.AddComponent<LevelExitDoor>();
            }

            levelExit.ConfigureNextScene(nextSceneName);
        }
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

    private static string GetNextSceneName(Scene scene)
    {
        int buildIndex = SceneUtility.GetBuildIndexByScenePath(scene.path);
        int nextBuildIndex = buildIndex + 1;
        if (buildIndex < 0 || nextBuildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            return string.Empty;
        }

        string nextPath = SceneUtility.GetScenePathByBuildIndex(nextBuildIndex);
        return Path.GetFileNameWithoutExtension(nextPath);
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
