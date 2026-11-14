using UnityEngine;

public class AirCurrent : MonoBehaviour
{
    public static readonly System.Collections.Generic.List<AirCurrent> All = new System.Collections.Generic.List<AirCurrent>();

    public Vector2 direction = Vector2.up;
    public float strength = 22.5f;

    Bounds bounds;

    void OnEnable()
    {
        All.Add(this);
        Refresh();
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    void Refresh()
    {
        var c = GetComponent<BoxCollider2D>();
        if (c == null) return;
        Bounds b = c.bounds;
        bounds = new Bounds(new Vector3(b.center.x, b.center.y, 0f), new Vector3(b.size.x, b.size.y, 20f));
    }

    void LateUpdate()
    {
        Refresh();
    }

    public static void Apply(Vector2 point, float dt, ref Vector2 velocity)
    {
        for (int i = 0; i < All.Count; i++)
        {
            var z = All[i];
            if (z == null) continue;
            Vector3 p = new Vector3(point.x, point.y, 0f);
            if (z.bounds.Contains(p))
            {
                velocity += z.direction.normalized * z.strength * dt;
            }
        }
    }
}
