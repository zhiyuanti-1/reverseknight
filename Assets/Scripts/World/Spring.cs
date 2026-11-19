using UnityEngine;

public class Spring : MonoBehaviour
{
    public Vector2 direction = Vector2.up;
    public float strength = 18f;

    void OnTriggerEnter2D(Collider2D other)
    {
        var motor = other.GetComponent<PlayerMotor>();
        if (motor == null) return;
        AudioManager.PlayAt("spring", transform.position, 0.8f, 16f);
        motor.Launch(direction, strength);
    }
}
