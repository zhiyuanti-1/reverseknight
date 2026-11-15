using UnityEngine;

// 摆锤：绕悬挂点做正弦摆动，末端挂一个 Hazard 触发器。
// 摆臂和锤头都是纯视觉/触发器，不做刚体碰撞。
public class Pendulum : MonoBehaviour
{
    [Min(0.1f)] public float period = 2.8f;
    [Range(0f, 90f)] public float amplitude = 65f;
    public float phase;

    void Update()
    {
        float angle = amplitude * Mathf.Sin((Time.time / Mathf.Max(0.1f, period)) * Mathf.PI * 2f + phase);
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
