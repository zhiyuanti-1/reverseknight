using System.Collections.Generic;
using UnityEngine;

public class PressureNozzle : MonoBehaviour
{
    public const float WarningTime = 0.9f;
    const float LockedTime = 0.4f;
    const float Range = 20f;
    const float PulseHalfLength = 0.275f;
    public float ZoneMin { get; private set; }
    public float ZoneMax { get; private set; }
    public bool Charging { get; private set; }
    public int Shots { get; private set; }
    public float Charge { get { return Charging ? Mathf.Clamp01(timer / WarningTime) : 0; } }
    float timer;
    bool cooling;
    bool aimLocked;
    Vector2 aim = Vector2.right;
    float pathLength;
    LineRenderer ring;
    LineRenderer sight;
    readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
    static Material ringMaterial;
    AudioSource chargeSrc;
    bool wasLocked;

    public void SetZone(float min, float max)
    {
        ZoneMin = min;
        ZoneMax = max;
    }

    void Awake()
    {
        var sr = gameObject.AddComponent<SpriteRenderer>(); sr.sprite = WaterChannelVisual.Nozzle;
        sr.sortingOrder = LevelBuilder.ORDER_HAZARD;
        var go = new GameObject("Pressure warning ring"); go.transform.SetParent(transform, false);
        ring = go.AddComponent<LineRenderer>(); ring.useWorldSpace = false;
        if (ringMaterial == null) ringMaterial = new Material(Shader.Find("Sprites/Default"));
        ring.sharedMaterial = ringMaterial; ring.widthMultiplier = 0.055f; ring.sortingOrder = 4;
        ring.startColor = ring.endColor = Palette.Hex("FFC46A");
        var sightObject = new GameObject("Aim warning");
        sightObject.transform.SetParent(transform, false);
        sight = sightObject.AddComponent<LineRenderer>();
        sight.useWorldSpace = false;
        sight.sharedMaterial = ringMaterial;
        sight.widthMultiplier = 0.035f;
        sight.sortingOrder = 3;
    }

