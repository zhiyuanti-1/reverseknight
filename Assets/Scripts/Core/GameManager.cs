using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I;

    public string[] levelPaths = { "Levels/level1", "Levels/level2", "Levels/level3", "Levels/level4" };
    public string[] levelNames = { "LEVEL 1   FLIP", "LEVEL 2   BROKEN BRIDGE", "LEVEL 3   BELL TOWER", "LEVEL 4   COOLANT WORKS" };

    public int LevelIndex { get; private set; }
    public int Deaths { get; private set; }
    public float RunTime { get; private set; }
    public float ClearTime { get; private set; }
    public bool Cleared { get; private set; }
    public bool AllClear { get; private set; }
    public bool Paused { get; private set; }
    public bool AtMenu { get; private set; } = true;
    public float RetryCountdown { get; private set; }
    public LavaChase Lava { get; private set; }
    public CoolantWorkshop Workshop { get; private set; }
    public bool Respawning => respawning;
    public PlayerMotor PlayerMotor { get; private set; }
    public int LevelCount { get { return levelPaths.Length; } }

    public string CurrentLevelName
    {
        get { return levelNames[Mathf.Clamp(LevelIndex, 0, levelNames.Length - 1)]; }
    }

    GameObject levelRoot;
    Vector2 checkpoint;
    CameraFollow cam;
    float restartHold;
    bool respawning;
    Bounds levelBounds;

    void Awake()
    {
        I = this;
        Application.targetFrameRate = 60;
        Physics2D.gravity = Vector2.zero;
        Physics2D.queriesHitTriggers = false;
        Physics2D.queriesStartInColliders = false;
        Time.timeScale = 1f;
    }

    void Start()
    {
        if (Camera.main != null) cam = Camera.main.GetComponent<CameraFollow>();
        SetupDebugTools();
    }

    void SetupDebugTools()
    {
        string[] args = System.Environment.GetCommandLineArgs();

        string levelArg = GetArg(args, "-level");
        string path = GetArg(args, "-autoshot");
        bool autoplay = HasArg(args, "-autoplay");
        if (int.TryParse(levelArg, out int n)) LoadLevel(n - 1);
        else if (autoplay || !string.IsNullOrEmpty(path)) LoadLevel(0);
        if (autoplay) gameObject.AddComponent<AutoPilot>();
        if (!string.IsNullOrEmpty(path))
        {
            var capture = gameObject.AddComponent<AutoCapture>();
            capture.directory = path;
        }
    }

    static bool HasArg(string[] args, string key)
    {
        foreach (var a in args) if (a == key) return true;
        return false;
    }

    static string GetArg(string[] args, string key)
    {
        for (int i = 0; i < args.Length - 1; i++) if (args[i] == key) return args[i + 1];
        return null;
    }

    public void LoadLevel(int index)
    {
        UnloadLevel();
        AtMenu = false;
        LevelIndex = Mathf.Clamp(index, 0, levelPaths.Length - 1);
        Deaths = 0;
        RunTime = 0f;
        ClearTime = 0f;
        Cleared = false;
        AllClear = false;
        Paused = false;
        respawning = false;
        restartHold = 0f;
        Time.timeScale = 1f;

        var asset = Resources.Load<TextAsset>(levelPaths[LevelIndex]);
        if (asset == null)
        {
            Debug.LogError("LEVEL ASSET MISSING: " + levelPaths[LevelIndex]);
            ShowMenu();
            return;
        }

        levelRoot = new GameObject("Level_" + (LevelIndex + 1));
        var hintAsset = Resources.Load<TextAsset>(levelPaths[LevelIndex] + "_hints");
        LevelInfo info = LevelBuilder.Build(asset.text, hintAsset != null ? hintAsset.text : "", levelRoot.transform);
        levelBounds = info.Bounds;

        GameObject player = PlayerFactory.Create(info.Spawn, levelRoot.transform);
        PlayerMotor = player.GetComponent<PlayerMotor>();
        Agent = player.GetComponent<PlayerAgent>();
        Agent.SetLimits(info.Bounds);

        checkpoint = info.Spawn;
        if (levelPaths[LevelIndex] == "Levels/level4")
        {
            Lava = levelRoot.AddComponent<LavaChase>();
            Lava.Setup(this, info.Bounds);
            Workshop = levelRoot.AddComponent<CoolantWorkshop>();
            Workshop.Setup(this);
        }

        if (cam != null)
        {
            cam.target = player.transform;
            cam.SetLimits(info.Bounds);
            cam.SetMap(asset.text);
            cam.SnapToTarget();
        }

        Debug.Log("LEVEL LOADED " + (LevelIndex + 1) + " size=" + info.Bounds.size + " spawn=" + info.Spawn);
    }

    void UnloadLevel()
    {
        StopAllCoroutines();
        if (levelRoot != null)
        {
            levelRoot.SetActive(false);
            Destroy(levelRoot);
        }
        levelRoot = null;
        PlayerMotor = null;
        Agent = null;
        Lava = null;
        Workshop = null;
        RetryCountdown = 0f;
        if (cam != null) cam.target = null;
    }

    public void ShowMenu()
    {
        UnloadLevel();
        AtMenu = true;
        Paused = false;
        Cleared = false;
        AllClear = false;
        Time.timeScale = 1f;
    }

    public PlayerAgent Agent { get; private set; }

    public void SetCheckpoint(Vector2 point)
    {
        checkpoint = point;
    }

    public void KillPlayer(PlayerAgent agent)
    {
        if (Cleared || AllClear || respawning || Paused) return;
        if (PlayerMotor == null || agent == null || agent != Agent) return;

        respawning = true;
        Deaths++;
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        PlayerMotor.Frozen = true;
        PlayerMotor.Velocity = Vector2.zero;
        yield return new WaitForSeconds(0.12f);
        if (Lava != null)
        {
            Workshop.ResetAt(checkpoint.x);
            Lava.ResetAt(checkpoint);
            foreach (var floor in levelRoot.GetComponentsInChildren<FragileFloor>())
                floor.ResetFloor();
        }
        PlayerMotor.Teleport(checkpoint);
        if (cam != null) cam.SnapToTarget();
        if (Lava != null && Lava.Started)
        {
            RetryCountdown = 0.75f;
            while (RetryCountdown > 0f)
            {
                yield return null;
                RetryCountdown = Mathf.Max(0f, RetryCountdown - Time.deltaTime);
            }
            Lava.ResumeChase();
        }
        PlayerMotor.GetComponent<PlayerInput>().ClearBuffers();
        PlayerMotor.Frozen = false;
        respawning = false;
    }

    public void CompleteLevel(PlayerAgent agent)
    {
        if (Cleared || AllClear) return;
        if (agent != null && agent != Agent) return;

        Cleared = true;
        ClearTime = RunTime;
        if (Lava != null) Lava.SealExit();
        if (PlayerMotor != null)
        {
            PlayerMotor.Frozen = true;
            PlayerMotor.Velocity = Vector2.zero;
        }
        if (LevelIndex >= levelPaths.Length - 1) AllClear = true;

        Debug.Log("LEVEL CLEAR index=" + LevelIndex + " deaths=" + Deaths + " time=" + ClearTime.ToString("0.00"));
    }

    public void TogglePause()
    {
        if (AtMenu || Cleared) return;
        Paused = !Paused;
        Time.timeScale = Paused ? 0f : 1f;
        if (PlayerMotor != null)
        {
            PlayerMotor.Frozen = Paused || respawning;
            if (Paused) PlayerMotor.Velocity = Vector2.zero;
        }
    }

    void Update()
    {
        if (PlayerInput.Source != null) return;
        if (AtMenu)
        {
            for (int i = 0; i < Mathf.Min(9, LevelCount); i++)
            {
                if (!Input.GetKeyDown(KeyCode.Alpha1 + i) && !Input.GetKeyDown(KeyCode.Keypad1 + i)) continue;
                LoadLevel(i);
                return;
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cleared) ShowMenu();
            else TogglePause();
            return;
        }

        if (Paused) return;

        if (!Cleared) RunTime += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (AllClear)
            {
                ShowMenu();
                return;
            }
            if (Cleared)
            {
                LoadLevel(LevelIndex + 1);
                return;
            }
        }

        if (Input.GetKey(KeyCode.R))
        {
            restartHold += Time.deltaTime;
            if (restartHold >= 0.5f)
            {
                restartHold = 0f;
                LoadLevel(LevelIndex);
            }
        }
        else
        {
            restartHold = 0f;
        }
    }

    public Bounds LevelBounds { get { return levelBounds; } }
}
