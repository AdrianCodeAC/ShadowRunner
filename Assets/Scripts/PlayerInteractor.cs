using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private float searchRadius = 5f;
    private GeneratorInteraction activeGenerator;
    private GUIStyle promptStyle;

    private void Update()
    {
        activeGenerator = FindClosestGenerator();

        if (activeGenerator != null && IsInteractPressed())
        {
            activeGenerator.Interact();
        }
    }

    private void OnGUI()
    {
        if (activeGenerator == null)
        {
            return;
        }

        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
            promptStyle.normal.textColor = Color.white;
        }

        GUI.depth = -100;
        Rect box = new Rect(Screen.width * 0.5f - 180f, Screen.height - 140f, 360f, 56f);
        GUI.Box(box, string.Empty);
        GUI.Label(box, activeGenerator.GetPromptText(), promptStyle);
    }

    private GeneratorInteraction FindClosestGenerator()
    {
        GeneratorInteraction[] generators = FindObjectsOfType<GeneratorInteraction>();
        if (generators.Length == 0)
        {
            TryInstallGeneratorInteraction();
            generators = FindObjectsOfType<GeneratorInteraction>();
        }

        GeneratorInteraction closest = null;
        float closestDistance = searchRadius;

        for (int i = 0; i < generators.Length; i++)
        {
            GeneratorInteraction candidate = generators[i];
            if (candidate == null || !candidate.CanInteract(transform))
            {
                continue;
            }

            float distance = candidate.GetDistanceTo(transform);
            if (distance <= closestDistance)
            {
                closestDistance = distance;
                closest = candidate;
            }
        }

        return closest;
    }

    private static void TryInstallGeneratorInteraction()
    {
        UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            InstallInHierarchy(roots[i].transform, false);
        }
    }

    private static void InstallInHierarchy(Transform current, bool generatorAncestor)
    {
        bool isGenerator = current.name.ToLowerInvariant().Contains("generator");
        if (isGenerator && !generatorAncestor && current.GetComponent<GeneratorInteraction>() == null)
        {
            current.gameObject.AddComponent<GeneratorInteraction>();
        }

        bool hasGeneratorAncestor = generatorAncestor || isGenerator;
        for (int i = 0; i < current.childCount; i++)
        {
            InstallInHierarchy(current.GetChild(i), hasGeneratorAncestor);
        }
    }

    private bool IsInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }
}
