using UnityEngine;

public static class FontLibrary
{
    static Font main;
    static bool tried;

    public static Font Main
    {
        get
        {
            if (!tried)
            {
                tried = true;
                main = Load();
                if (main == null) Debug.LogWarning("FONT: 中文系统字体加载失败，将回退英文");
                else Debug.Log("FONT ok: " + main.name);
            }
            return main;
        }
    }

    static Font Load()
    {
        string[] names =
        {
            "Microsoft YaHei", "Microsoft YaHei UI", "微软雅黑",
            "SimHei", "黑体", "SimSun", "宋体",
            "Noto Sans CJK SC", "Source Han Sans SC", "Arial Unicode MS"
        };
        try
        {
            return Font.CreateDynamicFontFromOSFont(names, 48);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("FONT exception: " + e.Message);
            return null;
        }
    }
}

public class HintText : MonoBehaviour
{
    public float lineHeight = 0.40f;
    public int lineCount = 1;

    bool scaled;
    int frames;

    void LateUpdate()
    {
        if (scaled) return;
        frames++;
        if (frames > 5) { scaled = true; return; }

        var mr = GetComponent<MeshRenderer>();
        if (mr == null) return;

        Bounds b = mr.bounds;
        if (b.size.y < 0.0001f) return;

        float target = lineHeight * Mathf.Max(1, lineCount);
        float k = target / b.size.y;
        transform.localScale = transform.localScale * k;
        scaled = true;
    }
}

public static class HintFactory
{
    public static void Build(string text, Vector2 worldPos, Transform parent)
    {
        var go = new GameObject("Hint");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

        var tm = go.AddComponent<TextMesh>();
        Font f = FontLibrary.Main;
        if (f != null) tm.font = f;
        tm.fontSize = 64;
        tm.characterSize = 0.42f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(0.52f, 0.57f, 0.72f, 0.55f);
        tm.richText = false;
        tm.text = text;

        var mr = go.GetComponent<MeshRenderer>();
        if (f != null) mr.sharedMaterial = f.material;
        mr.sortingOrder = -6;

        var ht = go.AddComponent<HintText>();
        ht.lineCount = text.Split('\n').Length;
    }
}
