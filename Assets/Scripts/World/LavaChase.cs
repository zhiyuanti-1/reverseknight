using UnityEngine;

public sealed class LavaChase : MonoBehaviour
{
    const float TriggerX = 24f;
    const float RetryLead = 18f;
    const float GateX = 218f;
    const float NormalSpeed = 5.6f;
    GameManager game;
    BoxCollider2D playerCollider;
    Mesh mesh;
    Material material;
    Transform edge;
    Transform gate;
    float height;
    float front = 6f;
    bool waiting;
    bool sealedExit;
    bool startedSfx;
    public bool Started { get; private set; }
    public float SlowRemaining { get; private set; }
    public float Front => front;
    public float VisibleFront => mesh.bounds.max.x;
    public float Distance => playerCollider.bounds.min.x - front;

    public void Setup(GameManager manager, Bounds bounds)
    {
        game = manager;
        playerCollider = manager.PlayerMotor.GetComponent<BoxCollider2D>();
        height = bounds.size.y;
        var body = new GameObject("Lava clipped to lethal front");
        body.transform.SetParent(transform, false);
        mesh = new Mesh { name = "Lava boundary mesh" };
        body.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = body.AddComponent<MeshRenderer>();
        material = new Material(Shader.Find("Sprites/Default"));
        material.mainTexture = DungeonArt.Lava.texture;
        material.mainTexture.wrapMode = TextureWrapMode.Repeat;
        renderer.sharedMaterial = material;
        renderer.sortingOrder = 8;
        edge = Tile("Lava boundary", SpriteFactory.Square, new Color(1f, 0.72f, 0.2f), 9);
        gate = Tile("Escape gate", DungeonArt.Gate, Color.white, 7);
        gate.position = new Vector3(GateX, 13.2f);
        gate.localScale = new Vector3(0.65f, 6.4f, 1f);
        var marker = Tile("Start line", SpriteFactory.Square, new Color(0.96f, 0.62f, 0.26f), 2);
        marker.position = new Vector3(TriggerX, 4.04f);
        marker.localScale = new Vector3(0.14f, 0.11f, 1f);
        ResetAt(game.PlayerMotor.transform.position);
    }

    public void ResetAt(Vector2 checkpoint)
    {
        Started = checkpoint.x >= TriggerX;
        waiting = Started;
        SlowRemaining = 0f;
        front = Started ? checkpoint.x - RetryLead : -5f;
        if (!Started)
        {
            startedSfx = false;
            AudioManager.StopAmbience();
        }
        DrawFront();
    }

    public void ResumeChase() => waiting = false;
    public void Slow() => SlowRemaining = 4f;
    public void SealExit()
    {
        sealedExit = true;
        AudioManager.StopAmbience();
    }

    void FixedUpdate()
    {
        if (waiting || sealedExit || game.Respawning || game.Paused) return;
        if (!Started)
        {
            if (game.PlayerMotor.transform.position.x < TriggerX) return;
            Started = true;
            front = 6f;
        }
        if (Started && !startedSfx)
        {
            startedSfx = true;
            AudioManager.Play("lava_start", 0.85f);
            AudioManager.PlayAmbience("lava_loop", 0.5f);
        }
        float slowTime = Mathf.Min(SlowRemaining, Time.fixedDeltaTime);
        front += slowTime * 2.4f + (Time.fixedDeltaTime - slowTime) * NormalSpeed;
        SlowRemaining = Mathf.Max(0f, SlowRemaining - Time.fixedDeltaTime);
        DrawFront();
        if (Distance <= 0f) game.KillPlayer(game.Agent, "lava");
    }

    void LateUpdate()
    {
        if (sealedExit)
            gate.position = Vector3.MoveTowards(gate.position, new Vector3(GateX, 7f), 15f * Time.deltaTime);
        DrawFront();
    }

    void DrawFront()
    {
        const float left = -6f;
        mesh.vertices = new[] { new Vector3(left, 0), new Vector3(front, 0),
            new Vector3(left, height), new Vector3(front, height) };
        // World-anchored UVs keep the texture still as the right edge advances.
        mesh.uv = new[] { new Vector2(left / 2f, 0), new Vector2(front / 2f, 0),
            new Vector2(left / 2f, height / 2f), new Vector2(front / 2f, height / 2f) };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateBounds();
        edge.position = new Vector3(front - 0.10f, height * 0.5f);
        edge.localScale = new Vector3(0.20f, height, 1f);
    }

    void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
    }

    Transform Tile(string label, Sprite sprite, Color color, int layer)
    {
        var tile = new GameObject(label);
        tile.transform.SetParent(transform, false);
        var renderer = tile.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = layer;
        return tile.transform;
    }
}
