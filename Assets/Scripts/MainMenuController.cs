using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallWhenMenuStartsDirectly()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "menu", System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].GetComponentInChildren<MainMenuController>(true) != null)
            {
                return;
            }
        }

        GameObject controller = new GameObject("Main Menu Controller");
        SceneManager.MoveGameObjectToScene(controller, scene);
        controller.AddComponent<MainMenuController>();
    }

    private enum MenuPage
    {
        Main,
        LevelSelect,
        Settings
    }

    private const string VolumeKey = "MasterVolume";
    private MenuPage currentPage;
    private float volume;
    private Texture2D backgroundTexture;
    private GUIStyle titleStyle;
    private GUIStyle headingStyle;
    private GUIStyle buttonStyle;
    private GUIStyle lockedButtonStyle;
    private GUIStyle labelStyle;

    private void Awake()
    {
        volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        AudioListener.volume = volume;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        if (backgroundTexture != null)
        {
            Destroy(backgroundTexture);
        }
    }

    private void OnGUI()
    {
        if (titleStyle == null)
        {
            BuildStyles();
        }

        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), backgroundTexture, ScaleMode.StretchToFill);
        float panelWidth = Mathf.Min(460f, Screen.width - 40f);
        Rect panel = new Rect((Screen.width - panelWidth) * 0.5f, Mathf.Max(28f, Screen.height * 0.08f), panelWidth, Screen.height * 0.84f);
        GUI.Box(panel, GUIContent.none);

        GUI.Label(new Rect(panel.x, panel.y + 28f, panel.width, 72f), "SHADOW RUNNER", titleStyle);
        switch (currentPage)
        {
            case MenuPage.LevelSelect:
                DrawLevelSelect(panel);
                break;
            case MenuPage.Settings:
                DrawSettings(panel);
                break;
            default:
                DrawMainMenu(panel);
                break;
        }
    }

    private void DrawMainMenu(Rect panel)
    {
        float y = panel.y + 150f;
        if (DrawButton(panel, ref y, "SELECT LEVEL", true)) currentPage = MenuPage.LevelSelect;
        if (DrawButton(panel, ref y, "SETTINGS", true)) currentPage = MenuPage.Settings;
        if (DrawButton(panel, ref y, "QUIT", true)) QuitGame();
    }

    private void DrawLevelSelect(Rect panel)
    {
        GUI.Label(new Rect(panel.x, panel.y + 108f, panel.width, 40f), "SELECT LEVEL", headingStyle);
        float y = panel.y + 160f;

        for (int level = 1; level <= 5; level++)
        {
            bool unlocked = LevelProgress.IsUnlocked(level);
            string sceneName = $"level {level}";
            bool available = Application.CanStreamedLevelBeLoaded(sceneName);
            string label = !unlocked ? $"LEVEL {level}  -  LOCKED" : available ? $"LEVEL {level}" : $"LEVEL {level}  -  COMING SOON";

            if (DrawButton(panel, ref y, label, unlocked && available))
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        y += 8f;
        if (DrawButton(panel, ref y, "BACK", true)) currentPage = MenuPage.Main;
    }

    private void DrawSettings(Rect panel)
    {
        GUI.Label(new Rect(panel.x, panel.y + 108f, panel.width, 40f), "SETTINGS", headingStyle);
        float x = panel.x + 55f;
        float y = panel.y + 180f;
        GUI.Label(new Rect(x, y, panel.width - 110f, 32f), $"MASTER VOLUME  {Mathf.RoundToInt(volume * 100f)}%", labelStyle);
        y += 42f;
        volume = GUI.HorizontalSlider(new Rect(x, y, panel.width - 110f, 28f), volume, 0f, 1f);
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(VolumeKey, volume);

        y += 65f;
        bool fullscreen = GUI.Toggle(new Rect(x, y, panel.width - 110f, 32f), Screen.fullScreen, "  FULLSCREEN", labelStyle);
        if (fullscreen != Screen.fullScreen)
        {
            Screen.fullScreen = fullscreen;
        }

        y += 72f;
        float buttonY = y;
        if (DrawButton(panel, ref buttonY, "BACK", true))
        {
            PlayerPrefs.Save();
            currentPage = MenuPage.Main;
        }
    }

    private bool DrawButton(Rect panel, ref float y, string text, bool enabled)
    {
        bool previousEnabled = GUI.enabled;
        GUI.enabled = enabled;
        Rect rect = new Rect(panel.x + 50f, y, panel.width - 100f, 52f);
        bool pressed = GUI.Button(rect, text, enabled ? buttonStyle : lockedButtonStyle);
        GUI.enabled = previousEnabled;
        y += 66f;
        return pressed;
    }

    private void BuildStyles()
    {
        backgroundTexture = new Texture2D(1, 1);
        backgroundTexture.SetPixel(0, 0, new Color(0.025f, 0.045f, 0.05f));
        backgroundTexture.Apply();

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 42,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = new Color(0.96f, 0.76f, 0.2f);

        headingStyle = new GUIStyle(titleStyle) { fontSize = 22 };
        headingStyle.normal.textColor = new Color(0.72f, 0.85f, 0.78f);

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.hover.textColor = new Color(1f, 0.82f, 0.3f);

        lockedButtonStyle = new GUIStyle(buttonStyle);
        lockedButtonStyle.normal.textColor = new Color(0.45f, 0.49f, 0.48f);

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
        labelStyle.normal.textColor = Color.white;
    }

    private static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
