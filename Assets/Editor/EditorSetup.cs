using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EditorSetup
{
    public static void SetupScene()
    {
        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(0f, 0f, -10f);
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6.2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.082f, 0.090f, 0.145f, 1f);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        camGO.AddComponent<AudioListener>();
        camGO.AddComponent<CameraFollow>();

        var gmGO = new GameObject("GameManager");
        gmGO.AddComponent<GameManager>();
        gmGO.AddComponent<HudGui>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Prototype.unity");
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Prototype.unity", true)
        };

        EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
        PlayerSettings.companyName = "ReverseKnight";
        PlayerSettings.productName = "ReverseKnight";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;

        AssetDatabase.SaveAssets();
        Debug.Log("SETUP OK");
    }

    public static void BuildWin()
    {
        if (!Directory.Exists("Build")) Directory.CreateDirectory("Build");

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Prototype.unity" },
            locationPathName = "Build/ReverseKnight.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log("BUILD RESULT " + report.summary.result + " errors=" + report.summary.totalErrors);
    }
}
