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
        Vector3 p = new Vector3(target.position.x, target.position.y, -10f);
        transform.position = Clamp(p);
        velocity = Vector3.zero;
    }

    void LateUpdate()
    {
        if (target == null) return;

        var motor = target.GetComponent<PlayerMotor>();
        float ahead = motor != null ? motor.Velocity.x * lookAhead : 0f;

        Vector3 desired = new Vector3(target.position.x + ahead, target.position.y + 1.6f, -10f);
        Vector3 p = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        transform.position = Clamp(p);
    }

    Vector3 Clamp(Vector3 p)
    {
        if (!useLimits || cam == null) return p;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        float margin = 2.5f;

        float minX = limits.min.x + halfW;
        float maxX = limits.max.x - halfW;
        if (minX <= maxX) p.x = Mathf.Clamp(p.x, minX, maxX);
        else p.x = limits.center.x;

        float minY = limits.min.y + halfH - margin;
        float maxY = limits.max.y - halfH + margin;
        if (minY <= maxY) p.y = Mathf.Clamp(p.y, minY, maxY);
        else p.y = limits.center.y;

        p.z = -10f;
        return p;
    }
}
