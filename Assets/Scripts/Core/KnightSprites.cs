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

        for (int i = 0; i < 6; i++)
        {
            RunR[i] = Load("knight_run_R_" + (i + 1));
            RunL[i] = Load("knight_run_L_" + (i + 1));
        }
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
