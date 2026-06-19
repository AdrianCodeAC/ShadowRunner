using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneGameplayBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        GameObject player = FindInSceneByTag(activeScene, "Player");
        if (player != null)
        {
            if (player.GetComponent<PlayerRespawn>() == null)
            {
                player.AddComponent<PlayerRespawn>();
            }

            if (player.GetComponent<PlayerInteractor>() == null)
            {
                player.AddComponent<PlayerInteractor>();
            }
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
            GameObject exitDoor = FindInSceneByName(activeScene, "Cube (4)");
            if (exitDoor != null && exitDoor.GetComponent<LevelExitDoor>() == null)
            {
                exitDoor.AddComponent<LevelExitDoor>();
            }
        }
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
        if (root.name == objectName)
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
