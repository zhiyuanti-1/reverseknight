using UnityEngine;

public static class DungeonArt
{
    const string Folder = "Art/Dungeon/";

    public static readonly Sprite Stone = Load("wall_stone");
    public static readonly Sprite Fragile = Load("floor_fragile");
    public static readonly Sprite Spike = Load("iron_spike", new Vector2(0.5f, 0));
    public static readonly Sprite Flag = Load("checkpoint_flag");
    public static readonly Sprite Exit = Load("exit_portal");
    public static readonly Sprite Spring = Load("spring");
    public static readonly Sprite Platform = Load("moving_platform");
    public static readonly Sprite Lava = Load("lava_flow");
    public static readonly Sprite Gate = Load("gate_iron");
    public static readonly Sprite Background = Load("dungeon_arches");

    public static void Tile(SpriteRenderer renderer, Sprite sprite, Vector2 size)
    {
        renderer.sprite = sprite;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = size;
        renderer.color = Color.white;
    }

    static Sprite Load(string file, Vector2? pivot = null)
    {
        Texture2D texture = Resources.Load<Texture2D>(Folder + file);
        if (texture == null)
        {
            Debug.LogWarning("Missing dungeon texture: " + file);
            return null;
        }

        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            pivot ?? new Vector2(0.5f, 0.5f), 48f, 0, SpriteMeshType.FullRect);
    }
}
