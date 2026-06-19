using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Health))]
public class ChallengeDeathReset : MonoBehaviour
{
    [SerializeField] private float resetDelaySeconds = 2f;

    private Health health;
    private bool dead;
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;

    private void Awake()
    {
        health = GetComponent<Health>();
        PlayerRespawn respawn = GetComponent<PlayerRespawn>();
        if (respawn != null)
        {
            respawn.enabled = false;
        }
    }

    private void OnEnable()
    {
        health.OnDied += OnPlayerDied;
    }

    private void OnDisable()
    {
        health.OnDied -= OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        if (dead)
        {
            return;
        }

        dead = true;
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.enabled = false;
        }
        StartCoroutine(ResetChallenge());
    }

    private IEnumerator ResetChallenge()
    {
        yield return new WaitForSecondsRealtime(resetDelaySeconds);
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameObject.scene.name);
    }

    private void OnGUI()
    {
        if (!dead)
        {
            return;
        }

        EnsureStyles();
        GUI.depth = -1000;
        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previousColor;
        GUI.Label(new Rect(0f, Screen.height * 0.38f, Screen.width, 80f), "YOU DIED", titleStyle);
        GUI.Label(new Rect(0f, Screen.height * 0.5f, Screen.width, 45f), "RESTARTING CHALLENGE...", subtitleStyle);
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
            fontSize = 52,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = new Color(0.95f, 0.12f, 0.08f);

        subtitleStyle = new GUIStyle(titleStyle) { fontSize = 20 };
        subtitleStyle.normal.textColor = Color.white;
    }
}
