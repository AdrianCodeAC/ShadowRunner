using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class LevelExitDoor : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "level 2";

    private void Awake()
    {
        Collider collider = GetComponent<Collider>();
        collider.isTrigger = true;
    }

    public void ConfigureNextScene(string sceneName)
    {
        nextSceneName = sceneName;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<Health>() == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            LevelProgress.UnlockAfterCompleting(gameObject.scene);
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
