using System.Collections.Generic;
using UnityEngine;

public sealed class CoolantWorkshop : MonoBehaviour
{
    sealed class Core
    {
        public GameObject Visual;
        public Vector2 Position;
        public bool Taken;
    }

    sealed class Receiver
    {
        public Vector2 Position;
        public SpriteRenderer Lamp;
        public GameObject Bridge;
        public SpriteRenderer Preview;
        public bool Used;
        public float Forming;
    }

    readonly List<Core> cores = new List<Core>();
    readonly List<Receiver> receivers = new List<Receiver>();
    readonly Color blue = new Color(0.38f, 0.9f, 1f);
    readonly Color amber = new Color(1f, 0.68f, 0.24f);
    GameManager game;
    Transform carried;
    public bool Carrying { get; private set; }
    public int BridgesUsed { get; private set; }
    public int SprayersUsed { get; private set; }

    public void Setup(GameManager manager)
    {
        game = manager;
        foreach (Vector2 point in new[] { new Vector2(8, 4.75f), new Vector2(34, 9.1f),
            new Vector2(76, 4.75f), new Vector2(142, 9.1f), new Vector2(194, 9.1f) })
        {
            var visual = Make("Cooling core", point, new Vector2(0.57f, 0.57f), SpriteFactory.Square, blue, 5);
            visual.transform.rotation = Quaternion.Euler(0, 0, 45);
            Make("Core shine", Vector2.zero, new Vector2(0.2f, 0.2f), SpriteFactory.Square, Color.white, 6, visual.transform);
            cores.Add(new Core { Position = point, Visual = visual.gameObject });
        }
        AddReceiver(14, false, 18, 22);
        AddReceiver(46, false, 50, 58);
        AddReceiver(90, false, 96, 106);
        AddReceiver(90, true, 0, 0);
        AddReceiver(154, false, 158, 166);
        AddReceiver(204, false, 208, 216);
        carried = Make("Carried cooling core", Vector2.zero, new Vector2(0.3f, 0.3f), SpriteFactory.Square, blue, 7).transform;
        carried.rotation = Quaternion.Euler(0, 0, 45);
        carried.gameObject.SetActive(false);
    }

    void AddReceiver(float x, bool spray, int bridgeStart, int bridgeEnd)
    {
        Vector2 pos = new Vector2(x, spray ? 9.15f : 4.85f);
        Color color = spray ? amber : blue;
        var housing = Make(spray ? "Spray receiver" : "Bridge receiver", pos,
            new Vector2(1.1f, 0.72f), DungeonArt.Gate, new Color(0.7f, 0.76f, 0.82f), 3);
        var lamp = Make("Core socket", Vector2.zero, new Vector2(0.35f, 0.35f), SpriteFactory.Square,
            color * 0.5f, 4, housing.transform);
        lamp.transform.rotation = Quaternion.Euler(0, 0, 45);
        var receiver = new Receiver { Position = pos, Lamp = lamp };
        if (spray)
        {
            HintFactory.Build("← 喷淋减速", new Vector2(x - 1.8f, 8f), transform);
            // A visible pipe points back toward the chasing lava.
            Make("Spray pipe", new Vector2(x - 2f, 9.75f), new Vector2(5f, 0.10f), SpriteFactory.Square, color, 3);
        }
        else
        {
            HintFactory.Build("凝固桥 →", new Vector2(x - 1.5f, 5.9f), transform);
            float width = bridgeEnd - bridgeStart;
            Vector2 center = new Vector2((bridgeStart + bridgeEnd) * 0.5f, 3.5f);
            var bridge = Make("Solidified bridge", center, Vector2.one, DungeonArt.Stone, blue, 1);
            DungeonArt.Tile(bridge, DungeonArt.Stone, new Vector2(width, 1f));
            bridge.color = new Color(0.59f, 0.91f, 0.94f);
            bridge.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(width, 1f);
            receiver.Bridge = bridge.gameObject;
            receiver.Bridge.SetActive(false);
            receiver.Preview = Make("Bridge outline", new Vector2(center.x, 3.98f), new Vector2(width, 0.06f),
                SpriteFactory.Square, new Color(0.38f, 0.9f, 1f, 0.4f), 2);
            Make("Receiver bridge link", new Vector2((x + bridgeStart) * 0.5f, 3.85f),
                new Vector2(bridgeStart - x, 0.08f), SpriteFactory.Square, color, 2);
        }
        receivers.Add(receiver);
    }

    void Update()
    {
        foreach (Receiver receiver in receivers)
        {
            if (receiver.Forming <= 0f) continue;
            receiver.Forming -= Time.deltaTime;
            if (receiver.Forming <= 0f) receiver.Bridge.SetActive(true);
        }
        if (game.PlayerMotor == null || game.PlayerMotor.Frozen || game.Cleared || game.Paused) return;
        Vector2 point = game.PlayerMotor.transform.position;
        if (!Carrying)
        {
            foreach (Core core in cores)
            {
                if (core.Taken || Vector2.Distance(point, core.Position) > 0.8f) continue;
                core.Taken = true;
                core.Visual.SetActive(false);
                Carrying = true;
                AudioManager.Play("core_pickup", 0.7f, 0.03f);
                break;
            }
        }
        if (!Carrying || !game.PlayerMotor.Grounded) return;
        foreach (Receiver receiver in receivers)
        {
            if (receiver.Used || Vector2.Distance(point, receiver.Position) > 0.95f) continue;
            Carrying = false;
            receiver.Used = true;
            receiver.Lamp.color = Color.white;
            if (receiver.Bridge == null)
            {
                game.Lava.Slow();
                SprayersUsed++;
                AudioManager.PlayAt("spray", receiver.Position, 0.85f, 24f);
            }
            else
            {
                receiver.Forming = 0.25f;
                receiver.Preview.color = Color.white;
                BridgesUsed++;
                AudioManager.PlayAt("bridge_form", receiver.Position, 0.8f, 24f);
            }
            break;
        }
    }

    void LateUpdate()
    {
        carried.gameObject.SetActive(Carrying);
        if (Carrying && game.PlayerMotor != null)
            carried.position = game.PlayerMotor.transform.position + new Vector3(-0.55f, 0.45f * game.PlayerMotor.GravitySign);
    }

    public void ResetAt(float checkpointX)
    {
        Carrying = false;
        foreach (Core core in cores)
        {
            if (core.Position.x < checkpointX) continue;
            core.Taken = false;
            core.Visual.SetActive(true);
        }
        foreach (Receiver receiver in receivers)
        {
            if (receiver.Position.x < checkpointX) continue;
            receiver.Used = false;
            receiver.Forming = 0f;
            receiver.Lamp.color = (receiver.Bridge == null ? amber : blue) * 0.5f;
            if (receiver.Bridge == null) continue;
            receiver.Bridge.SetActive(false);
            receiver.Preview.color = new Color(0.38f, 0.9f, 1f, 0.4f);
        }
    }

    SpriteRenderer Make(string name, Vector2 pos, Vector2 size, Sprite sprite, Color color, int order, Transform parent = null)
    {
        var item = new GameObject(name);
        item.transform.SetParent(parent != null ? parent : transform, false);
        item.transform.localPosition = pos;
        item.transform.localScale = new Vector3(size.x, size.y, 1);
        var renderer = item.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }
}