    public void ResetCycle()
    {
        if (!Charging && !cooling) return;
        timer = 0; cooling = false; Charging = false; aimLocked = false;
        wasLocked = false;
        if (chargeSrc != null && chargeSrc.isPlaying) AudioManager.FadeOut(chargeSrc, 0.25f);
        ring.positionCount = 0;
        sight.positionCount = 0;
        foreach (var pulse in GetComponentsInChildren<PressurePulse>())
        { pulse.gameObject.SetActive(false); Destroy(pulse.gameObject); }
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm == null || gm.PlayerMotor == null || gm.Paused) return;
        float x = gm.PlayerMotor.transform.position.x;
        if (gm.Respawning || gm.Cleared || x < ZoneMin || x >= ZoneMax)
        { ResetCycle(); return; }
        if (cooling)
        {
            timer += Time.deltaTime;
            if (timer >= 2.7f - WarningTime) { timer = 0; cooling = false; }
            return;
        }
        Charging = true;
        if (!aimLocked)
        {
            Vector2 offset = gm.PlayerMotor.GetCenter() - (Vector2)transform.position;
            if (offset.sqrMagnitude > 0.0001f) aim = offset.normalized;
            pathLength = FindPathLength(gm.PlayerMotor);
        }
        timer += Time.deltaTime;
        if (timer >= WarningTime - LockedTime) aimLocked = true;
        if (timer >= WarningTime)
        {
            Fire();
            timer = 0; cooling = true; Charging = false; aimLocked = false;
            ring.positionCount = 0;
            sight.positionCount = 0;
            return;
        }
        Color sightColor = aimLocked ? Palette.Hex("FF9B61") : new Color(1f, 0.76f, 0.42f, 0.32f);
        sight.startColor = sight.endColor = sightColor;
        sight.positionCount = 2;
        sight.SetPosition(0, aim * 0.45f);
        sight.SetPosition(1, aim * pathLength);
        int points = Mathf.Max(2, Mathf.CeilToInt(Charge * 40));
        ring.positionCount = points;
        for (int i = 0; i < points; i++)
        {
            float a = Mathf.PI * 0.5f - i / 39f * Mathf.PI * 2f;
            ring.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 0.58f);
        }

        if (Charging)
        {
            if (chargeSrc == null)
            {
                chargeSrc = AudioManager.Loop("nozzle_charge", transform, 0.45f);
            }
            else if (!chargeSrc.isPlaying && chargeSrc.clip != null)
            {
                chargeSrc.volume = 0f;
                chargeSrc.Play();
                AudioManager.FadeIn(chargeSrc, 0.45f, 0.3f);
            }
        }
        if (!Charging && chargeSrc != null && chargeSrc.isPlaying) AudioManager.FadeOut(chargeSrc, 0.25f);
        if (aimLocked && !wasLocked)
            AudioManager.PlayAt("nozzle_lock", transform.position, 0.85f, 24f);
        wasLocked = aimLocked;
    }

    float FindPathLength(PlayerMotor player)
    {
        float distance = Range;
        if (aim.x > 0.001f)
            distance = Mathf.Min(distance, (ZoneMax - transform.position.x) / aim.x);
        else if (aim.x < -0.001f)
            distance = Mathf.Min(distance, (ZoneMin - transform.position.x) / aim.x);

        var filter = new ContactFilter2D { useTriggers = false };
        Physics2D.Raycast(transform.position, aim, filter, hits, distance);
        foreach (RaycastHit2D hit in hits)
            if (hit.collider.transform != player.transform)
                distance = Mathf.Min(distance, hit.distance);
        return distance;
    }

    void Fire()
    {
        if (pathLength <= PulseHalfLength) return;
        AudioManager.PlayAt("nozzle_fire", transform.position, 0.9f, 28f);
        if (chargeSrc != null && chargeSrc.isPlaying) AudioManager.FadeOut(chargeSrc, 0.2f);
        wasLocked = false;
        var go = new GameObject("High pressure pulse"); go.transform.SetParent(transform, false);
        go.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = SpriteFactory.Rounded;
        sr.color = Palette.Hex("FF8654"); sr.sortingOrder = 4;
        go.transform.localScale = new Vector3(0.55f, 0.45f, 1);
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(0.9f, 0.9f);
        go.AddComponent<Hazard>();
        var pulse = go.AddComponent<PressurePulse>();
        pulse.Setup(aim, pathLength - PulseHalfLength);
        Shots++;
        var core = new GameObject("White pressure core"); core.transform.SetParent(go.transform, false);
        core.transform.localScale = new Vector3(0.7f, 0.48f, 1);
        var inner = core.AddComponent<SpriteRenderer>(); inner.sprite = SpriteFactory.Rounded;
        inner.color = Palette.Hex("FFF4D4"); inner.sortingOrder = 5;
    }
}

public class PressurePulse : MonoBehaviour
{
    Vector2 direction;
    float remaining;
    bool flyPlayed;

    public void Setup(Vector2 aim, float distance)
    {
        direction = aim;
        remaining = distance;
    }

    void Update()
    {
        if (flyPlayed) return;
        var gm = GameManager.I;
        if (gm == null || gm.PlayerMotor == null) return;
        if (Vector2.Distance(transform.position, gm.PlayerMotor.transform.position) < 7f)
        {
            flyPlayed = true;
            AudioManager.PlayAt("pulse_fly", transform.position, 0.5f, 14f);
        }
    }

    void FixedUpdate()
    {
        if (remaining <= 0f) { gameObject.SetActive(false); Destroy(gameObject); return; }
        float step = Mathf.Min(remaining, 18f * Time.fixedDeltaTime);
        transform.position += (Vector3)(direction * step);
        remaining -= step;
    }
}
