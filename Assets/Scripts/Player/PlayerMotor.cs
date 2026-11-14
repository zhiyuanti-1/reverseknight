using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PlayerMotor : MonoBehaviour
{
    [Header("水平")]
    public float maxSpeed = 8f;
    public float groundAccel = 60f;
    public float groundDecel = 90f;
    public float airAccel = 35f;

    [Header("垂直")]
    public float gravity = 45f;
    public float maxFallSpeed = 24f;

    [Header("反转")]
    public float flipDuration = 0.08f;
    public float flipSearchDistance = 6.5f;

    [Header("碰撞")]
    public LayerMask solidMask = ~0;
    public float skin = 0.02f;

    public Vector2 Velocity;
    public int GravitySign { get; private set; } = 1;
    public bool Grounded { get; private set; }
    public bool InFlip { get { return flipTimer > 0f; } }
    // 反转过渡进度 0..1，供表现层选帧
    public float FlipProgress
    {
        get
        {
            if (flipTimer <= 0f) return 0f;
            return 1f - Mathf.Clamp01(flipTimer / Mathf.Max(0.001f, flipDuration));
        }
    }
    public float UpSign { get { return GravitySign; } }
    public bool Frozen { get; set; }
    public Collider2D GroundCollider { get { return groundCollider; } }

    public System.Action OnFlip;
    public System.Action OnLand;

    BoxCollider2D box;
    PlayerInput input;

    float moveLock;
    float flipTimer;
    Vector2 flipFrom;
    Vector2 flipTo;
    Collider2D groundCollider;

    void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        input = GetComponent<PlayerInput>();
        Physics2D.queriesHitTriggers = false;
        Physics2D.queriesStartInColliders = false;
    }

    void FixedUpdate()
    {
        if (Frozen) return;

        float dt = Time.fixedDeltaTime;

        if (flipTimer > 0f)
        {
            TickFlip(dt);
            return;
        }

        moveLock -= dt;

        bool flipPressed = input != null && input.TakeFlip();
        float move = (input != null && moveLock <= 0f) ? input.Move : 0f;

        if (flipPressed)
        {
            input.ClearBuffers();
            BeginFlip();
            return;
        }

        float target = move * maxSpeed;
        float accel = Grounded
            ? (Mathf.Abs(target) > 0.01f ? groundAccel : groundDecel)
            : airAccel;
        Velocity.x = Mathf.MoveTowards(Velocity.x, target, accel * dt);

        Velocity.y -= gravity * GravitySign * dt;
        AirCurrent.Apply(GetCenter(), dt, ref Velocity);

        if (Velocity.y > maxFallSpeed) Velocity.y = maxFallSpeed;
        if (Velocity.y < -maxFallSpeed) Velocity.y = -maxFallSpeed;

        if (Grounded && groundCollider != null)
        {
            var mp = groundCollider.GetComponent<MovingPlatform>();
            if (mp != null) transform.position += (Vector3)mp.Step;
        }

        Move(Velocity * dt);
    }

    void Move(Vector2 delta)
    {
        Vector2 size = ColliderSize();
        Vector2 pos = transform.position;

        if (Mathf.Abs(delta.x) > 0.0001f)
        {
            Vector2 dir = new Vector2(Mathf.Sign(delta.x), 0f);
            RaycastHit2D hit = Physics2D.BoxCast(pos, size, 0f, dir, Mathf.Abs(delta.x) + skin, solidMask);
            if (hit.collider != null)
            {
                pos.x += dir.x * Mathf.Max(0f, hit.distance - skin);
                Velocity.x = 0f;
            }
            else
            {
                pos.x += delta.x;
            }
        }

        if (Mathf.Abs(delta.y) > 0.0001f)
        {
            Vector2 diry = new Vector2(0f, Mathf.Sign(delta.y));
            RaycastHit2D hit = Physics2D.BoxCast(pos, size, 0f, diry, Mathf.Abs(delta.y) + skin, solidMask);
            if (hit.collider != null)
            {
                pos.y += diry.y * Mathf.Max(0f, hit.distance - skin);
                Velocity.y = 0f;
            }
            else
            {
                pos.y += delta.y;
            }
        }

        transform.position = pos;
        UpdateGrounded();
    }

    void UpdateGrounded()
    {
        Vector2 size = ColliderSize();
        Vector2 pos = transform.position;
        RaycastHit2D hit = Physics2D.BoxCast(pos, size, 0f, new Vector2(0f, -GravitySign), 0.06f, solidMask);

        bool was = Grounded;
        Grounded = hit.collider != null;
        groundCollider = Grounded ? hit.collider : null;

        if (Grounded)
        {
            if (Velocity.y * GravitySign < 0f) Velocity.y = 0f;
            if (!was && OnLand != null) OnLand();
        }
    }

    void BeginFlip()
    {
        bool wasGrounded = Grounded;

        GravitySign = -GravitySign;
        Velocity.y = 0f;
        if (OnFlip != null) OnFlip();

        Grounded = false;
        groundCollider = null;

        // 空中翻转只改变重力方向，不吸附：用于在半空中反复翻转维持滞空
        if (!wasGrounded) return;

        Vector2 dir = new Vector2(0f, -GravitySign);
        Vector2 size = ColliderSize();
        Vector2 pos = transform.position;
        RaycastHit2D hit = Physics2D.BoxCast(pos, size, 0f, dir, flipSearchDistance, solidMask);

        if (hit.collider != null)
        {
            flipFrom = pos;
            flipTo = pos + dir * Mathf.Max(0f, hit.distance - skin);
            flipTimer = Mathf.Max(0.001f, flipDuration);
            Velocity = Vector2.zero;
        }
    }

    void TickFlip(float dt)
    {
        flipTimer -= dt;
        float t = 1f - Mathf.Clamp01(flipTimer / Mathf.Max(0.001f, flipDuration));
        transform.position = Vector2.Lerp(flipFrom, flipTo, t);

        if (flipTimer <= 0f)
        {
            flipTimer = 0f;
            transform.position = flipTo;
            Velocity = Vector2.zero;
            UpdateGrounded();
            if (OnLand != null) OnLand();
        }
    }

    Vector2 ColliderSize()
    {
        Vector2 s = box.size;
        return new Vector2(s.x * 0.96f, s.y * 0.96f);
    }

    public Vector2 GetCenter()
    {
        return box.bounds.center;
    }

    public void Launch(Vector2 dir, float speed)
    {
        flipTimer = 0f;
        Velocity = dir.normalized * speed;
        moveLock = 0.2f;
        Grounded = false;
        groundCollider = null;
    }

    public void Teleport(Vector2 position)
    {
        flipTimer = 0f;
        transform.position = position;
        Velocity = Vector2.zero;
        GravitySign = 1;
        Grounded = false;
        groundCollider = null;
        if (input != null) input.ClearBuffers();
    }
}
