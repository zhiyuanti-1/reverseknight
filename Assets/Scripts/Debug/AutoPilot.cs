using UnityEngine;

public class AutoPilot : MonoBehaviour, IInputSource
{
    public float Move { get; private set; }
    public bool FlipPressed { get; private set; }

    float t;
    int flips;
    float logTimer;
    PlayerMotor motor;

    void Awake()
    {
        PlayerInput.Source = this;
        Debug.Log("AUTOPILOT ON");
    }

    void OnDestroy()
    {
        if (PlayerInput.Source == this) PlayerInput.Source = null;
    }

    void Update()
    {
        t += Time.deltaTime;
        Move = 1f;
        FlipPressed = false;

        // 每 0.7 秒翻一次，模拟连续翻转的节奏
        float nextFlip = 1.2f + flips * 0.7f;
        if (t >= nextFlip && flips < 12)
        {
            FlipPressed = true;
            flips++;
        }

        if (motor == null && GameManager.I != null) motor = GameManager.I.PlayerMotor;

        logTimer += Time.deltaTime;
        if (logTimer >= 0.2f)
        {
            logTimer = 0f;
            if (motor != null)
            {
                Vector3 p = motor.transform.position;
                Debug.Log(string.Format("TEL t={0:0.0} pos=({1:0.00},{2:0.00}) v=({3:0.00},{4:0.00}) g={5} grounded={6} flip={7}",
                    t, p.x, p.y, motor.Velocity.x, motor.Velocity.y, motor.GravitySign, motor.Grounded, motor.InFlip));
            }
        }
    }
}
