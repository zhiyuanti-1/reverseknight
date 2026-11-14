using System.IO;
using UnityEngine;

public class AutoCapture : MonoBehaviour
{
    public string directory = ".";
    public float quitAfter = 9f;

    float[] times = { 1.2f, 2.0f, 2.6f, 3.4f, 4.2f, 5.2f, 6.4f, 7.6f };
    int index;
    float t;

    void Start()
    {
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        Debug.Log("AUTOCAPTURE dir=" + Path.GetFullPath(directory));

        string[] args = System.Environment.GetCommandLineArgs();
        bool overview = false;
        foreach (var a in args) if (a == "-overview") overview = true;

        // -shotevery 0.05 [-shotfrom 0.6] : 密集抓帧，用来回看动作细节
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-shotevery" && i + 1 < args.Length)
            {
                float iv = 0.05f;
                float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out iv);
                if (iv <= 0.01f) iv = 0.01f;
                float from = 0.8f;
                if (i + 3 < args.Length && args[i + 2] == "-shotfrom")
                {
                    float.TryParse(args[i + 3], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out from);
                }
                var list = new System.Collections.Generic.List<float>();
                for (float s = from; s <= 8.8f; s += iv) list.Add(s);
                times = list.ToArray();
                quitAfter = (list.Count > 0 ? list[list.Count - 1] : 1f) + 0.3f;
            }
        }

        if (overview)
        {
            var cam = Camera.main;
            var gm = GameManager.I;
            if (cam != null && gm != null)
            {
                Bounds b = gm.LevelBounds;
                float size = Mathf.Max(b.size.x / cam.aspect, b.size.y) * 0.5f + 1f;
                cam.orthographicSize = size;
                cam.transform.position = new Vector3(b.center.x, b.center.y, -10f);
                var f = cam.GetComponent<CameraFollow>();
                if (f != null) f.enabled = false;
            }
            times = new float[] { 1.0f };
            quitAfter = 1.8f;
        }
    }

    void Diag()
    {
        var cam = Camera.main;
        if (cam == null) { Debug.Log("DIAG no camera"); return; }
        Debug.Log("DIAG cam pos=" + cam.transform.position.ToString("F2") + " ortho=" + cam.orthographic + " size=" + cam.orthographicSize + " aspect=" + cam.aspect + " rect=" + cam.rect + " mask=" + cam.cullingMask + " near=" + cam.nearClipPlane + " far=" + cam.farClipPlane);

        var gm = GameManager.I;
        if (gm == null) { Debug.Log("DIAG no gm"); return; }
        if (gm.PlayerMotor != null)
            Debug.Log("DIAG player=" + gm.PlayerMotor.transform.position.ToString("F2"));

        var root = GameObject.Find("Level_1");
        if (root == null) { Debug.Log("DIAG no root"); return; }

        var rs = root.GetComponentsInChildren<SpriteRenderer>();
        Debug.Log("DIAG rendererCount=" + rs.Length);
        int n = 0;
        foreach (var r in rs)
        {
            if (n++ >= 6) break;
            Debug.Log("DIAGR " + r.name + " pos=" + r.transform.position.ToString("F2") + " bounds=" + r.bounds.center.ToString("F2") + "/" + r.bounds.size.ToString("F2") + " visible=" + r.isVisible + " color=" + r.color + " layer=" + r.gameObject.layer);
        }
    }

    void Update()
    {
        t += Time.deltaTime;

        if (index < times.Length && t >= times[index])
        {
            string file = Path.Combine(directory, "shot_" + index.ToString("00") + ".png");
            ScreenCapture.CaptureScreenshot(file);
            Debug.Log("SHOT " + file);
            index++;
        }

        if (t >= quitAfter)
        {
            Debug.Log("AUTOCAPTURE DONE");
            Application.Quit();
        }
    }
}
