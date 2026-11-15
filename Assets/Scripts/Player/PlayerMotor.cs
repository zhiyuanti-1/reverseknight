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
    [Range(0f, 1f)]
    [Tooltip("翻转后保留的水平速度比例。1 = 完全保持原速，0 = 清零（旧行为）")]
    public float flipHorizontalRetain = 1f;

    [Header("碰撞")]
    public LayerMask solidMask = ~0;
    public float skin = 0.02f;

    [Header("渲染")]
    [Tooltip("在固定步之间插值渲染位置。物理只在固定步里跳一次，直接跟 transform 会在 60Hz / 144Hz 屏幕上看到台阶式抖动")]
    public bool smoothRendering = true;

    public Vector2 Velocity;
    public int GravitySign { get; private set; } = 1;
    public bool Grounded { get; private set; }
    public bool InFlip { get { return flipTimer > 0f; } }
    // 物理吸附进度 0..1；翻面表现使用 PlayerVisual 自己的计时，贴地/空中都播
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
    float flipCarryX;
    Vector2 flipDir;
    float flipSpeed;
    float flipTraveled;
    float flipMaxTravel;
    Collider2D groundCollider;

    // 固定步插值：stepFrom 是上一个固定步结束时的位置，stepTo 是当前固定步结束时的位置
    Vector3 stepFrom;
    Vector3 stepTo;

    void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        input = GetComponent<PlayerInput>();
        Physics2D.queriesHitTriggers = false;
        Physics2D.queriesStartInColliders = false;
        stepFrom = stepTo = transform.position;
    }

    // 渲染位置：在两次固定步之间插值，表现层用它来消除物理步进和显示刷新率不一致造成的抖动
    public Vector3 VisualPosition
    {
        get
        {
            if (!smoothRendering) return transform.position;
            return Vector3.LerpUnclamped(stepFrom, stepTo, InterpolationAlpha);
        }
    }

    float InterpolationAlpha
    {
        get
        {
            if (Time.timeScale <= 0f) return 1f;
            float dt = Time.fixedDeltaTime;
            if (dt <= 0.00001f) return 1f;
            return Mathf.Clamp01((Time.time - Time.fixedTime) / dt);
        }
    }

    void FixedUpdate()
    {
        bool inFlip = flipTimer > 0f;
        stepFrom = transform.position;
        if (!Frozen) Step(Time.fixedDeltaTime);
        stepTo = transform.position;

        // 翻转吸附一步能跨 1 格以上：插值会让视觉整段落后一步，收尾时再突然追上。
        // 翻转期间直接跟物理步，跑动这种小步长才做插值。
        if (inFlip || (stepTo - stepFrom).sqrMagnitude > 0.75f * 0.75f) stepFrom = stepTo;
    }

    void Step(float dt)
    {
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

        float belt = 0f;
        if (Grounded && groundCollider != null)
        {
            var conveyor = groundCollider.GetComponent<Conveyor>();
            if (conveyor != null) belt = conveyor.BeltSpeed;
        }

        float target = Mathf.Clamp(move * maxSpeed + belt, -maxSpeed, maxSpeed);
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
        MoveHorizontal(delta.x);
        MoveVertical(delta.y);
        UpdateGrounded();
    }

    // 水平、垂直分开做盒体扫掠；翻转过程也走这两条路径，避免高速穿进墙面
    bool MoveHorizontal(float dx)
    {
        if (Mathf.Abs(dx) <= 0.0001f) return false;

        Vector2 size = ColliderSize();
        Vector2 pos = transform.position;
        Vector2 dir = new Vector2(Mathf.Sign(dx), 0f);
        RaycastHit2D hit = Physics2D.BoxCast(pos, size, 0f, dir, Mathf.Abs(dx) + skin, solidMask);
        if (hit.collider != null)
        {
            transform.position = pos + dir * Mathf.Max(0f, hit.distance - skin);
            Velocity.x = 0f;
            return true;
        }

        transform.position = pos + new Vector2(dx, 0f);
        return false;
    }

    bool MoveVertical(float dy)
    {
        if (Mathf.Abs(dy) <= 0.0001f) return false;

        Vector2 size = ColliderSize();
        Vector2 pos = transform.position;
        Vector2 dir = new Vector2(0f, Mathf.Sign(dy));
        RaycastHit2D hit = Physics2D.BoxCast(pos, size, 0f, dir, Mathf.Abs(dy) + skin, solidMask);
        if (hit.collider != null)
        {
            transform.position = pos + dir * Mathf.Max(0f, hit.distance - skin);
            Velocity.y = 0f;
            return true;
        }

        transform.position = pos + new Vector2(0f, dy);
        return false;
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
        flipCarryX = Velocity.x * Mathf.Clamp01(flipHorizontalRetain);

        GravitySign = -GravitySign;
        // 只清垂直速度；水平速度带过整个吸附过程，落地后不用从 0 重新加速
        Velocity = new Vector2(flipCarryX, 0f);
        if (OnFlip != null) OnFlip();

        Grounded = false;
        groundCollider = null;

        // 空中翻转只改变重力方向，不吸附：用于在半空中反复翻转维持滞空
        if (!wasGrounded) return;

        Vector2 dir = new Vector2(0f, -GravitySign);
        Vector2 size = ColliderSize();
        Vector2 pos = transform.position;
        RaycastHit2D hit = Physics2D.BoxCast(pos, size, 0f, dir, flipSearchDistance, solidMask);
        if (hit.collider == null) return;

        float distance = Mathf.Max(0f, hit.distance - skin);
        flipDir = dir;

        // 翻转期间会横向移动：按横向落点采样目标面，让吸附和翻面动画都在 flipDuration 内结束。
        // 没有落点面时退回起点面，避免翻转落到空处。
        float travelX = flipCarryX * Mathf.Max(0.001f, flipDuration);
        float endX = pos.x + travelX;
        float endDistance = SampleFlipSurface(new Vector2(endX, pos.y), dir, size);
        flipMaxTravel = endDistance > 0f ? endDistance : distance;
        flipSpeed = flipMaxTravel / Mathf.Max(0.001f, flipDuration);
        flipTraveled = 0f;
        flipTimer = Mathf.Max(0.001f, flipDuration);
    }

    float SampleFlipSurface(Vector2 origin, Vector2 dir, Vector2 size)
    {
        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, dir, flipSearchDistance, solidMask);
        return hit.collider != null ? Mathf.Max(0f, hit.distance - skin) : 0f;
    }

    void TickFlip(float dt)
    {
        flipTimer -= dt;

        // 吸附过程中继续横向滑行；撞到墙就吃掉这部分动量
        if (Mathf.Abs(flipCarryX) > 0.0001f && MoveHorizontal(flipCarryX * dt))
            flipCarryX = 0f;

        // 垂直方向边走边扫掠：先撞到更低的天花板就停在那，换面变高则继续往上探
        if (flipTraveled < flipMaxTravel)
        {
            float step = Mathf.Min(flipSpeed * dt, flipMaxTravel - flipTraveled);
            Vector2 before = transform.position;
            MoveVertical(flipDir.y * step);
            flipTraveled += Mathf.Abs(transform.position.y - before.y);
        }

        if (flipTimer <= 0f)
        {
            flipTimer = 0f;
            Velocity = new Vector2(flipCarryX, 0f);
            UpdateGrounded();
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
        stepFrom = stepTo = position;
        Velocity = Vector2.zero;
        GravitySign = 1;
        Grounded = false;
        groundCollider = null;
        if (input != null) input.ClearBuffers();
    }
}
