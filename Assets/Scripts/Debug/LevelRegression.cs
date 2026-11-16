using System.Collections;
using UnityEngine;

// Opt-in physics/camera regression: ReverseKnight.exe -level-regression -logFile regression.log
public class LevelRegression : MonoBehaviour, IInputSource
{
    public float Move { get; private set; }
    bool flip;
    public bool FlipPressed { get { bool pressed = flip; flip = false; return pressed; } }
    int failures;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-level-regression") >= 0)
            new GameObject("Level regression").AddComponent<LevelRegression>();
    }

    void Check(bool condition, string message)
    {
        if (condition) Debug.Log("REGRESSION PASS " + message);
        else { failures++; Debug.LogError("REGRESSION FAIL " + message); }
    }

    IEnumerator Start()
    {
        PlayerInput.Source = this;
        yield return null;
        var gm = GameManager.I;
        var cam = Camera.main;
        var follow = cam.GetComponent<CameraFollow>();
        // Direct transitions and downward checkpoint snaps must select the correct floor.
        foreach (int level in new[] { 3, 5, 8 })
        {
            gm.LoadLevel(level - 1);
            yield return null;
            var motor = gm.PlayerMotor;
            motor.Frozen = true;
            float[] heights = level == 5 ? new[] { 3.72f, 10.72f, 16.72f, 22.72f, 28.72f, 3.72f }
                : level == 3 ? new[] { 2.72f, 9.72f, 16.72f, 23.72f, 30.72f, 2.72f }
                : new[] { 3.72f, 7.28f, 3.72f };
            foreach (float aspect in new[] { 4f / 3f, 16f / 9f, 21f / 9f })
            {
                cam.aspect = aspect;
                foreach (float y in heights)
                {
                    motor.Teleport(new Vector2(4f, y));
                    follow.SnapToTarget();
                    yield return null;
                    Check(Mathf.Abs(cam.transform.position.y - y) + 1.4f <= cam.orthographicSize,
                        "camera level=" + level + " y=" + y + " aspect=" + aspect);
                }
            }
        }
        cam.ResetAspect();

        // Ride every elevator with the real player motor, including the top landing.
        gm.LoadLevel(4);
        yield return new WaitForSeconds(0.1f);
        var platforms = FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None);
        foreach (var platform in platforms)
        {
            Vector2 start = platform.transform.position;
            gm.PlayerMotor.Teleport(start + Vector2.up * 0.92f);
            follow.SnapToTarget();
            yield return new WaitForSeconds(3.7f);
            Check(gm.PlayerMotor.transform.position.y > start.y + 7.5f && gm.Deaths == 0,
                "elevator at " + start + " platform=" + platform.transform.position
                + " player=" + gm.PlayerMotor.transform.position + " deaths=" + gm.Deaths
                + " grounded=" + gm.PlayerMotor.Grounded);
            var size = new Vector2(0.65f, 1.3f);
            var hits = Physics2D.OverlapBoxAll(gm.PlayerMotor.transform.position, size, 0f);
            foreach (var hit in hits)
                Check(hit.isTrigger || hit.transform == gm.PlayerMotor.transform,
                    "landing clear of " + hit.name + " at " + start);
        }

        // Traverse all three fragile ceiling sections using ordinary run/flip input.
        gm.LoadLevel(5);
        yield return new WaitForSeconds(0.1f);
        Move = 1f;
        foreach (float x in new[] { 14.5f, 28.5f, 41.5f, 57.5f, 71.5f, 87.5f })
        {
            float deadline = Time.time + 5f;
            while (gm.PlayerMotor.transform.position.x < x && gm.Deaths == 0 && Time.time < deadline)
                yield return null;
            if (gm.Deaths > 0 || Time.time >= deadline) break;
            flip = true;
            yield return new WaitForSeconds(0.2f);
        }
        float finishDeadline = Time.time + 5f;
        while (!gm.Cleared && gm.Deaths == 0 && Time.time < finishDeadline) yield return null;
        Move = 0f;
        Check(gm.Cleared && gm.Deaths == 0, "level 6 full route without deaths");
        Debug.Log("REGRESSION DONE failures=" + failures);
        PlayerInput.Source = null;
        Application.Quit(failures == 0 ? 0 : 1);
    }
}
