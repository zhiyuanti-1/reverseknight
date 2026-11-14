using UnityEngine;

public sealed class LavaChase : MonoBehaviour
{
    const float TriggerX = 12f;
    const float RetryLead = 14f;
    const float GateX = 238f;

    GameManager game;
    Transform body;
    SpriteRenderer flow;
    Transform edge;
    Transform[] cracks;
    Transform gate;
    float front;
    float height;
    float animationTime;
    bool waiting;
    bool sealedExit;

    public bool Started { get; private set; }
    public float Distance => game.PlayerMotor.transform.position.x - front;

    public void Setup(GameManager manager, Bounds bounds)
    {
        game = manager;
        height = bounds.size.y;
        body = Tile("熔潮", new Color(0.85f, 0.16f, 0.045f, 0.93f), 8);
        flow = body.GetComponent<SpriteRenderer>();
        DungeonArt.Tile(flow, DungeonArt.Lava, new Vector2(1f, height));
        edge = Tile("熔潮前沿", new Color(1f, 0.72f, 0.2f), 9);
        cracks = new Transform[14];
        for (int i = 0; i < cracks.Length; i++)
            cracks[i] = Tile("熔岩亮纹", new Color(1f, 0.43f, 0.10f, 0.78f), 9);
        gate = Tile("逃生石门", new Color(0.55f, 0.44f, 0.32f), 7);
        var gateArt = gate.GetComponent<SpriteRenderer>();
        gateArt.sprite = DungeonArt.Gate;
        gateArt.color = Color.white;
        gate.position = new Vector3(GateX, 12.4f);
        gate.localScale = new Vector3(0.7f, 6.2f, 1f);
        var marker = Tile("追逐起点", new Color(0.96f, 0.62f, 0.26f), 2);
        marker.position = new Vector3(TriggerX, 4.04f);
        marker.localScale = new Vector3(0.14f, 0.11f, 1f);
        ResetAt(game.PlayerMotor.transform.position);
    }

    public void ResetAt(Vector2 checkpoint)
    {
        Started = checkpoint.x >= TriggerX;
        waiting = Started;
        front = Started ? checkpoint.x - RetryLead : -5f;
        Layout();
    }

    public void ResumeChase() => waiting = false;

    public void SealExit() => sealedExit = true;

    void FixedUpdate()
    {
        if (waiting || sealedExit) return;
        if (!Started)
        {
            if (game.PlayerMotor.transform.position.x < TriggerX) return;
            Started = true;
        }
        // Fixed speeds let the player earn breathing room by running cleanly.
        float speed = front < 52f ? 6.65f : front < 152f ? 7.05f : 7.25f;
        front += speed * Time.fixedDeltaTime;
        if (Distance <= 0.32f) game.KillPlayer(game.Agent);
    }

    void LateUpdate()
    {
        animationTime += Time.deltaTime;
        if (sealedExit)
        {
            gate.position = Vector3.MoveTowards(gate.position, new Vector3(GateX, 7f), 15f * Time.deltaTime);
            front = Mathf.MoveTowards(front, GateX - 0.38f, 15f * Time.deltaTime);
        }
        Layout();
    }

    void Layout()
    {
        float width = Mathf.Max(0f, front + 6f);
        body.position = new Vector3(-6f + width * 0.5f, height * 0.5f);
        flow.size = new Vector2(width, height);
        float glow = 0.95f + 0.05f * Mathf.Sin(animationTime * 6f);
        flow.color = new Color(1f, glow, glow, 0.96f);
        edge.position = new Vector3(front - 0.13f, height * 0.5f);
        edge.localScale = new Vector3(0.26f, height, 1f);
        for (int i = 0; i < cracks.Length; i++)
        {
            float wave = Mathf.Sin(animationTime * 7f + i * 2.4f);
            cracks[i].position = new Vector3(front - 0.9f - i % 4 * 1.45f, i + 0.5f);
            cracks[i].localScale = new Vector3(1.1f + wave * 0.4f, 0.13f, 1f);
        }
    }

    Transform Tile(string label, Color color, int layer)
    {
        var tile = new GameObject(label);
        tile.transform.SetParent(transform, false);
        var sprite = tile.AddComponent<SpriteRenderer>();
        sprite.sprite = SpriteFactory.Square;
        sprite.color = color;
        sprite.sortingOrder = layer;
        return tile.transform;
    }
}
