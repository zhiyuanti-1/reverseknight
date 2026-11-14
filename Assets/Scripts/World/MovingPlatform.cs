using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Vector2 travel = new Vector2(4f, 0f);
    public float speed = 2f;
    public float waitAtEnds = 0.5f;
    [Range(0f, 1f)] public float startPhase = 0f;

    public Vector2 Step { get; private set; }

    Vector2 origin;
    float t;
    int dir = 1;
    float wait;

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
