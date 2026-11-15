using UnityEngine;

// 角色表现层：跑动循环、滞空、反转过渡帧，以及反转时的残影。
// 反转时机体不做新动作旋转，而是保留视觉根节点 0/180 度的翻面，再由 flip 帧补足蜷缩过程。
public class PlayerVisual : MonoBehaviour
{
    [Header("结构")]
    public Transform visualRoot;
    public SpriteRenderer body;
    public bool useSprites;

    [Header("跑动")]
    [Tooltip("用 15 帧（6 关键帧 + 6 中间帧 + 3 张 1/4 补帧）跑动；关闭则退回原来的 6 帧")]
    public bool smoothRunFrames = true;
    [Tooltip("基础循环速度（次/秒）")]
    public float runCycleRate = 2.33f;
    [Tooltip("水平速度每增加 1，循环速度增加多少（次/秒）")]
    public float runSpeedCycleRate = 0.183f;

    [Header("反转")]
    public float flipFrames = 5f;

    [Header("残影")]
    public int ghostCount = 4;
    public float ghostLife = 0.13f;
    public float ghostInterval = 0.013f;

    PlayerMotor motor;

    float roll;
    float squash;
    float facing = 1f;
    float runTime;
    float landTimer;
    int lastFlipFrame = -1;
    float flipVisualTimer;
    float flipVisualDuration = 0.08f;

    SpriteRenderer[] ghosts;
    float[] ghostAge;
    float ghostTimer;

    // 供调试探针读取当前翻面状态
    public float Roll { get { return roll; } }
    public bool FlipVisual { get { return flipVisualTimer > 0f; } }

    void Start()
    {
        motor = GetComponentInParent<PlayerMotor>();
        if (motor != null)
        {
            motor.OnLand += OnLanded;
            motor.OnFlip += OnFlipped;
        }

        if (useSprites) BuildGhosts();
    }

    void OnDestroy()
    {
        if (motor != null)
        {
            motor.OnLand -= OnLanded;
            motor.OnFlip -= OnFlipped;
        }
    }

    void OnLanded()
    {
        squash = 1f;
        landTimer = 0.09f;
    }

    void OnFlipped()
    {
        ghostTimer = 0f;
        lastFlipFrame = -1;
        // 翻面表现独立计时：贴地吸附、空中翻转、连续翻转都播完整的 5 帧
        flipVisualDuration = Mathf.Max(0.01f, motor != null ? motor.flipDuration : 0.08f);
        flipVisualTimer = flipVisualDuration;
    }

    void Update()
    {
        if (motor == null) return;

        float dt = Time.deltaTime;

        UpdateRoll(dt);
        UpdateFacing(dt);
        UpdateSquash(dt);

        bool visualFlip = flipVisualTimer > 0f;
        float flipProgress = visualFlip
            ? Mathf.Clamp01(1f - flipVisualTimer / flipVisualDuration)
            : 0f;

        if (visualRoot != null)
        {
            // 物理在固定步里一次跳一格，直接跟 transform 会看到台阶；这里按固定步插值出连续位置
            visualRoot.localPosition = motor.VisualPosition - transform.position;
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, roll);
            // Turning upside down also reverses local X. Compensate after the
            // edge-on midpoint so facing continues to match world movement.
            float horizontal = (facing >= 0f ? 1f : -1f) * (roll > 90f ? -1f : 1f);
            visualRoot.localScale = new Vector3(sx * horizontal, sy, 1f);
        }

        if (useSprites && body != null)
        {
            UpdateSprite(dt, visualFlip, flipProgress);
            UpdateGhosts(dt);
        }

