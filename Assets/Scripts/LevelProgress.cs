using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelProgress
{
    private const string HighestUnlockedKey = "HighestUnlockedLevel";
    private static readonly string[] LevelSceneNames =
    {
        "level 1",
        "level 2",
        "level 3",
        "level 4",
        "level 5"
    };

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

    public static string GetSceneName(int levelNumber)
    {
        return levelNumber >= 1 && levelNumber <= LevelSceneNames.Length
            ? LevelSceneNames[levelNumber - 1]
            : string.Empty;
    }

    public static string GetNextSceneName(string currentSceneName)
    {
        if (!TryGetLevelNumber(currentSceneName, out int currentLevel) || currentLevel >= LevelSceneNames.Length)
        {
            return string.Empty;
        }

        return GetSceneName(currentLevel + 1);
    }

    public static bool TryGetLevelNumber(string sceneName, out int levelNumber)
    {
        levelNumber = 0;
        if (string.IsNullOrEmpty(sceneName))
        {
            return false;
        }

        for (int i = 0; i < LevelSceneNames.Length; i++)
        {
            if (string.Equals(sceneName, LevelSceneNames[i], System.StringComparison.OrdinalIgnoreCase))
            {
                levelNumber = i + 1;
                return true;
            }
        }

        return false;
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
