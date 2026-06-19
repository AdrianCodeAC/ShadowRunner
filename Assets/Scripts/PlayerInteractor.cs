using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private float searchRadius = 3f;
    [SerializeField] private string promptText = "Press E to disable";

    private GeneratorInteraction activeGenerator;

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

        Rect box = new Rect(Screen.width * 0.5f - 140f, Screen.height - 110f, 280f, 40f);
        GUI.Box(box, string.Empty);
        GUI.Label(new Rect(box.x + 12f, box.y + 10f, box.width - 24f, 20f), promptText);
    }

    private GeneratorInteraction FindClosestGenerator()
    {
        GeneratorInteraction[] generators = FindObjectsOfType<GeneratorInteraction>();
        GeneratorInteraction closest = null;
        float closestDistance = searchRadius;

        for (int i = 0; i < generators.Length; i++)
        {
            GeneratorInteraction candidate = generators[i];
            if (candidate == null || !candidate.CanInteract(transform))
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            if (distance <= closestDistance)
            {
                closestDistance = distance;
                closest = candidate;
            }
        }

        return closest;
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
