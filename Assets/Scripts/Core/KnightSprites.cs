using UnityEngine;

// 角色精灵缓存：从 Resources/Art/Characters/Knight 读取，缺图时 Ready=false，
// PlayerFactory 会退回旧的色块占位表现。
public static class KnightSprites
{
    public const string Path = "Art/Characters/Knight/";

    public static bool Ready { get; private set; }
    public static Sprite Idle;
    public static Sprite IdleFront;
    public static Sprite Jump;
    public static Sprite Fall;
    public static Sprite Land;
    public static Sprite[] RunR = new Sprite[6];
    public static Sprite[] RunL = new Sprite[6];
    // 跑动循环：6 张关键帧 + 6 张光流中间帧 + 2/3/4 关键帧后的 3 张 1/4 补帧，共 15 帧
    public static Sprite[] RunCycle;
    public static Sprite[] Flip = new Sprite[5];

    static bool tried;

    public static void Ensure()
    {
        if (tried) return;
        tried = true;

        Idle = Load("knight_idle");
        IdleFront = Load("knight_idle_front");
        Jump = Load("knight_jump");
        Fall = Load("knight_fall");
        Land = Load("knight_land");

        var cycle = new System.Collections.Generic.List<Sprite>();
        for (int i = 0; i < 6; i++)
        {
            RunR[i] = Load("knight_run_R_" + (i + 1));
            RunL[i] = Load("knight_run_L_" + (i + 1));
            cycle.Add(RunR[i]);
            // 2→3、3→4、4→5 这三段再补一张 1/4 处的帧
            if (i == 1 || i == 2 || i == 3)
                cycle.Add(Load("knight_run_R_" + (i + 1) + "q"));
            cycle.Add(Load("knight_run_R_" + (i + 1) + "m"));
        }
        RunCycle = cycle.ToArray();
        for (int i = 0; i < 5; i++) Flip[i] = Load("knight_flip_" + (i + 1));

        Ready = Idle != null && Jump != null && Fall != null
                && RunR[0] != null && RunL[0] != null && Flip[0] != null;
    }

    static Sprite Load(string name)
    {
        var s = Resources.Load<Sprite>(Path + name);
        if (s == null) Debug.LogWarning("[KnightSprites] 缺少精灵: " + Path + name);
        return s;
    }
}
