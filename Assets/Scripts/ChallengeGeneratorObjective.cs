using System;
using System.Collections.Generic;
using UnityEngine;

public class ChallengeGeneratorObjective : MonoBehaviour
{
    private const int RequiredGeneratorCount = 4;

    private GeneratorInteraction[] generators;
    private LevelExitDoor exitDoor;
    private Renderer blockerRenderer;
    private Collider blockerCollider;
    private int remainingGenerators = RequiredGeneratorCount;
    private bool questComplete;
    private GUIStyle questStyle;
    private bool initialized;

    public void Initialize(GameObject ending)
    {
        if (initialized || ending == null)
        {
            return;
        }

        generators = FindObjectsOfType<GeneratorInteraction>(true);
        Array.Sort(generators, CompareGeneratorOrder);
        if (generators.Length < RequiredGeneratorCount)
        {
            return;
        }

        initialized = true;
        exitDoor = ending.GetComponent<LevelExitDoor>();
        if (exitDoor != null)
        {
            exitDoor.enabled = false;
        }

        blockerRenderer = FindRedBlocker(ending.transform);
        blockerCollider = blockerRenderer != null ? blockerRenderer.GetComponent<Collider>() : null;

        PairGeneratorLights(ending.gameObject.scene);

        for (int i = 0; i < RequiredGeneratorCount; i++)
        {
            generators[i].Disabled += OnGeneratorDisabled;
        }

        RefreshGate();
    }