        if (visualFlip) flipVisualTimer = Mathf.Max(0f, flipVisualTimer - dt);
    }

    float sx = 1f;
    float sy = 1f;

    void UpdateRoll(float dt)
    {
        float targetRoll = motor.GravitySign > 0 ? 0f : 180f;
        float speed = 180f / Mathf.Max(0.01f, motor.flipDuration);
        roll = Mathf.MoveTowards(roll, targetRoll, speed * dt);
    }

    void UpdateFacing(float dt)
    {
        if (Mathf.Abs(motor.Velocity.x) > 0.5f)
            facing = Mathf.MoveTowards(facing, Mathf.Sign(motor.Velocity.x), 14f * dt);
    }

    void UpdateSquash(float dt)
    {
        squash = Mathf.MoveTowards(squash, 0f, 8f * dt);
        landTimer -= dt;
        sy = 1f - 0.18f * Mathf.Sin(squash * Mathf.PI);
        sx = 1f + 0.12f * Mathf.Sin(squash * Mathf.PI);
    }

    void UpdateSprite(float dt, bool visualFlip, float flipProgress)
    {
        if (visualFlip)
        {
            int f = Mathf.Clamp(Mathf.FloorToInt(flipProgress * flipFrames), 0, KnightSprites.Flip.Length - 1);
            var spr = KnightSprites.Flip[f];
            if (spr != null) body.sprite = spr;

            ghostTimer -= dt;
            if (ghostTimer <= 0f)
            {
                ghostTimer = ghostInterval;
                SpawnGhost(spr);
            }
            return;
        }

        if (!motor.Grounded)
        {
            bool rising = motor.Velocity.y * motor.GravitySign > 0.5f;
            var air = rising ? KnightSprites.Jump : KnightSprites.Fall;
            if (air != null) body.sprite = air;
            runTime = 0f;
            return;
        }

        float speed = Mathf.Abs(motor.Velocity.x);
        if (landTimer > 0f && KnightSprites.Land != null)
        {
            body.sprite = KnightSprites.Land;
            return;
        }

        if (speed > 0.6f)
        {
            var cycle = smoothRunFrames ? KnightSprites.RunCycle : KnightSprites.RunR;
            float cyclesPerSecond = runCycleRate + speed * runSpeedCycleRate;
            runTime += dt * cyclesPerSecond * cycle.Length;
            int f = Mathf.FloorToInt(runTime) % cycle.Length;
            // 向左跑由 visualRoot 的 X 轴镜像完成，左右帧贴图内容一致
            var sprite = cycle[f];
            if (sprite == null) sprite = KnightSprites.RunR[0];
            if (sprite != null) body.sprite = sprite;
        }
        else
        {
            runTime = 0f;
            if (KnightSprites.Idle != null) body.sprite = KnightSprites.Idle;
        }
    }

    // ---------- 残影 ----------

    void BuildGhosts()
    {
        ghosts = new SpriteRenderer[ghostCount];
        ghostAge = new float[ghostCount];
        for (int i = 0; i < ghostCount; i++)
        {
            var go = new GameObject("Ghost" + i);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;
            sr.enabled = false;
            ghosts[i] = sr;
            ghostAge[i] = 999f;
        }
    }

    void SpawnGhost(Sprite sprite)
    {
        if (ghosts == null || sprite == null) return;
        int slot = 0;
        float worst = -1f;
        for (int i = 0; i < ghosts.Length; i++)
        {
            if (ghostAge[i] > worst)
            {
                worst = ghostAge[i];
                slot = i;
            }
        }
        var g = ghosts[slot];
        g.sprite = sprite;
        g.enabled = true;
        var t = g.transform;
        t.position = body.transform.position;
        t.rotation = body.transform.rotation;
        t.localScale = body.transform.lossyScale;
        ghostAge[slot] = 0f;
    }

    void UpdateGhosts(float dt)
    {
        if (ghosts == null) return;
        for (int i = 0; i < ghosts.Length; i++)
        {
            if (!ghosts[i].enabled) continue;
            ghostAge[i] += dt;
            float k = 1f - ghostAge[i] / ghostLife;
            if (k <= 0f)
            {
                ghosts[i].enabled = false;
                continue;
            }
            var c = ghosts[i].color;
            c.r = 0.95f; c.g = 0.72f; c.b = 0.58f;
            c.a = 0.22f * k * k;
            ghosts[i].color = c;
        }
    }
}
