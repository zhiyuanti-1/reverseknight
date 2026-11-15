using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

// 调试探针（-probe <csv>）：逐帧记录物理位置、渲染插值位置、重力朝向和精灵帧，
// 用来量化物理固定步与显示刷新率不一致造成的跑动抖动。
public class MotionProbe : MonoBehaviour
{
    public string file = "motion_probe.csv";
    public float duration = 12f;

    StreamWriter writer;
    PlayerVisual visual;
    float started = -1f;
    bool finished;

    void Start()
    {
        writer = new StreamWriter(file, false, new UTF8Encoding(false));
        writer.WriteLine("frame,rt,dt,time,fixedTime,fixedDt,camX,camY,rootX,rootY,visualX,visualY,vx,grounded,flip,gravity,roll,flipVisual,sprite,gcKB");
    }

    void Update()
    {
        if (writer == null || finished) return;

        var gm = GameManager.I;
        if (gm == null || gm.PlayerMotor == null) return;
        var motor = gm.PlayerMotor;
        if (visual == null) visual = motor.GetComponent<PlayerVisual>();
        if (started < 0f) started = Time.realtimeSinceStartup;

        string sprite = visual != null && visual.body != null && visual.body.sprite != null
            ? visual.body.sprite.name : "";
        var cam = Camera.main;
        Vector3 root = motor.transform.position;
        Vector3 shown = motor.VisualPosition;
        int gcKb = (int)(System.GC.GetTotalMemory(false) / 1024);

        writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
            "{0},{1:F4},{2:F4},{3:F4},{4:F4},{5:F5},{6:F3},{7:F3},{8:F3},{9:F3},{10:F3},{11:F3},{12:F3},{13},{14},{15},{16:F1},{17},{18},{19}",
            Time.frameCount, Time.realtimeSinceStartup, Time.unscaledDeltaTime, Time.time, Time.fixedTime,
            Time.fixedDeltaTime,
            cam != null ? cam.transform.position.x : 0f, cam != null ? cam.transform.position.y : 0f,
            root.x, root.y, shown.x, shown.y,
            motor.Velocity.x, motor.Grounded ? 1 : 0, motor.InFlip ? 1 : 0, motor.GravitySign,
            visual != null ? visual.Roll : 0f, visual != null && visual.FlipVisual ? 1 : 0,
            sprite, gcKb));

        if (Time.realtimeSinceStartup - started >= duration)
        {
            finished = true;
            writer.Flush();
            writer.Close();
            writer = null;
            Debug.Log("PROBE DONE");
            Application.Quit();
        }
    }

    void OnDestroy()
    {
        if (writer != null)
        {
            writer.Flush();
            writer.Close();
            writer = null;
        }
    }
}
