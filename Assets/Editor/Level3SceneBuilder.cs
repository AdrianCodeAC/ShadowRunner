using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Level3SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/level 3.unity";
    private const string StarterRootName = "Level3Starter";

    static Level3SceneBuilder()
    {
        EditorApplication.delayCall += BuildIfNeeded;
    }

    [MenuItem("Shadow Runner/Build Level 3 Starter")]
    public static void BuildIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !System.IO.File.Exists(ScenePath))
        {
            return;
        }

        Scene previousScene = SceneManager.GetActiveScene();
        Scene levelScene = SceneManager.GetSceneByPath(ScenePath);
        bool openedForBuild = !levelScene.IsValid() || !levelScene.isLoaded;

        if (openedForBuild)
        {
            levelScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        if (levelScene.GetRootGameObjects().Any(root => root.name == StarterRootName))
        {
            AddSceneToBuildSettings();
            if (openedForBuild)
            {
                EditorSceneManager.CloseScene(levelScene, true);
            }
            return;
        }

        SceneManager.SetActiveScene(levelScene);
        RemoveDefaultCamera(levelScene);

        GameObject starterRoot = new GameObject(StarterRootName);
        Material floorMaterial = GetOrCreateMaterial("Level3Floor", new Color(0.24f, 0.27f, 0.28f));
        Material wallMaterial = GetOrCreateMaterial("Level3Walls", new Color(0.82f, 0.68f, 0.22f));

        CreateCube("Floor", new Vector3(0f, -0.5f, 0f), new Vector3(30f, 1f, 30f), floorMaterial, starterRoot.transform);
        CreateCube("Wall North", new Vector3(0f, 2f, 15f), new Vector3(30f, 5f, 1f), wallMaterial, starterRoot.transform);
        CreateCube("Wall South", new Vector3(0f, 2f, -15f), new Vector3(30f, 5f, 1f), wallMaterial, starterRoot.transform);
        CreateCube("Wall East", new Vector3(15f, 2f, 0f), new Vector3(1f, 5f, 30f), wallMaterial, starterRoot.transform);
        CreateCube("Wall West", new Vector3(-15f, 2f, 0f), new Vector3(1f, 5f, 30f), wallMaterial, starterRoot.transform);

        CreateSpawnPoint(starterRoot.transform);
        CreatePlayer(starterRoot.transform);

        EditorSceneManager.MarkSceneDirty(levelScene);
        EditorSceneManager.SaveScene(levelScene, ScenePath);
        AddSceneToBuildSettings();

        if (previousScene.IsValid() && previousScene.isLoaded)
        {
            SceneManager.SetActiveScene(previousScene);
        }

        if (openedForBuild)
        {
            EditorSceneManager.CloseScene(levelScene, true);
        }
    }

    private static void RemoveDefaultCamera(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponent<Camera>() != null)
            {
                Object.DestroyImmediate(root);
            }
        }
    }

    private static void CreatePlayer(Transform parent)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        player.transform.SetParent(parent);
        player.transform.SetPositionAndRotation(new Vector3(0f, 1f, -8f), Quaternion.identity);

        Rigidbody body = player.AddComponent<Rigidbody>();
        body.constraints = RigidbodyConstraints.FreezeRotation;

        player.AddComponent<Health>();
        player.AddComponent<PlayerMovement>();
        player.AddComponent<ShadowExposureDamage>();
        player.AddComponent<PlayerRespawn>();
        player.AddComponent<PlayerInteractor>();
        player.AddComponent<ShadowStatusUI>();

        GameObject cameraObject = new GameObject("player camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(player.transform);
        cameraObject.transform.localPosition = new Vector3(0f, 0.65f, 0.18f);
        cameraObject.transform.localRotation = Quaternion.identity;
        cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<MouseLook>();
    }

    private static void CreateSpawnPoint(Transform parent)
    {
        GameObject spawn = new GameObject("PlayerSpawn");
        spawn.transform.SetParent(parent);
        spawn.transform.SetPositionAndRotation(new Vector3(0f, 1f, -8f), Quaternion.identity);
    }

    private static void CreateCube(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent);
        cube.transform.SetPositionAndRotation(position, Quaternion.identity);
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Material GetOrCreateMaterial(string materialName, Color color)
    {
        const string materialFolder = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(materialFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        string path = $"{materialFolder}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        material = new Material(shader) { color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void AddSceneToBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Any(scene => scene.path == ScenePath))
        {
            return;
        }

        EditorBuildSettings.scenes = scenes
            .Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) })
            .ToArray();
    }
}
