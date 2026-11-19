using UnityEngine;

public class FragileFloor : MonoBehaviour
{
    public float breakDelay = 0.5f;
    public float respawnDelay = 3f;

    SpriteRenderer sr;
    Collider2D col;
    Color baseColor = Palette.Fragile;
    float standTime;
    float downTime;
    bool broken;
    bool crackPlayed;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        if (sr != null) baseColor = sr.color;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (broken)
        {
            downTime -= dt;
            if (downTime <= 0f)
            {
                AudioManager.PlayAt("crate_reset", transform.position, 0.6f, 14f);
                ResetFloor();
            }
            return;
        }

        bool standing = false;
        var gm = GameManager.I;
        if (gm != null && gm.PlayerMotor != null && col != null)
        {
            standing = gm.PlayerMotor.GroundCollider == col;
        }

        if (standing)
        {
            standTime += dt;
            if (!crackPlayed)
            {
                crackPlayed = true;
                AudioManager.PlayAt("crate_crack", transform.position, 0.65f, 14f);
            }
            if (sr != null)
            {
                float k = Mathf.Clamp01(standTime / Mathf.Max(0.01f, breakDelay));
                sr.color = Color.Lerp(baseColor, Palette.Hazard, k);
            }
            if (standTime >= breakDelay) Break();
        }
        else
        {
            standTime = Mathf.Max(0f, standTime - dt * 2f);
            crackPlayed = false;
        }
    }

    public void ResetFloor()
    {
        broken = false;
        standTime = 0f;
        downTime = 0f;
        if (col != null) col.enabled = true;
        if (sr != null) sr.color = baseColor;
    }

    void Break()
    {
        broken = true;
        crackPlayed = false;
        AudioManager.PlayAt("crate_break", transform.position, 0.8f, 16f);
        downTime = respawnDelay;
        if (col != null) col.enabled = false;
        if (sr != null) sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.12f);
    }
}
