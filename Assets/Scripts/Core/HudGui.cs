using UnityEngine;

public class HudGui : MonoBehaviour
{
    GUIStyle styleLabel;
    GUIStyle styleBold;
    GUIStyle styleBig;
    GUIStyle menuTitle;
    GUIStyle menuLabel;
    GUIStyle menuButton;
    bool chinese;
    int lastHeight = -1;

    void BuildStyles()
    {
        if (lastHeight == Screen.height && styleLabel != null) return;
        lastHeight = Screen.height;

        float k = Screen.height / 720f;
        styleLabel = new GUIStyle(GUI.skin.label);
        styleLabel.fontSize = Mathf.RoundToInt(16f * k);
        styleLabel.normal.textColor = new Color(0.86f, 0.87f, 0.92f, 0.95f);

        styleBold = new GUIStyle(styleLabel);
        styleBold.fontStyle = FontStyle.Bold;
        styleBold.fontSize = Mathf.RoundToInt(20f * k);

        styleBig = new GUIStyle(styleLabel);
        styleBig.fontStyle = FontStyle.Bold;
        styleBig.fontSize = Mathf.RoundToInt(34f * k);
        styleBig.alignment = TextAnchor.MiddleCenter;
        styleBig.normal.textColor = new Color(0.95f, 0.82f, 0.45f, 1f);

        Font font = FontLibrary.Main;
        chinese = font != null;
        menuLabel = new GUIStyle(styleBold) { font = font, alignment = TextAnchor.MiddleCenter };
        menuTitle = new GUIStyle(styleBig) { font = font, fontSize = Mathf.RoundToInt(49f * k) };
        menuButton = new GUIStyle(GUI.skin.button)
        {
            font = font,
            fontSize = Mathf.RoundToInt(23f * k),
            fontStyle = FontStyle.Bold
        };
    }

    void OnGUI()
    {
        var gm = GameManager.I;
        if (gm == null) return;

        BuildStyles();

        float w = Screen.width;
        float h = Screen.height;
        float k = h / 720f;

        if (gm.AtMenu)
        {
            DrawMenu(gm, w, h, k);
            return;
        }

        GUI.Label(new Rect(24f * k, 16f * k, w * 0.6f, 40f * k), gm.CurrentLevelName, styleBold);

        string stats = "DEATHS  " + gm.Deaths.ToString("00") + "        TIME  " + Format(gm.Cleared ? gm.ClearTime : gm.RunTime);
        GUI.Label(new Rect(w - 380f * k, 16f * k, 360f * k, 40f * k), stats, styleBold);

        if (gm.LevelIndex == 0 && !gm.Cleared)
        {
            GUI.Label(new Rect(24f * k, h - 56f * k, w - 40f * k, 30f * k),
                "A / D  MOVE          SPACE  FLIP GRAVITY          R  HOLD TO RESTART          ESC  PAUSE", styleLabel);
        }

        if (gm.Paused)
        {
            GUI.Label(new Rect(0f, h * 0.45f, w, 60f * k), "PAUSED", styleBig);
            DrawActions(gm, w, h * 0.57f, k, false);
        }

        if (gm.Cleared)
        {
            string msg = gm.AllClear ? "ESCAPED" : "LEVEL CLEAR";
            GUI.Label(new Rect(0f, h * 0.42f, w, 60f * k), msg, styleBig);
            GUI.Label(new Rect(0f, h * 0.42f + 52f * k, w, 40f * k),
                "DEATHS " + gm.Deaths + "        TIME " + Format(gm.ClearTime), styleLabel);
            DrawActions(gm, w, h * 0.58f, k, true);
        }

        if (gm.Lava != null && !gm.Cleared && !gm.Paused) DrawChase(gm, w, h, k);
    }

