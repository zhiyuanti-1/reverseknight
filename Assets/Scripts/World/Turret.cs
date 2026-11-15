using UnityEngine;

// 横向炮台：按固定间隔发射子弹。子弹是 Hazard 触发器，碰到玩家即死。
public class Turret : MonoBehaviour
{
    public int direction = 1;
    public float interval = 2f;
    public float speed = 13f;
    [Range(0f, 1f)] public float phase;

    float timer;

    void Start()
    {
        timer = Mathf.Clamp01(phase) * interval;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < interval) return;
        timer -= interval;
        Fire();
    }

    void Fire()
    {
        var go = new GameObject("Bullet");
        go.transform.SetParent(transform.parent, false);
        go.transform.position = transform.position + new Vector3(direction * 0.6f, 0f, 0f);
        go.transform.localScale = Vector3.one * 0.35f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Circle;
        sr.color = Palette.Hazard;
        sr.sortingOrder = 3;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = Vector2.one * 0.9f;
        go.AddComponent<Hazard>();

        var bullet = go.AddComponent<Bullet>();
        bullet.direction = direction;
        bullet.speed = speed;
    }
}
