using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneGameplayBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        GameObject player = FindInSceneByTag(activeScene, "Player");
        if (player == null)
        {
            player = FindInSceneByName(activeScene, "Player");
        }

        if (player != null)
        {
            EnsurePlayerSetup(player);
        }

        if (activeScene.name == "level 2")
        {
            GameObject generator = FindInSceneByName(activeScene, "generator");
            if (generator != null && generator.GetComponent<GeneratorInteraction>() == null)
            {
                generator.AddComponent<GeneratorInteraction>();
            }
        }

        if (activeScene.name == "level 1")
        {
            GameObject exitDoor = FindInSceneByName(activeScene, "ending");
            if (exitDoor == null)
            {
                exitDoor = FindInSceneByName(activeScene, "Cube (4)");
            }

            if (exitDoor != null && exitDoor.GetComponent<LevelExitDoor>() == null)
            {
                exitDoor.AddComponent<LevelExitDoor>();
            }
        }
    }

    private static void EnsurePlayerSetup(GameObject player)
    {
        EnsureComponent<Health>(player);
        EnsureComponent<Rigidbody>(player);
        EnsureComponent<PlayerMovement>(player);
        EnsureComponent<ShadowExposureDamage>(player);
        EnsureComponent<PlayerRespawn>(player);
        EnsureComponent<PlayerInteractor>(player);
        EnsureComponent<ShadowStatusUI>(player);

        Camera playerCamera = player.GetComponentInChildren<Camera>(true);
        if (playerCamera != null)
        {
            EnsureComponent<MouseLook>(playerCamera.gameObject);
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
