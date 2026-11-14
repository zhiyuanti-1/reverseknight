using UnityEngine;

public class Hazard : MonoBehaviour
{
}

public class Goal : MonoBehaviour
{
    float t;

    void Update()
    {
        t += Time.deltaTime;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float k = 0.75f + 0.25f * Mathf.Sin(t * 3f);
            sr.color = new Color(Palette.Goal.r * k, Palette.Goal.g * k, Palette.Goal.b * k, 1f);
        }
    }
}

public class Checkpoint : MonoBehaviour
{
    public int index;
    public Vector2 respawnPoint;
    public bool activated;

    SpriteRenderer flag;

    public void Setup(int i, Vector2 point, SpriteRenderer flagRenderer)
    {
        index = i;
        respawnPoint = point;
        flag = flagRenderer;
        if (flag != null) flag.color = Palette.CheckpointOff;
    }

    public void Activate()
    {
        var gm = GameManager.I;
        if (gm == null) return;
        if (!activated)
        {
            activated = true;
            if (flag != null) flag.color = Palette.CheckpointOn;
        }
        gm.SetCheckpoint(respawnPoint);
    }
}
