using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.10f;
    public float lookAhead = 0.10f;

    bool useLimits;
    Bounds limits;
    Vector3 velocity;

    Camera cam;
    readonly System.Collections.Generic.List<Vector2> corridors = new System.Collections.Generic.List<Vector2>();
    int corridor;

    public void SetMap(string text)
    {
        corridors.Clear();
        var map = LevelDefinition.Parse(text);
        int start = -1;
        for (int y = 0; y <= map.Height; y++)
        {
            int solid = 0;
            if (y < map.Height)
                for (int x = 0; x < map.Width; x++)
                {
                    char c = map.At(x, map.Height - 1 - y);
                    if (c == '#' || c == 'F' || c == '>' || c == '<') solid++;
                }
            bool open = y < map.Height && solid < map.Width * 0.72f;
            if (open && start < 0) start = y;
            if (open || start < 0) continue;
            if (y - start >= 3) corridors.Add(new Vector2(start, y));
            start = -1;
        }
        corridor = 0;
    }

    float ChannelCenter()
    {
        if (corridors.Count == 0) return limits.center.y;
        float y = target.position.y;
        while (y > corridors[corridor].y + 0.3f && corridor + 1 < corridors.Count) corridor++;
        while (y < corridors[corridor].x - 0.3f && corridor > 0) corridor--;
        Vector2 span = corridors[corridor];
        float halfHeight = (span.y - span.x) * 0.5f + 1.3f;
        cam.orthographicSize = Mathf.Max(halfHeight, 8f / Mathf.Max(0.3f, cam.aspect));
        // 在井口和楼层切换期间也给角色留出完整的上下视野。
        float clearance = Mathf.Max(0f, cam.orthographicSize - 1.5f);
        return Mathf.Clamp((span.x + span.y) * 0.5f, y - clearance, y + clearance);
    }

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    public void SetLimits(Bounds b)
    {
        limits = b;
        useLimits = true;
    }

    public void SnapToTarget()
    {
        if (target == null) return;
        Vector3 p = new Vector3(target.position.x, ChannelCenter(), -10f);
        transform.position = KeepTargetVisible(Clamp(p));
        velocity = Vector3.zero;
    }

    void LateUpdate()
    {
        if (target == null) return;

        float center = ChannelCenter();
        float ahead = cam.orthographicSize * cam.aspect * 0.2f;

        Vector3 desired = new Vector3(target.position.x + ahead, center, -10f);
        Vector3 p = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        transform.position = KeepTargetVisible(Clamp(p));
    }

    Vector3 KeepTargetVisible(Vector3 p)
    {
        // 平滑跟随不能让上升中的角色先跑出画面；地图边缘同样保留此余量。
        float clearance = Mathf.Max(0f, cam.orthographicSize - 1.5f);
        p.y = Mathf.Clamp(p.y, target.position.y - clearance, target.position.y + clearance);
        return p;
    }

    Vector3 Clamp(Vector3 p)
    {
        if (!useLimits || cam == null) return p;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float minX = limits.min.x + halfW;
        float maxX = limits.max.x - halfW;
        if (minX <= maxX) p.x = Mathf.Clamp(p.x, minX, maxX);
        else p.x = limits.center.x;

        float minY = limits.min.y + halfH;
        float maxY = limits.max.y - halfH;
        if (minY <= maxY) p.y = Mathf.Clamp(p.y, minY, maxY);
        else p.y = limits.center.y;

        p.z = -10f;
        return p;
    }
}
