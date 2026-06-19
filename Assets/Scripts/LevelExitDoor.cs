using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class LevelExitDoor : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "level 2";
    private bool isFinalLevel;

    private void Awake()
    {
        Collider collider = GetComponent<Collider>();
        collider.isTrigger = true;
    }

    public void ConfigureNextScene(string sceneName)
    {
        isFinalLevel = false;
        nextSceneName = sceneName;
    }

    public void ConfigureFinalLevel()
    {
        isFinalLevel = true;
        nextSceneName = string.Empty;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<Health>() == null)
        {
            return;
        }

        string routedScene = LevelProgress.GetNextSceneName(gameObject.scene.name);
        bool routedFinalLevel = LevelProgress.TryGetLevelNumber(gameObject.scene.name, out int levelNumber) &&
            levelNumber == 5;

        if (isFinalLevel || routedFinalLevel)
        {
            LevelProgress.UnlockAfterCompleting(gameObject.scene);
            MainMenuController.ShowVictoryOnNextLoad();
            SceneManager.LoadScene("menu");
            return;
        }

        string destination = !string.IsNullOrEmpty(routedScene) ? routedScene : nextSceneName;
        if (!string.IsNullOrEmpty(destination))
        {
            LevelProgress.UnlockAfterCompleting(gameObject.scene);
            SceneManager.LoadScene(destination);
        }
    }
}
