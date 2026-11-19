using UnityEngine;

public class PlayerAgent : MonoBehaviour
{
    Bounds limits;
    bool hasLimits;

    public void SetLimits(Bounds b)
    {
        limits = b;
        hasLimits = true;
    }

    void Update()
    {
        if (!hasLimits) return;
        var p = transform.position;
        if (p.y < limits.min.y - 4f || p.y > limits.max.y + 6f ||
            p.x < limits.min.x - 6f || p.x > limits.max.x + 6f)
        {
            var gm = GameManager.I;
            if (gm != null) gm.KillPlayer(this, "fall");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var gm = GameManager.I;
        if (gm == null) return;

        if (other.GetComponent<Hazard>() != null)
        {
            string cause = other.GetComponent<PressurePulse>() != null ? "water" : "spike";
            gm.KillPlayer(this, cause);
            return;
        }

        var cp = other.GetComponent<Checkpoint>();
        if (cp != null)
        {
            cp.Activate();
            return;
        }

        if (other.GetComponent<Goal>() != null)
        {
            gm.CompleteLevel(this);
        }
    }
}
