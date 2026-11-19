using System.Collections;
using System.IO;
using UnityEngine;

// Real input/physics regression; optional -water-shots <directory> captures the running game.
public class WaterRegression : MonoBehaviour, IInputSource
{
    public float Move { get; private set; }
    bool flip;
    public bool FlipPressed { get { bool value = flip; flip = false; return value; } }
    int failures;
    string shots;
    GameManager gm;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-water-regression") >= 0)
            new GameObject("Water regression").AddComponent<WaterRegression>();
    }
    void Check(bool pass, string message)
    { if (pass) Debug.Log("WATER PASS " + message); else { failures++; Debug.LogError("WATER FAIL " + message); } }
    IEnumerator Shot(string name)
    {
        if (string.IsNullOrEmpty(shots)) yield break;
        gm.PlayerMotor.Frozen = true;
        Camera.main.GetComponent<CameraFollow>().SnapToTarget();
        yield return null;
        // Hidden player windows may not present a backbuffer. Render the actual
        // game camera explicitly; this captures the world, without the OnGUI HUD.
        var camera = Camera.main;
        var previous = camera.targetTexture;
        var active = RenderTexture.active;
        var render = RenderTexture.GetTemporary(1280, 720, 24);
        camera.targetTexture = render; camera.Render(); RenderTexture.active = render;
        var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
        File.WriteAllBytes(Path.Combine(shots, name + ".png"), pixels.EncodeToPNG());
        camera.targetTexture = previous; RenderTexture.active = active;
        RenderTexture.ReleaseTemporary(render); Destroy(pixels);
        yield return new WaitForSeconds(0.15f);
        gm.PlayerMotor.Frozen = false;
    }
    IEnumerator Start()
    {
        var args = System.Environment.GetCommandLineArgs();
        int index = System.Array.IndexOf(args, "-water-shots");
        if (index >= 0 && index + 1 < args.Length) { shots = args[index + 1]; Directory.CreateDirectory(shots); }
        PlayerInput.Source = this;
        yield return null;
        gm = GameManager.I; gm.LoadLevel(7);
        yield return new WaitForSeconds(0.15f);
        Check(FindObjectsByType<WaterChannelVisual>(FindObjectsSortMode.None).Length == 10, "10 water channels");
        Check(FindObjectsByType<ConveyorVisual>(FindObjectsSortMode.None).Length == 0, "no mechanical rollers");
        var nozzles = FindObjectsByType<PressureNozzle>(FindObjectsSortMode.None);
        int ceilingNozzles = System.Array.FindAll(nozzles, n => n.transform.position.y > 5.5f).Length;
        Check(nozzles.Length == 4 && ceilingNozzles == 2, "two floor and two ceiling nozzles");
        gm.PlayerMotor.Teleport(new Vector2(8.5f, 3.72f));
        yield return new WaitForSeconds(0.5f);
        Check(Mathf.Abs(gm.PlayerMotor.Velocity.x - 4) < 0.1f, "idle drift +4");
        Move = 1; yield return new WaitForSeconds(0.4f);
        Check(Mathf.Abs(gm.PlayerMotor.Velocity.x - 10) < 0.1f, "downstream speed +10");
        Move = 0; flip = true; yield return new WaitForSeconds(0.5f);
        Check(gm.PlayerMotor.GravitySign == -1 && Mathf.Abs(gm.PlayerMotor.Velocity.x + 4) < 0.1f, "ceiling drift -4");
        // Re-center on the same channel so the sample cannot run onto its dry end.
        gm.PlayerMotor.transform.position = new Vector3(10, 7.28f, 0);
        Physics2D.SyncTransforms();
        Move = 1; yield return new WaitForSeconds(0.3f);
        Check(Mathf.Abs(gm.PlayerMotor.Velocity.x - 4) < 0.1f,
            "upstream speed +4 actual=" + gm.PlayerMotor.Velocity.x);
        Move = 0; gm.PlayerMotor.Teleport(new Vector2(40.5f, 3.72f));
        yield return new WaitForSeconds(0.4f);
        Check(Mathf.Abs(gm.PlayerMotor.Velocity.x) < 0.1f, "dry island stationary");

        var nozzle = System.Array.Find(nozzles, n => n.transform.position.x > 48 && n.transform.position.x < 49);
        gm.PlayerMotor.Teleport(new Vector2(nozzle.ZoneMin + 0.5f, 3.72f));
        gm.PlayerMotor.Frozen = true; nozzle.ResetCycle(); int count = nozzle.Shots;
        yield return new WaitForSeconds(0.45f);
        Check(nozzle.Charging && nozzle.Shots == count && nozzle.Charge > 0.3f, "full first-shot warning");
        gm.TogglePause(); float charge = nozzle.Charge;
        yield return new WaitForSecondsRealtime(0.2f);
        Check(Mathf.Abs(nozzle.Charge - charge) < 0.01f, "pause freezes warning");
        gm.TogglePause(); gm.PlayerMotor.Frozen = true;
        yield return new WaitForSeconds(0.5f);
        Check(nozzle.Shots == count + 1, "fires after warning");
        gm.PlayerMotor.Teleport(new Vector2(nozzle.ZoneMax + 1, 3.72f));
        yield return null; yield return null;
        Check(FindObjectsByType<PressurePulse>(FindObjectsSortMode.None).Length == 0 && !nozzle.Charging,
            "leaving zone clears pressure");
        foreach (var cp in FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
        {
            cp.Activate(); gm.PlayerMotor.Frozen = false;
            gm.KillPlayer(gm.Agent); yield return new WaitForSeconds(0.25f);
            Check(Vector2.Distance(gm.PlayerMotor.transform.position, cp.respawnPoint) < 0.2f,
                "checkpoint retry " + cp.index);
            Check(FindObjectsByType<PressurePulse>(FindObjectsSortMode.None).Length == 0, "retry clears pulses");
        }

        foreach (var gun in nozzles)
        {
            int deathsBefore = gm.Deaths;
            float targetY = gun.transform.position.y < 5.5f ? 7.28f : 3.72f;
            gm.PlayerMotor.Teleport(new Vector2(gun.transform.position.x - 3f, targetY));
            gm.PlayerMotor.Frozen = true;
            gun.ResetCycle();
            yield return new WaitForSeconds(1.25f);
            Check(gm.Deaths == deathsBefore + 1, "pulse hits diagonally backwards at x=" + gun.transform.position.x);
        }

        gm.PlayerMotor.Teleport(new Vector2(nozzle.transform.position.x + 14f, 3.72f));
        gm.PlayerMotor.Frozen = true;
        int longRangeDeaths = gm.Deaths;
        yield return new WaitForSeconds(1.85f);
        Check(gm.Deaths == longRangeDeaths + 1, "pulse hits beyond original 12-grid range");

        gm.LoadLevel(7); yield return new WaitForSeconds(0.15f);
        nozzles = FindObjectsByType<PressureNozzle>(FindObjectsSortMode.None);
        Move = 1;
        foreach (float x in new[] { 20f, 27f, 45f, 51f, 71f, 77f, 98f, 104f })
        {
            float deadline = Time.time + 10;
            while (gm.PlayerMotor.transform.position.x < x && gm.Deaths == 0 && Time.time < deadline) yield return null;
            if (gm.Deaths > 0 || Time.time >= deadline) break;
            var activeGun = System.Array.Find(nozzles, gun => x >= gun.ZoneMin && x <= gun.ZoneMin + 3f);
            if (activeGun != null)
            {
                // Brake with A before the spikes, then let the current carry the knight
                // gently while the shot locks.
                Move = -1;
                while (gm.PlayerMotor.Velocity.x > 0.5f && gm.Deaths == 0 && Time.time < deadline) yield return null;
                Move = 0;
                while (activeGun.Charge < 0.57f && gm.Deaths == 0 && Time.time < deadline) yield return null;
                if (gm.Deaths > 0 || Time.time >= deadline) break;
                if (x == 20 || x == 45 || x == 98) yield return Shot("water_route_" + x);
                Move = 1;
            }
            Debug.Log("WATER ROUTE flip from " + gm.PlayerMotor.transform.position.ToString("F2"));
            flip = true; yield return new WaitForSeconds(0.2f);
        }
        float end = Time.time + 5;
        while (!gm.Cleared && gm.Deaths == 0 && Time.time < end) yield return null;
        Move = 0;
        Check(gm.Cleared && gm.Deaths == 0, "full route zero deaths at " + gm.PlayerMotor.transform.position);
        int activated = 0;
        foreach (var cp in FindObjectsByType<Checkpoint>(FindObjectsSortMode.None)) if (cp.activated) activated++;
        Check(activated == 4, "all four checkpoints reached");
        Debug.Log("WATER REGRESSION DONE failures=" + failures);
        PlayerInput.Source = null;
        Application.Quit(failures == 0 ? 0 : 1);
    }
}
