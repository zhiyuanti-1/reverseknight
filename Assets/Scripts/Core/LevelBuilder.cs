using System.Collections.Generic;
using UnityEngine;

public static class LevelBuilder
{
    public const int ORDER_BG = -2;
    public const int ORDER_SOLID = 0;
    public const int ORDER_DECOR = 1;
    public const int ORDER_HAZARD = 2;
    public const int ORDER_GOAL = 3;

    public static LevelInfo Build(string text, string hintText, Transform parent)
    {
        LevelDefinition def = LevelDefinition.Parse(text);

        var info = new LevelInfo();
        info.Bounds = new Bounds(
            new Vector3(def.Width * 0.5f, def.Height * 0.5f, 0f),
            new Vector3(def.Width, def.Height, 1f));
        info.Spawn = new Vector2(1.5f, 2f);

        BuildBackdrop(def, parent);
        BuildGroundRuns(def, parent);
        BuildProps(def, parent, info);
        BuildCurrents(def, parent);
        BuildHints(hintText, parent);

        return info;
    }

    static void BuildBackdrop(LevelDefinition def, Transform parent)
    {
        var backdrop = new GameObject("Dungeon arches");
        backdrop.transform.SetParent(parent, false);
        backdrop.transform.position = new Vector3(def.Width * 0.5f, def.Height * 0.5f);
        var renderer = backdrop.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = ORDER_BG - 2;
        DungeonArt.Tile(renderer, DungeonArt.Background, new Vector2(def.Width, def.Height));
    }