    private static int CompareGeneratorOrder(GeneratorInteraction left, GeneratorInteraction right)
    {
        string leftGroup = GetGeneratorGroupName(left.transform);
        string rightGroup = GetGeneratorGroupName(right.transform);
        int numberComparison = ExtractNumber(leftGroup).CompareTo(ExtractNumber(rightGroup));
        if (numberComparison != 0)
        {
            return numberComparison;
        }

        int nameComparison = string.Compare(leftGroup, rightGroup, StringComparison.OrdinalIgnoreCase);
        if (nameComparison != 0)
        {
            return nameComparison;
        }

        return left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex());
    }

    private static string GetGeneratorGroupName(Transform generator)
    {
        return generator.parent != null ? generator.parent.name : generator.name;
    }

    private static int ExtractNumber(string value)
    {
        int number = 0;
        bool foundDigit = false;
        for (int i = 0; i < value.Length; i++)
        {
            if (!char.IsDigit(value[i]))
            {
                continue;
            }

            foundDigit = true;
            number = number * 10 + (value[i] - '0');
        }

        return foundDigit ? number : 0;
    }

    private void PairGeneratorLights(UnityEngine.SceneManagement.Scene scene)
    {
        List<Light> availableLights = new List<Light>();
        Light[] sceneLights = FindObjectsOfType<Light>(true);
        for (int i = 0; i < sceneLights.Length; i++)
        {
            Light candidate = sceneLights[i];
            GeneratorInteraction owner = candidate.GetComponentInParent<GeneratorInteraction>();
            bool autoCreatedOnGenerator = owner != null && candidate.gameObject == owner.gameObject;
            if (candidate.gameObject.scene == scene && candidate.type != LightType.Directional &&
                candidate.GetComponentInParent<EnemyPatrol>() == null &&
                candidate.GetComponentInParent<GuardVisionDamage>() == null && !autoCreatedOnGenerator)
            {
                availableLights.Add(candidate);
            }
        }

        List<Renderer> availableBulbs = new List<Renderer>();
        Renderer[] sceneRenderers = FindObjectsOfType<Renderer>(true);
        for (int i = 0; i < sceneRenderers.Length; i++)
        {
            Renderer candidate = sceneRenderers[i];
            string loweredName = candidate.name.ToLowerInvariant();
            if (candidate.gameObject.scene == scene &&
                (loweredName.Contains("bulb") || loweredName.Contains("light")) &&
                candidate.GetComponentInParent<EnemyPatrol>() == null)
            {
                availableBulbs.Add(candidate);
            }
        }

        bool[] usesOwnedBulb = new bool[RequiredGeneratorCount - 1];
        for (int i = 0; i < RequiredGeneratorCount - 1; i++)
        {
            usesOwnedBulb[i] = generators[i].TryConfigureFromOwnHierarchy();
            if (usesOwnedBulb[i])
            {
                availableLights.Remove(generators[i].ControlledLight);
                availableBulbs.Remove(generators[i].ControlledRenderer);
            }
        }

        for (int i = 0; i < RequiredGeneratorCount - 1; i++)
        {
            if (usesOwnedBulb[i])
            {
                continue;
            }

            Light assignedLight = TakeNearestLight(generators[i].transform.position, availableLights);
            Vector3 rendererOrigin = assignedLight != null
                ? assignedLight.transform.position
                : generators[i].transform.position;
            Renderer assignedRenderer = TakeNearestRenderer(rendererOrigin, availableBulbs);
            generators[i].ConfigureControlledLight(assignedLight, assignedRenderer);
        }

        generators[RequiredGeneratorCount - 1].ConfigureControlledLight(null, null);
    }

    private static Light TakeNearestLight(Vector3 origin, List<Light> candidates)
    {
        int closestIndex = -1;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < candidates.Count; i++)
        {
            float distance = (candidates[i].transform.position - origin).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        if (closestIndex < 0)
        {
            return null;
        }

        Light closest = candidates[closestIndex];
        candidates.RemoveAt(closestIndex);
        return closest;
    }

    private static Renderer TakeNearestRenderer(Vector3 origin, List<Renderer> candidates)
    {
        int closestIndex = -1;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < candidates.Count; i++)
        {
            float distance = (candidates[i].bounds.center - origin).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        if (closestIndex < 0)
        {
            return null;
        }

        Renderer closest = candidates[closestIndex];
        candidates.RemoveAt(closestIndex);
        return closest;
    }

    private void OnDestroy()
    {
        if (generators == null)
        {
            return;
        }

        for (int i = 0; i < generators.Length; i++)
        {
            if (generators[i] != null)
            {
                generators[i].Disabled -= OnGeneratorDisabled;
            }
        }
    }

    private void OnGUI()
    {
        if (!initialized)
        {
            return;
        }

        if (questStyle == null)
        {
            questStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            questStyle.normal.textColor = new Color(1f, 0.88f, 0.35f);
        }

        Rect questRect = new Rect(Screen.width - 285f, 15f, 270f, 42f);
        GUI.Box(questRect, GUIContent.none);
        string questText = questComplete
            ? "QUEST COMPLETE"
            : $"QUEST: {remainingGenerators}/{RequiredGeneratorCount} GENS";
        GUI.Label(questRect, questText, questStyle);
    }

    private void OnGeneratorDisabled(GeneratorInteraction generator)
    {
        RefreshGate();
    }

    private void RefreshGate()
    {
        int disabledCount = 0;
        for (int i = 0; i < RequiredGeneratorCount; i++)
        {
            if (generators[i].IsDisabled)
            {
                disabledCount++;
            }
        }

        remainingGenerators = RequiredGeneratorCount - disabledCount;

        if (remainingGenerators > 0)
        {
            return;
        }

        questComplete = true;
        if (blockerRenderer != null)
        {
            blockerRenderer.enabled = false;
        }
        if (blockerCollider != null && (exitDoor == null || blockerCollider.gameObject != exitDoor.gameObject))
        {
            blockerCollider.enabled = false;
        }
        if (exitDoor != null)
        {
            exitDoor.enabled = true;
        }
    }

    private static Renderer FindRedBlocker(Transform ending)
    {
        Renderer[] endingRenderers = ending.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < endingRenderers.Length; i++)
        {
            if (IsRed(endingRenderers[i]))
            {
                return endingRenderers[i];
            }
        }

        Renderer closest = null;
        float closestDistance = float.PositiveInfinity;
        Renderer[] sceneRenderers = FindObjectsOfType<Renderer>(true);
        for (int i = 0; i < sceneRenderers.Length; i++)
        {
            Renderer candidate = sceneRenderers[i];
            if (candidate.gameObject.scene != ending.gameObject.scene || !IsRed(candidate))
            {
                continue;
            }

            float distance = (candidate.bounds.center - ending.position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = candidate;
            }
        }

        return closest;
    }

    private static bool IsRed(Renderer renderer)
    {
        if (renderer == null || renderer.sharedMaterial == null)
        {
            return false;
        }

        Color color = renderer.sharedMaterial.color;
        return color.r > 0.45f && color.r > color.g * 1.35f && color.r > color.b * 1.35f;
    }
}
