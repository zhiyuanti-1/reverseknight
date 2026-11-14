using UnityEngine;

public static class SpriteFactory
{
    const int PIXELS_PER_UNIT = 32;

    static Sprite square;
    static Sprite rounded;
    static Sprite triangle;
    static Sprite circle;

    public static Sprite Square
    {
        get
        {
            if (square == null) square = Create(Solid(32, 32, Color.white), new Vector2(0.5f, 0.5f));
            return square;
        }
    }

    public static Sprite Rounded
    {
        get
        {
            if (rounded == null) rounded = Create(RoundedTexture(32, 32, 7), new Vector2(0.5f, 0.5f));
            return rounded;
        }
    }

    public static Sprite Triangle
    {
        get
        {
            if (triangle == null) triangle = Create(TriangleTexture(32, 32), new Vector2(0.5f, 0f));
            return triangle;
        }
    }

    public static Sprite Circle
    {
        get
        {
            if (circle == null) circle = Create(CircleTexture(32, 32), new Vector2(0.5f, 0.5f));
            return circle;
        }
    }

    static Sprite Create(Texture2D tex, Vector2 pivot)
    {
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), pivot, PIXELS_PER_UNIT, 0, SpriteMeshType.FullRect);
    }

    static Texture2D Solid(int w, int h, Color c)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = c;
        t.SetPixels(px);
        return t;
    }

    static Texture2D RoundedTexture(int w, int h, int radius)
    {
        var t = solidCanvas(w, h);
        var px = new Color[w * h];
        float r = radius;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, r, w - r);
                float cy = Mathf.Clamp(y + 0.5f, r, h - r);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                px[y * w + x] = d <= r ? Color.white : new Color(1f, 1f, 1f, 0f);
            }
        }
        t.SetPixels(px);
        return t;
    }

    static Texture2D TriangleTexture(int w, int h)
    {
        var t = solidCanvas(w, h);
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float ny = (y + 0.5f) / h;
                float halfWidth = (1f - ny) * 0.5f * w;
                float dx = Mathf.Abs(x + 0.5f - w * 0.5f);
                px[y * w + x] = dx <= halfWidth ? Color.white : new Color(1f, 1f, 1f, 0f);
            }
        }
        t.SetPixels(px);
        return t;
    }

    static Texture2D CircleTexture(int w, int h)
    {
        var t = solidCanvas(w, h);
        var px = new Color[w * h];
        float r = Mathf.Min(w, h) * 0.5f;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(w * 0.5f, h * 0.5f));
                px[y * w + x] = d <= r ? Color.white : new Color(1f, 1f, 1f, 0f);
            }
        }
        t.SetPixels(px);
        return t;
    }

    static Texture2D solidCanvas(int w, int h)
    {
        return new Texture2D(w, h, TextureFormat.RGBA32, false);
    }
}

public static class Palette
{
    public static readonly Color Background = Hex("151725");
    public static readonly Color Solid = Hex("343A52");
    public static readonly Color SolidEdge = Hex("81839B");
    public static readonly Color Hazard = Hex("F07351");
    public static readonly Color Platform = Hex("5A6280");
    public static readonly Color Fragile = Hex("7A6A4A");
    public static readonly Color Spring = Hex("4E8C6A");
    public static readonly Color Current = new Color(0.45f, 0.72f, 0.92f, 0.16f);
    public static readonly Color CurrentUp = new Color(0.42f, 0.80f, 0.86f, 0.20f);
    public static readonly Color CurrentDown = new Color(0.72f, 0.50f, 0.92f, 0.20f);
    public static readonly Color CheckpointOff = Hex("9296B0");
    public static readonly Color CheckpointOn = Hex("77E5AA");
    public static readonly Color Goal = Hex("D8A64A");
    public static readonly Color Player = Hex("E8E2D0");
    public static readonly Color PlayerHelmet = Hex("2A2C3B");
    public static readonly Color Cape = Hex("9E4433");

    public static Color Hex(string hex)
    {
        Color c;
        if (ColorUtility.TryParseHtmlString("#" + hex, out c)) return c;
        return Color.magenta;
    }
}
