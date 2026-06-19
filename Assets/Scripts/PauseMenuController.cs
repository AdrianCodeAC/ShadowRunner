using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseMenuController : MonoBehaviour
{
    private enum PausePage
    {
        Main,
        Settings
    }

    private const string VolumeKey = "MasterVolume";
    private bool isPaused;
    private PausePage currentPage;
    private float volume;
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle labelStyle;

    private void Awake()
    {
        volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
    }

    private void Update()
    {
        if (PausePressed())
        {
            SetPaused(!isPaused);
        }
    }

    private void OnDisable()
    {
        if (isPaused)
        {
            SetPaused(false);
        }
    }

    private void OnGUI()
    {
        if (!isPaused)
        {
            return;
        }

        EnsureStyles();
        GUI.depth = -300;
        GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none);

        float panelWidth = Mathf.Min(440f, Screen.width - 40f);
        Rect panel = new Rect((Screen.width - panelWidth) * 0.5f, Mathf.Max(35f, Screen.height * 0.12f), panelWidth, Screen.height * 0.74f);
        GUI.Box(panel, GUIContent.none);
        GUI.Label(new Rect(panel.x, panel.y + 25f, panel.width, 60f), currentPage == PausePage.Main ? "PAUSED" : "SETTINGS", titleStyle);

        if (currentPage == PausePage.Settings)
        {
            DrawSettings(panel);
        }
        else
        {
            DrawMainPage(panel);
        }
    }

    private void DrawMainPage(Rect panel)
    {
        float y = panel.y + 125f;
        if (DrawButton(panel, ref y, "RESUME")) SetPaused(false);
        if (DrawButton(panel, ref y, "SETTINGS")) currentPage = PausePage.Settings;
        if (DrawButton(panel, ref y, "MAIN MENU")) ReturnToMainMenu();
    }

    private void DrawSettings(Rect panel)
    {
        float x = panel.x + 55f;
        float y = panel.y + 130f;
        GUI.Label(new Rect(x, y, panel.width - 110f, 32f), $"MASTER VOLUME  {Mathf.RoundToInt(volume * 100f)}%", labelStyle);
        y += 42f;
        volume = GUI.HorizontalSlider(new Rect(x, y, panel.width - 110f, 28f), volume, 0f, 1f);
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(VolumeKey, volume);

        y += 62f;
        bool fullscreen = GUI.Toggle(new Rect(x, y, panel.width - 110f, 32f), Screen.fullScreen, "  FULLSCREEN", labelStyle);
        if (fullscreen != Screen.fullScreen)
        {
            Screen.fullScreen = fullscreen;
        }

        y += 70f;
        float buttonY = y;
        if (DrawButton(panel, ref buttonY, "BACK"))
        {
            PlayerPrefs.Save();
            currentPage = PausePage.Main;
        }
    }

    private bool DrawButton(Rect panel, ref float y, string text)
    {
        bool pressed = GUI.Button(new Rect(panel.x + 50f, y, panel.width - 100f, 52f), text, buttonStyle);
        y += 66f;
        return pressed;
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused;
        currentPage = PausePage.Main;
        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = paused;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
    }

    private void ReturnToMainMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        PlayerPrefs.Save();
        SceneManager.LoadScene("menu");
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
        {
            return;
        }

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 36,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = new Color(0.96f, 0.76f, 0.2f);

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.hover.textColor = new Color(1f, 0.82f, 0.3f);

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
        labelStyle.normal.textColor = Color.white;
    }

    private static bool PausePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.P);
#endif
    }
}
