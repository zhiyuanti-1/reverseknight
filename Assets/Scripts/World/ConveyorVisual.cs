using UnityEngine;

// 传送带外观：机身由根节点的平铺 SpriteRenderer 画，履带面是循环滚动的四边形网格，
// 两端各有一个滚轮。碰撞和带速逻辑仍在 Conveyor / PlayerMotor 里，这里只做表现。
public class ConveyorVisual : MonoBehaviour
{
    const float PixelsPerUnit = 48f;
    const float BeltPixels = 14f;       // 履带面高度（与 conveyor_belt.png 一致）
    const float RollerPixels = 20f;     // 滚轮贴图尺寸
    const float PatternWorld = 1f;      // 花纹每 1 世界格重复一次
    const float WheelRadius = 9f / PixelsPerUnit;

    static Material sharedBeltMaterial;

    int direction = 1;
    int side = 1;
    float speed = 3f;
    float length = 1f;
    float scroll;
    float rollerAngle;

    Mesh beltMesh;
    Vector2[] beltUv;
    float[] beltX;
    Transform[] rollers;

    // len：整段传送带的长度；dir：+1 向右、-1 向左；faceSide：履带面朝上 (+1) 还是朝下 (-1)
    public void Setup(float len, int dir, int faceSide)
    {
        length = Mathf.Max(0.06f, len);
        direction = dir >= 0 ? 1 : -1;
        side = faceSide >= 0 ? 1 : -1;

        var conveyor = GetComponent<Conveyor>();
        if (conveyor != null) speed = Mathf.Abs(conveyor.speed);

        BuildBelt();
        BuildRollers();
        RefreshUv();
    }

    void BuildBelt()
    {
        float beltHeight = BeltPixels / PixelsPerUnit;
        float half = length * 0.5f;
        float halfH = beltHeight * 0.5f;

        var go = new GameObject("Belt");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, side * (0.5f - halfH), 0f);

        beltMesh = new Mesh { name = "Conveyor belt" };
        beltMesh.MarkDynamic();

        float[] ys = { -halfH, halfH, halfH, -halfH };
        beltX = new[] { -half, -half, half, half };
        var vertices = new Vector3[4];
        for (int i = 0; i < 4; i++) vertices[i] = new Vector3(beltX[i], ys[i], 0f);

        beltMesh.vertices = vertices;
        beltMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        beltUv = new Vector2[4];

        go.AddComponent<MeshFilter>().sharedMesh = beltMesh;

        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = BeltMaterial();
        renderer.sortingOrder = LevelBuilder.ORDER_SOLID + 1;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    void BuildRollers()
    {
        float radius = RollerPixels / PixelsPerUnit * 0.5f;
        float beltHeight = BeltPixels / PixelsPerUnit;
        // 滚轮贴在履带面下沿，留 0.004 格缝隙，避免和履带面同排序时抢层级
        float centerY = side * (0.5f - beltHeight - radius - 0.004f);
        float inset = Mathf.Min(0.26f, length * 0.25f);
        float x = length * 0.5f - inset;

        rollers = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            var go = new GameObject(i == 0 ? "RollerL" : "RollerR");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(i == 0 ? -x : x, centerY, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DungeonArt.Roller;
            sr.sortingOrder = LevelBuilder.ORDER_SOLID + 1;
            rollers[i] = go.transform;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f || beltMesh == null) return;

        scroll += speed * dt;
        RefreshUv();

        // 滚轮和履带面同步：地面带接触点在轮子上方，天花板带接触点在下方，转向相反
        float omega = -side * direction * speed / WheelRadius;
        rollerAngle = Mathf.Repeat(rollerAngle + omega * Mathf.Rad2Deg * dt, 360f);
        for (int i = 0; i < rollers.Length; i++)
            if (rollers[i] != null) rollers[i].localRotation = Quaternion.Euler(0f, 0f, rollerAngle);
    }

    void RefreshUv()
    {
        if (beltMesh == null || beltUv == null) return;

        float phase = scroll / PatternWorld;
        float vBottom = side > 0 ? 0f : 1f;    // 贴图顶部是承托面（亮轨）
        float vTop = 1f - vBottom;
        for (int i = 0; i < 4; i++)
        {
            // 向右：u 随 scroll 减小，花纹向右走；向左同时镜像贴图，箭头朝左
            float u = direction > 0
                ? beltX[i] / PatternWorld - phase
                : phase - beltX[i] / PatternWorld;
            beltUv[i] = new Vector2(u, i == 1 || i == 2 ? vTop : vBottom);
        }
        beltMesh.uv = beltUv;
    }

    static Material BeltMaterial()
    {
        if (sharedBeltMaterial == null)
        {
            sharedBeltMaterial = new Material(Shader.Find("Sprites/Default"));
            if (DungeonArt.Belt != null) sharedBeltMaterial.mainTexture = DungeonArt.Belt.texture;
        }
        return sharedBeltMaterial;
    }

    void OnDestroy()
    {
        if (beltMesh != null) Destroy(beltMesh);
    }
}