    void DrawMenu(GameManager gm, float w, float h, float k)
    {
        float width = Mathf.Min(w - 32f * k, 520f * k);
        float left = (w - width) * 0.5f;
        Fill(new Rect(0, 0, w, h), new Color(0.068f, 0.076f, 0.123f));
        KnightSprites.Ensure();
        Sprite knight = KnightSprites.IdleFront ?? KnightSprites.Idle;
        if (knight != null)
        {
            Rect spriteRect = knight.rect;
            Texture2D texture = knight.texture;
            float iconHeight = 87f * k;
            float iconWidth = iconHeight * spriteRect.width / spriteRect.height;
            var uv = new Rect(spriteRect.x / texture.width, spriteRect.y / texture.height,
                spriteRect.width / texture.width, spriteRect.height / texture.height);
            GUI.DrawTextureWithTexCoords(new Rect((w - iconWidth) * 0.5f, 29f * k, iconWidth, iconHeight), texture, uv);
        }
        GUI.Label(new Rect(0, 118f * k, w, 68f * k), chinese ? "反转骑士" : "REVERSE KNIGHT", menuTitle);
        GUI.Label(new Rect(0, 193f * k, w, 34f * k), chinese ? "选择关卡" : "SELECT A LEVEL", menuLabel);
        for (int i = 0; i < gm.LevelCount; i++)
        {
            string title = gm.levelNames[i];
            if (chinese)
            {
                switch (i)
                {
                    case 0: title = "01    翻转"; break;
                    case 1: title = "02    断桥"; break;
                    case 2: title = "03    钟楼"; break;
                    case 3: title = "04    熔潮逃生"; break;
                }
            }
            var rect = new Rect(left, (248f + i * 74f) * k, width, 60f * k);
            Color previous = GUI.backgroundColor;
            if (i == 3) GUI.backgroundColor = new Color(1f, 0.62f, 0.36f);
            bool selected = GUI.Button(rect, title, menuButton);
            GUI.backgroundColor = previous;
            if (selected)
            {
                gm.LoadLevel(i);
                break;
            }
        }
        GUI.Label(new Rect(0, h - 94f * k, w, 34f * k),
            chinese ? "点击关卡或按 1–4 进入" : "CLICK A LEVEL OR PRESS 1–4", menuLabel);
        GUI.Label(new Rect(0, h - 54f * k, w, 34f * k),
            chinese ? "A / D 移动    空格 翻转    Esc 暂停" : "A / D  MOVE    SPACE  FLIP    ESC  PAUSE", menuLabel);
    }

    void DrawActions(GameManager gm, float w, float y, float k, bool finished)
    {
        float width = Mathf.Min((w - 30f * k) * 0.5f, 230f * k);
        var left = new Rect(w * 0.5f - width - 5f * k, y, width, 48f * k);
        var right = new Rect(w * 0.5f + 5f * k, y, width, 48f * k);
        string action = finished
            ? (gm.AllClear ? (chinese ? "重玩本关" : "RETRY") : (chinese ? "下一关" : "NEXT LEVEL"))
            : (chinese ? "继续游戏" : "RESUME");
        if (GUI.Button(left, action, menuButton))
        {
            if (finished) gm.LoadLevel(gm.LevelIndex + (gm.AllClear ? 0 : 1));
            else gm.TogglePause();
        }
        if (GUI.Button(right, chinese ? "返回选关" : "LEVEL SELECT", menuButton)) gm.ShowMenu();
    }

    void DrawChase(GameManager gm, float w, float h, float k)
    {
        if (gm.RetryCountdown > 0f)
        {
            GUI.Label(new Rect(0, h * 0.34f, w, 58f * k), chinese ? "准备冲刺" : "GET READY", menuTitle);
            return;
        }
        if (!gm.Lava.Started) return;
        float distance = Mathf.Max(0f, gm.Lava.Distance);
        Color heat = new Color(1f, 0.35f, 0.12f);
        float danger = 1f - Mathf.Clamp01(distance / 12f);
        Fill(new Rect(0, h * 0.14f, (3f + 11f * danger) * k, h * 0.72f),
            new Color(heat.r, heat.g, heat.b, 0.2f + 0.52f * danger));
        float width = Mathf.Min(246f * k, w - 32f * k);
        float x = (w - width) * 0.5f;
        Fill(new Rect(x, h - 58f * k, width, 38f * k), new Color(0.08f, 0.07f, 0.10f, 0.9f));
        Fill(new Rect(x, h - 22f * k, width * danger, 3f * k), heat);
        GUI.Label(new Rect(x, h - 57f * k, width, 34f * k),
            (chinese ? "岩浆距离  " : "LAVA GAP  ") + distance.ToString("0.0"), menuLabel);
    }

    static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    static string Format(float t)
    {
        int total = Mathf.FloorToInt(t);
        int m = total / 60;
        int s = total % 60;
        return m.ToString("00") + ":" + s.ToString("00");
    }
}