    static void BuildHints(string hintText, Transform parent)
    {
        if (string.IsNullOrEmpty(hintText)) return;

        string[] lines = hintText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        foreach (var raw in lines)
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//")) continue;

            string[] parts = line.Split(new[] { ' ' }, 3);
            if (parts.Length < 3) continue;

            float x, y;
            if (!float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x)) continue;
            if (!float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y)) continue;

            HintFactory.Build(parts[2].Replace("\\n", "\n"), new Vector2(x, y), parent);
        }
    }

    static float RowBottom(LevelDefinition def, int row)
    {
        return def.Height - 1 - row;
    }

    static void BuildGroundRuns(LevelDefinition def, Transform parent)
    {
        for (int row = 0; row < def.Height; row++)
        {
            int col = 0;
            while (col < def.Width)
            {
                char c = def.At(col, row);
                if (c == '#' || c == 'F' || c == '>' || c == '<')
                {
                    int start = col;
                    while (col < def.Width && def.At(col, row) == c) col++;
                    int len = col - start;

                    float y = RowBottom(def, row);
                    float cx = start + len * 0.5f;
                    float cy = y + 0.5f;

                    if (c == '#')
                    {
                        var solid = Block(parent, "Solid", cx, cy, len, 1f, Palette.Solid, ORDER_SOLID, true);
                        TileBlock(solid, DungeonArt.Stone, len, 1f);
                        bool above = def.At(start, row - 1) == '#' || def.At(start, row - 1) == 'F';
                        bool below = def.At(start, row + 1) == '#' || def.At(start, row + 1) == 'F';
                        if (!above) Block(parent, "EdgeT", cx, y + 1f - 0.035f, len, 0.07f, Palette.SolidEdge, ORDER_DECOR, false);
                        if (!below) Block(parent, "EdgeB", cx, y + 0.035f, len, 0.07f, Palette.SolidEdge, ORDER_DECOR, false);
                    }
                    else if (c == '>' || c == '<')
                    {
                        MakeConveyor(parent, def, start, row, len, cx, cy, c == '>' ? 1 : -1);
                    }
                    else
                    {
                        // 碎裂地板按段拆分，每段独立计时，可以边跑边塌
                        const int chunk = 3;
                        bool above2 = def.At(start, row - 1) == '#' || def.At(start, row - 1) == 'F';
                        bool below2 = def.At(start, row + 1) == '#' || def.At(start, row + 1) == 'F';

                        for (int i = 0; i < len; i += chunk)
                        {
                            int w = Mathf.Min(chunk, len - i);
                            float bx = start + i + w * 0.5f;
                            Color tint = ((i / chunk) % 2 == 0)
                                ? Palette.Fragile
                                : new Color(Palette.Fragile.r * 0.82f, Palette.Fragile.g * 0.82f, Palette.Fragile.b * 0.82f, 1f);

                            var piece = Block(parent, "Fragile", bx, cy, w, 1f, tint, ORDER_SOLID, true);
                            TileBlock(piece, DungeonArt.Fragile, w, 1f);
                            piece.AddComponent<FragileFloor>();

                            if (!above2) Block(parent, "EdgeT", bx, y + 1f - 0.035f, w, 0.07f, Palette.Hazard, ORDER_DECOR, false);
                            if (!below2) Block(parent, "EdgeB", bx, y + 0.035f, w, 0.07f, Palette.Hazard, ORDER_DECOR, false);
                        }
                    }
                }
                else
                {
                    col++;
                }
            }
        }
    }

    static void BuildProps(LevelDefinition def, Transform parent, LevelInfo info)
    {
        int checkpointIndex = 0;

        for (int row = 0; row < def.Height; row++)
        {
            for (int col = 0; col < def.Width; col++)
            {
                char c = def.At(col, row);
                float bottom = RowBottom(def, row);
                float cx = col + 0.5f;

                switch (c)
                {
                    case 'P':
                        info.Spawn = new Vector2(cx, bottom + 0.7f);
                        break;

                    case '^':
                        MakeSpike(parent, cx, bottom, 1f);
                        break;

                    case 'T':
                        MakeSpike(parent, cx, bottom, 1f, 3f);
                        break;

                    case 'v':
                        MakeSpike(parent, cx, bottom + 1f, -1f);
                        break;

                    case 'V':
                        MakeSpike(parent, cx, bottom + 1f, -1f, 3f);
                        break;

                    case 'S':
                        MakeSpring(parent, cx, bottom);
                        break;

                    case 'C':
                        MakeCheckpoint(parent, def, col, row, cx, bottom, checkpointIndex);
                        checkpointIndex++;
                        break;

                    case 'G':
                        MakeGoal(parent, cx, bottom);
                        break;

                    case 'X':
                        MakeSpike(parent, cx, bottom + 2f, -1f, 2f);
                        break;

                    case 'M':
                        MakePlatform(parent, cx, bottom + 0.2f, new Vector2(4f, 0f), 0f);
                        break;

                    case 'm':
                        MakePlatform(parent, cx, bottom + 0.2f, new Vector2(4f, 0f), 1f);
                        break;

                    case 'N':
                        MakePlatform(parent, cx, bottom + 0.2f, new Vector2(0f, 3f));
                        break;

                    case 'L':
                        // 长升降台与楼面齐平；玩家站上去才启动，井口在玩家不在平台上时被平台封住
                        MakePlatform(parent, cx, bottom - 0.2f, new Vector2(0f, 7f), 0f, 2.5f, true);
                        break;

                    case 'O':
                        MakePendulum(parent, cx, bottom + 1f, true, col);
                        break;

                    case 'o':
                        MakePendulum(parent, cx, bottom, false, col);
                        break;

                    case 't':
                        MakeTurret(parent, cx, bottom + 0.8f, 1, col);
                        break;

                    case 'u':
                        MakeTurret(parent, cx, bottom + 0.8f, -1, col);
                        break;
                }
            }
        }
    }

    static bool IsCurrent(char c)
    {
        return c == 'W' || c == 'A' || c == 'B';
    }

    static void CurrentConfig(char c, out Vector2 dir, out float strength, out Color color)
    {
        if (c == 'W') { dir = Vector2.up; strength = 62f; color = Palette.Current; return; }
        if (c == 'A') { dir = Vector2.up; strength = 46f; color = Palette.CurrentUp; return; }
        dir = Vector2.down; strength = 46f; color = Palette.CurrentDown;
    }

    static void BuildCurrents(LevelDefinition def, Transform parent)
    {
        bool[,] used = new bool[def.Height, def.Width];

        for (int row = 0; row < def.Height; row++)
        {
            for (int col = 0; col < def.Width; col++)
            {
                char kind = def.At(col, row);
                if (used[row, col] || !IsCurrent(kind)) continue;

                int w = 0;
                while (col + w < def.Width && def.At(col + w, row) == kind) w++;

                int h = 1;
                bool grow = true;
                while (grow && row + h < def.Height)
                {
                    for (int i = 0; i < w; i++)
                    {
                        if (def.At(col + i, row + h) != kind) { grow = false; break; }
                    }
                    if (grow) h++;
                }

                for (int r = row; r < row + h; r++)
                    for (int i = 0; i < w; i++)
                        used[r, col + i] = true;

                float top = def.Height - row;
                float bottom = top - h;
                float centerX = col + w * 0.5f;
                float centerY = bottom + h * 0.5f;

                Vector2 dir;
                float strength;
                Color color;
                CurrentConfig(kind, out dir, out strength, out color);

                var go = Block(parent, "AirCurrent", centerX, centerY, w, h, color, ORDER_BG, true);
                var bc = go.GetComponent<BoxCollider2D>();
                bc.isTrigger = true;
                var ac = go.AddComponent<AirCurrent>();
                ac.direction = dir;
                ac.strength = strength;

                Block(parent, "Edge", centerX, bottom + 0.04f, w, 0.08f, new Color(color.r, color.g, color.b, 0.55f), ORDER_DECOR, false);
            }
        }
    }

    static void MakeSpike(Transform parent, float cx, float baseY, float dir, float height = 1f)
    {
        var root = new GameObject("Spike");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(cx, baseY, 0f);

        var vis = new GameObject("Vis");
        vis.transform.SetParent(root.transform, false);
        vis.transform.localScale = new Vector3(0.8f, 0.8f * height * dir, 1f);
        var sr = vis.AddComponent<SpriteRenderer>();
        sr.sprite = DungeonArt.Spike;
        sr.sortingOrder = ORDER_HAZARD;

        var trig = root.AddComponent<BoxCollider2D>();
        trig.isTrigger = true;
        trig.size = new Vector2(0.62f, 0.85f * height);
        trig.offset = new Vector2(0f, 0.425f * height * dir);
        root.AddComponent<Hazard>();
    }

    static void MakeSpring(Transform parent, float cx, float baseY)
    {
        var root = new GameObject("Spring");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(cx, baseY, 0f);

        Prop(root.transform, "Spring art", DungeonArt.Spring, 0f, 0.26f, 0.96f, 0.52f, ORDER_DECOR);

        var trig = root.AddComponent<BoxCollider2D>();
        trig.isTrigger = true;
        trig.size = new Vector2(0.9f, 0.55f);
        trig.offset = new Vector2(0f, 0.28f);

        var s = root.AddComponent<Spring>();
        s.direction = Vector2.up;
        s.strength = 18f;
    }

    static void MakeCheckpoint(Transform parent, LevelDefinition def, int col, int row, float cx, float bottom, int index)
    {
        var root = new GameObject("Checkpoint");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(cx, bottom, 0f);

        Block(root.transform, "Pole", 0f, 1f, 0.075f, 2f, Palette.Hex("B19875"), ORDER_DECOR, false, true);
        var flag = Prop(root.transform, "Flag", DungeonArt.Flag, 0.37f, 1.74f, 0.72f, 0.57f, ORDER_DECOR + 1);

        var trig = root.AddComponent<BoxCollider2D>();
        trig.isTrigger = true;
        trig.size = new Vector2(1.2f, 2.4f);
        trig.offset = new Vector2(0f, 1.2f);

        var cp = root.AddComponent<Checkpoint>();
        cp.Setup(index, new Vector2(cx, bottom + 0.75f), flag);
    }

    static void MakeGoal(Transform parent, float cx, float bottom)
    {
        var root = new GameObject("Goal");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(cx, bottom, 0f);

        Prop(root.transform, "Portal", DungeonArt.Exit, 0f, 1.15f, 1.32f, 2.3f, ORDER_GOAL);

        var trig = root.AddComponent<BoxCollider2D>();
        trig.isTrigger = true;
        trig.size = new Vector2(1.1f, 2.3f);
        trig.offset = new Vector2(0f, 1.15f);

        root.AddComponent<Goal>();
    }

    static void MakePlatform(Transform parent, float cx, float cy, Vector2 travel, float phase = 0f, float speed = 2f, bool playerActivated = false)
    {
        var go = Block(parent, "MovingPlatform", cx, cy, 2.4f, 0.4f, Palette.Platform, ORDER_DECOR, true);
        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = DungeonArt.Platform;
        renderer.color = Color.white;
        var mp = go.AddComponent<MovingPlatform>();
        mp.travel = travel;
        mp.speed = speed;
        mp.waitAtEnds = 0.4f;
        mp.startPhase = phase;
        mp.playerActivated = playerActivated;
    }

    static bool IsSolidTile(char c)
    {
        return c == '#' || c == 'F' || c == '>' || c == '<';
    }

    // 传送带：实心块 + 带速，外观由 ConveyorVisual 负责（机身平铺、履带面滚动、两端滚轮）
    static void MakeConveyor(Transform parent, LevelDefinition def, int start, int row, int len, float cx, float cy, int dir)
    {
        var belt = Block(parent, "Conveyor", cx, cy, len, 1f, Color.white, ORDER_SOLID, true);
        belt.transform.localScale = Vector3.one;
        belt.GetComponent<BoxCollider2D>().size = new Vector2(len, 1f);
        DungeonArt.Tile(belt.GetComponent<SpriteRenderer>(), DungeonArt.BeltFrame, new Vector2(len, 1f));

        var conveyor = belt.AddComponent<Conveyor>();
        conveyor.direction = dir;
        conveyor.speed = 3f;

        var visual = belt.AddComponent<ConveyorVisual>();
        visual.Setup(len, dir, BeltFaceSide(def, start, row));
    }

    static int BeltFaceSide(LevelDefinition def, int start, int row)
    {
        bool above = IsSolidTile(def.At(start, row - 1));
        bool below = IsSolidTile(def.At(start, row + 1));
        if (!above) return 1;
        if (!below) return -1;
        return 1;
    }

    static void MakePendulum(Transform parent, float cx, float pivotY, bool hangDown, int col)
    {
        const float length = 4.0f;

        var root = new GameObject("Pendulum");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(cx, pivotY, 0f);

        var pivot = new GameObject("Pivot");
        pivot.transform.SetParent(root.transform, false);
        pivot.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        var psr = pivot.AddComponent<SpriteRenderer>();
        psr.sprite = SpriteFactory.Circle;
        psr.color = Palette.SolidEdge;
        psr.sortingOrder = ORDER_DECOR;

        var arm = new GameObject("Arm");
        arm.transform.SetParent(root.transform, false);
        arm.transform.localPosition = new Vector3(0f, hangDown ? -length * 0.5f : length * 0.5f, 0f);
        arm.transform.localScale = new Vector3(0.16f, length, 1f);
        var asr = arm.AddComponent<SpriteRenderer>();
        asr.sprite = SpriteFactory.Square;
        asr.color = Palette.PendulumArm;
        asr.sortingOrder = ORDER_HAZARD - 1;

        var tip = new GameObject("Blade");
        tip.transform.SetParent(root.transform, false);
        tip.transform.localPosition = new Vector3(0f, hangDown ? -length : length, 0f);
        tip.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
        var tsr = tip.AddComponent<SpriteRenderer>();
        tsr.sprite = SpriteFactory.Circle;
        tsr.color = Palette.Hazard;
        tsr.sortingOrder = ORDER_HAZARD;
        var trig = tip.AddComponent<BoxCollider2D>();
        trig.isTrigger = true;
        trig.size = Vector2.one * 1.2f;
        tip.AddComponent<Hazard>();

        var pend = root.AddComponent<Pendulum>();
        pend.period = 2.6f + (col % 3) * 0.3f;
        pend.amplitude = 60f;
        pend.phase = (col % 5) * 1.1f;
    }

    static void MakeTurret(Transform parent, float cx, float cy, int dir, int col)
    {
        var root = new GameObject("Turret");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(cx, cy, 0f);

        var baseGo = new GameObject("Base");
        baseGo.transform.SetParent(root.transform, false);
        baseGo.transform.localScale = new Vector3(0.72f, 0.72f, 1f);
        var bsr = baseGo.AddComponent<SpriteRenderer>();
        bsr.sprite = SpriteFactory.Square;
        bsr.color = Palette.Platform;
        bsr.sortingOrder = ORDER_DECOR;

        var barrel = new GameObject("Barrel");
        barrel.transform.SetParent(root.transform, false);
        barrel.transform.localPosition = new Vector3(dir * 0.36f, 0f, 0f);
        barrel.transform.localScale = new Vector3(0.7f, 0.2f, 1f);
        var rsr = barrel.AddComponent<SpriteRenderer>();
        rsr.sprite = SpriteFactory.Square;
        rsr.color = Palette.SolidEdge;
        rsr.sortingOrder = ORDER_HAZARD;

        var turret = root.AddComponent<Turret>();
        turret.direction = dir;
        turret.interval = 2.0f + (col % 3) * 0.2f;
        turret.phase = (col % 5) * 0.2f;
        turret.speed = 13f;
    }

    static void TileBlock(GameObject block, Sprite sprite, float w, float h)
    {
        block.transform.localScale = Vector3.one;
        block.GetComponent<BoxCollider2D>().size = new Vector2(w, h);
        DungeonArt.Tile(block.GetComponent<SpriteRenderer>(), sprite, new Vector2(w, h));
    }

    static SpriteRenderer Prop(Transform parent, string name, Sprite sprite, float x, float y, float w, float h, int order)
    {
        var prop = Block(parent, name, x, y, w, h, Color.white, order, false, true);
        var renderer = prop.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        return renderer;
    }

    static GameObject Block(Transform parent, string name, float cx, float cy, float w, float h, Color color, int order, bool collider, bool local = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        if (local) go.transform.localPosition = new Vector3(cx, cy, 0f);
        else go.transform.position = new Vector3(cx, cy, 0f);
        go.transform.localScale = new Vector3(w, h, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Square;
        sr.color = color;
        sr.sortingOrder = order;

        if (collider)
        {
            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = Vector2.one;
        }

        return go;
    }
}
