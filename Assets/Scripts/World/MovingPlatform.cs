using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Vector2 travel = new Vector2(4f, 0f);
    public float speed = 2f;
    public float waitAtEnds = 0.5f;
    [Range(0f, 1f)] public float startPhase = 0f;
    // 玩家站上去才启动，到顶后等玩家离开才返回；用于封住升降井口。
    public bool playerActivated;

    public Vector2 Step { get; private set; }

    Vector2 origin;
    float t;
    int dir = 1;
    float wait;

    bool PlayerOn()
    {
        var gm = GameManager.I;
        if (gm == null || gm.PlayerMotor == null) return false;
        var col = GetComponent<Collider2D>();
        return col != null && gm.PlayerMotor.GroundCollider == col;
    }

    void Start()
    {
        origin = transform.position;
        t = Mathf.Clamp01(startPhase);
        dir = t >= 0.999f ? -1 : 1;

        Vector2 startPos = origin + travel * t;
        transform.position = new Vector3(startPos.x, startPos.y, 0f);
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector2 before = transform.position;

        float len = travel.magnitude;
        if (len < 0.01f)
        {
            Step = Vector2.zero;
            return;
        }

        bool riding = playerActivated && PlayerOn();

        if (playerActivated)
        {
            // 停在井底时只等玩家站上来；玩家不在平台上时平台就是井盖。
            if (t <= 0f && !riding)
            {
                Step = Vector2.zero;
                return;
            }
            // 到顶后玩家还站着就等着，离开后再返回。
            if (t >= 1f && riding)
            {
                Step = Vector2.zero;
                return;
            }
            // 玩家在下降途中踩上来，就直接反向再上去。
            if (dir < 0 && riding)
            {
                dir = 1;
                wait = 0f;
            }
        }

        if (wait > 0f)
        {
            wait -= dt;
        }
        else
        {
            t += dir * speed * dt / len;
            if (t >= 1f)
            {
                t = 1f;
                dir = -1;
                wait = waitAtEnds;
            }
            else if (t <= 0f)
            {
                t = 0f;
                dir = 1;
                wait = waitAtEnds;
            }
        }

        Vector2 p = origin + travel * t;
        transform.position = new Vector3(p.x, p.y, 0f);
        Step = (Vector2)transform.position - before;
    }
}
