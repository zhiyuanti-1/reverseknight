using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I;

    public string[] levelPaths = { "Levels/level1", "Levels/level2", "Levels/level3", "Levels/level4", "Levels/level5", "Levels/level6", "Levels/level7", "Levels/level8" };
    public string[] levelNames = { "LEVEL 1   FLIP", "LEVEL 2   BROKEN BRIDGE", "LEVEL 3   BELL TOWER", "LEVEL 4   COOLANT WORKS", "LEVEL 5   ELEVATOR SHAFT", "LEVEL 6   TWIN FRAGILE BRIDGE", "LEVEL 7   PENDULUM HALL", "LEVEL 8   COUNTERFLOW" };

    public int LevelIndex { get; private set; }
    public int Deaths { get; private set; }
    public float RunTime { get; private set; }
    public float ClearTime { get; private set; }
    public bool Cleared { get; private set; }
    public bool AllClear { get; private set; }
    public bool Paused { get; private set; }
    public bool AtMenu { get; private set; } = true;

    // 菜单有两级：标题主页 -> 选关页
    public enum MenuPage { Title, LevelSelect }

    public MenuPage Page { get; private set; } = MenuPage.Title;
    public int TitleIndex { get; private set; }
    public int SelectIndex { get; private set; }
    public const int TitleActionCount = 3;

    public void SetPage(MenuPage page)
    {
        Page = page;
        TitleIndex = 0;
    }

    public void MoveSelect(int delta)
    {
        SelectIndex = (SelectIndex + delta + LevelCount) % LevelCount;
        if (delta != 0) AudioManager.Play("ui_hover", 0.3f, 0.05f);
    }

    public void ShowTitle()
    {
        UnloadLevel();
        AtMenu = true;
        Paused = false;
        Cleared = false;
        AllClear = false;
        Time.timeScale = 1f;
        SetPage(MenuPage.Title);
    }

    // 标题页三个按钮：0 开始游戏 1 选择关卡 2 退出游戏
    public void MoveTitleSelection(int delta)
    {
        TitleIndex = (TitleIndex + delta + TitleActionCount) % TitleActionCount;
        if (delta != 0) AudioManager.Play("ui_hover", 0.3f, 0.05f);
    }

    public void ActivateTitle(int index)
    {
        TitleIndex = Mathf.Clamp(index, 0, TitleActionCount - 1);
        AudioManager.Play("ui_confirm", 0.6f, 0.02f);
        switch (TitleIndex)
        {
            case 0: LoadLevel(0); break;
            case 1: SetPage(MenuPage.LevelSelect); break;
            case 2: QuitGame(); break;
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
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
        gameObject.AddComponent<AudioManager>();
        AudioManager.PlayMusic("bgm_menu", 0.7f);
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

        string probePath = GetArg(args, "-probe");
        if (!string.IsNullOrEmpty(probePath))
        {
            var probe = gameObject.AddComponent<MotionProbe>();
            probe.file = probePath;
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
        string music = LevelIndex == 2 ? "bgm_bell"
            : LevelIndex == 3 ? "bgm_forge"
            : (LevelIndex >= 4 && LevelIndex <= 6) ? "bgm_gears"
            : "bgm_level";
        AudioManager.PlayMusic(music, 0.5f, 1.0f);
        if (LevelIndex == 7) AudioManager.PlayAmbience("water_loop", 0.26f, 1.2f);
        else if (LevelIndex == 1 || LevelIndex == 2) AudioManager.PlayAmbience("air_loop", 0.35f, 1.0f);
        else AudioManager.StopAmbience(0.6f);
        SelectIndex = LevelIndex;
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
        LevelInfo info = LevelBuilder.Build(asset.text, hintAsset != null ? hintAsset.text : "", levelRoot.transform,
            levelPaths[LevelIndex] == "Levels/level8");
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
        AudioManager.StopAmbience(0.6f);
        AudioManager.PlayMusic("bgm_menu", 0.7f, 0.9f);
        AtMenu = true;
        Paused = false;
        Cleared = false;
        AllClear = false;
        Time.timeScale = 1f;
        SetPage(MenuPage.LevelSelect);
    }

    public PlayerAgent Agent { get; private set; }

    public void SetCheckpoint(Vector2 point)
    {
        checkpoint = point;
    }

    public void KillPlayer(PlayerAgent agent, string cause = "spike")
    {
        if (Cleared || AllClear || respawning || Paused) return;
        if (PlayerMotor == null || agent == null || agent != Agent) return;

        string sfx = cause == "lava" ? "death_lava"
            : cause == "water" ? "death_water"
            : cause == "fall" ? "death_fall" : "death_spike";
        AudioManager.Play(sfx, 0.85f, 0.05f);

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
        }
        if (levelRoot != null)
        {
            foreach (var floor in levelRoot.GetComponentsInChildren<FragileFloor>())
                floor.ResetFloor();
            foreach (var bullet in levelRoot.GetComponentsInChildren<Bullet>())
                Destroy(bullet.gameObject);
            foreach (var nozzle in levelRoot.GetComponentsInChildren<PressureNozzle>())
                nozzle.ResetCycle();
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
        AudioManager.Play("respawn", 0.7f, 0.03f);
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

        AudioManager.Play("goal", 0.85f);
        StartCoroutine(ClearAudio(AllClear));
        Debug.Log("LEVEL CLEAR index=" + LevelIndex + " deaths=" + Deaths + " time=" + ClearTime.ToString("0.00"));
    }

    System.Collections.IEnumerator ClearAudio(bool all)
    {
        yield return new WaitForSeconds(0.7f);
        AudioManager.Play(all ? "all_clear" : "level_clear", 0.9f);
    }

    public void TogglePause()
    {
        if (AtMenu || Cleared) return;
        Paused = !Paused;
        Time.timeScale = Paused ? 0f : 1f;
        AudioManager.Play(Paused ? "pause_open" : "pause_close", 0.7f);
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
            if (Page == MenuPage.Title)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) MoveTitleSelection(-1);
                if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) MoveTitleSelection(1);
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                    ActivateTitle(TitleIndex);
                else if (Input.GetKeyDown(KeyCode.Escape))
                    QuitGame();
                return;
            }

            // 选关页：方向键选、Enter 进入、1-4 直接跳关、Esc 回主菜单
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) MoveSelect(-1);
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) MoveSelect(1);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                LoadLevel(SelectIndex);
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetPage(MenuPage.Title);
                return;
            }
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
