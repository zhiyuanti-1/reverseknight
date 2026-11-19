using UnityEngine;

// Appearance only: the flat stone collider and Conveyor still own movement.
public class WaterChannelVisual : MonoBehaviour
{
    public static Sprite Nozzle { get { return LoadSprite("sluice_nozzle", 1.15f); } }
    static Sprite nozzle;
    static Sprite arrow;
    static Material waterMaterial;
    Mesh mesh;
    Vector2[] uv = new Vector2[4];
    float length;
    int direction, side;
    float phase;
    float scrollSpeed;

    static Sprite LoadSprite(string name, float size)
    {
        if (nozzle != null) return nozzle;
        var tex = Resources.Load<Texture2D>("Art/Water/" + name);
        if (tex == null) return SpriteFactory.Circle;
        tex.filterMode = FilterMode.Point;
        nozzle = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, tex.width / size);
        return nozzle;
    }

    static Sprite Arrow()
    {
        if (arrow != null) return arrow;
        var tex = new Texture2D(24, 12, TextureFormat.RGBA32, false);
        var pixels = new Color[24 * 12];
        for (int y = 0; y < 12; y++)
            for (int x = 0; x < 24; x++)
            {
                bool shaft = x >= 2 && x <= 14 && y >= 5 && y <= 6;
                bool head = x >= 12 && x <= 20 && Mathf.Abs(y - 5.5f) <= (20 - x) * 0.65f;
                pixels[y * 24 + x] = shaft || head ? Color.white : Color.clear;
            }
        tex.SetPixels(pixels); tex.filterMode = FilterMode.Point; tex.Apply();
        arrow = Sprite.Create(tex, new Rect(0, 0, 24, 12), Vector2.one * 0.5f, 32);
        return arrow;
    }

    public void Setup(float len, int dir, int face)
    {
        length = len; direction = dir; side = face;
        scrollSpeed = GetComponent<Conveyor>().BeltSpeed * 0.5f;
        // Preserve the underlying stone, recess water inside its collision surface.
        DungeonArt.Tile(GetComponent<SpriteRenderer>(), DungeonArt.Stone, new Vector2(len, 1));
        var go = new GameObject("Flowing water and foam");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0, side * 0.29f, 0);
        mesh = new Mesh { name = "Water channel" };
        mesh.vertices = new[] { new Vector3(-len/2,-0.2f), new Vector3(-len/2,0.2f), new Vector3(len/2,0.2f), new Vector3(len/2,-0.2f) };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        var tint = side > 0 ? Color.white : new Color(0.66f, 1f, 0.85f);
        mesh.colors = new[] { tint, tint, tint, tint };
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        if (waterMaterial == null)
        {
            waterMaterial = new Material(Shader.Find("Sprites/Default"));
            var texture = Resources.Load<Texture2D>("Art/Water/water_flow");
            if (texture != null) { texture.wrapMode = TextureWrapMode.Repeat; texture.filterMode = FilterMode.Point; }
            waterMaterial.mainTexture = texture;
        }
        renderer.sharedMaterial = waterMaterial;
        renderer.sortingOrder = LevelBuilder.ORDER_DECOR;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        for (float x = -len / 2 + 1f; x < len / 2 - 0.3f; x += 3f)
        {
            var mark = new GameObject("Flow direction"); mark.transform.SetParent(transform, false);
            mark.transform.localPosition = new Vector3(x, side * 0.28f, 0);
            var sr = mark.AddComponent<SpriteRenderer>(); sr.sprite = Arrow();
            sr.flipX = dir < 0; sr.color = new Color(0.85f, 1f, 1f, 0.9f); sr.sortingOrder = 2;
        }
        Refresh();
    }

    void Update() { phase = Mathf.Repeat(phase + Time.deltaTime * scrollSpeed, 1f); Refresh(); }
    void Refresh()
    {
        // The source has a small border: sample only the water/foam band.
        float bottom = side > 0 ? 0.12f : 0.88f;
        uv[0] = new Vector2(-phase, bottom); uv[1] = new Vector2(-phase, 1-bottom);
        uv[2] = new Vector2(length/2-phase, 1-bottom); uv[3] = new Vector2(length/2-phase, bottom);
        if (mesh != null) mesh.uv = uv;
    }
    void OnDestroy() { if (mesh != null) Destroy(mesh); }
}
