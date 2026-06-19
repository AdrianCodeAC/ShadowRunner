using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelProgress
{
    private const string HighestUnlockedKey = "HighestUnlockedLevel";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetForNewGameSession()
    {
        PlayerPrefs.SetInt(HighestUnlockedKey, 1);
        PlayerPrefs.Save();
    }

    public static int HighestUnlockedLevel => Mathf.Clamp(PlayerPrefs.GetInt(HighestUnlockedKey, 1), 1, 5);

    public static bool IsUnlocked(int levelNumber)
    {
        return levelNumber >= 1 && levelNumber <= HighestUnlockedLevel;
    }

    public static void UnlockAfterCompleting(Scene scene)
    {
        string sceneName = scene.name.ToLowerInvariant();
        if (!sceneName.StartsWith("level "))
        {
            return;
        }

        string numberText = sceneName.Substring("level ".Length);
        if (int.TryParse(numberText, out int completedLevel))
        {
            UnlockLevel(completedLevel + 1);
        }
    }

    private static void UnlockLevel(int levelNumber)
    {
        int clampedLevel = Mathf.Clamp(levelNumber, 1, 5);
        if (clampedLevel <= HighestUnlockedLevel)
        {
            return;
        }

        PlayerPrefs.SetInt(HighestUnlockedKey, clampedLevel);
        PlayerPrefs.Save();
    }
}
