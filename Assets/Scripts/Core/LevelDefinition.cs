using UnityEngine;

public class LevelDefinition
{
    public string[] Rows;
    public int Height { get { return Rows.Length; } }
    public int Width { get { return Rows.Length > 0 ? Rows[0].Length : 0; } }

    public char At(int col, int row)
    {
        if (row < 0 || row >= Height || col < 0 || col >= Width) return ' ';
        return Rows[row][col];
    }

    public static LevelDefinition Parse(string text)
    {
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var list = new System.Collections.Generic.List<string>();
        int maxWidth = 0;

        foreach (var raw in lines)
        {
            if (raw.StartsWith("//")) continue;
            list.Add(raw);
            if (raw.Length > maxWidth) maxWidth = raw.Length;
        }

        while (list.Count > 0 && list[list.Count - 1].Trim().Length == 0) list.RemoveAt(list.Count - 1);
        while (list.Count > 0 && list[0].Trim().Length == 0) list.RemoveAt(0);

        for (int i = 0; i < list.Count; i++) list[i] = list[i].PadRight(maxWidth);

        return new LevelDefinition { Rows = list.ToArray() };
    }
}

public class LevelInfo
{
    public Vector2 Spawn;
    public Bounds Bounds;
}
