using UnityEngine;

public static class PlayerFactory
{
    public static GameObject Create(Vector2 spawn, Transform parent)
    {
        var root = new GameObject("Player");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(spawn.x, spawn.y, 0f);

        var rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        var box = root.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.7f, 1.4f);

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        KnightSprites.Ensure();

        SpriteRenderer body;
        bool useSprites = KnightSprites.Ready;
        if (useSprites)
        {
            // 贴图锚点在碰撞盒中心，脚底正好落在碰撞盒底部
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = KnightSprites.Idle;
            sr.sortingOrder = 6;
            body = sr;
        }
        else
        {
            var go = Part(visual.transform, "Body", SpriteFactory.Rounded, Palette.Player, new Vector2(0.7f, 1.4f), Vector2.zero, 6);
            Part(visual.transform, "Helmet", SpriteFactory.Square, Palette.PlayerHelmet, new Vector2(0.46f, 0.16f), new Vector2(0f, 0.56f), 7);
            Part(visual.transform, "Eye", SpriteFactory.Square, Palette.PlayerHelmet, new Vector2(0.1f, 0.1f), new Vector2(0.2f, 0.3f), 8);
            Part(visual.transform, "Cape", SpriteFactory.Square, Palette.Cape, new Vector2(0.5f, 0.9f), new Vector2(-0.24f, -0.05f), 5);
            body = go.GetComponent<SpriteRenderer>();
        }

        root.AddComponent<PlayerInput>();
        root.AddComponent<PlayerMotor>();

        var pv = root.AddComponent<PlayerVisual>();
        pv.visualRoot = visual.transform;
        pv.body = body;
        pv.useSprites = useSprites;

        root.AddComponent<PlayerAgent>();

        return root;
    }

    static GameObject Part(Transform parent, string name, Sprite sprite, Color color, Vector2 size, Vector2 offset, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;

        return go;
    }
}
