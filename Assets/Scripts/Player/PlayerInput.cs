using UnityEngine;

public interface IInputSource
{
    float Move { get; }
    bool FlipPressed { get; }
}

public class PlayerInput : MonoBehaviour
{
    public static IInputSource Source;

    public float Move { get; private set; }

    bool flipPending;

    void Update()
    {
        if (Source != null)
        {
            Move = Mathf.Clamp(Source.Move, -1f, 1f);
            if (Source.FlipPressed) flipPending = true;
            return;
        }

        float m = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) m -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) m += 1f;
        Move = m;

        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.J) ||
            Input.GetKeyDown(KeyCode.LeftShift) ||
            Input.GetKeyDown(KeyCode.K)) flipPending = true;
    }

    public bool TakeFlip()
    {
        if (!flipPending) return false;
        flipPending = false;
        return true;
    }

    public void ClearBuffers()
    {
        flipPending = false;
    }
}
