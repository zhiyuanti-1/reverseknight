using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局音频入口：按名称加载 Resources/Audio，统一管理音效池、BGM 交叉淡化、
/// 环境声与脚步循环。所有切换都走淡入淡出，避免出现突兀的音量跳变。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager I;

    const string RES_DIR = "Audio/";
    const int POOL_SIZE = 16;

    readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    readonly Dictionary<string, AudioSource> loops = new Dictionary<string, AudioSource>();

    AudioSource[] pool;
    int nextIndex;

    AudioSource[] musicSrc = new AudioSource[2];
    int musicActive;
    string musicName;

    AudioSource[] ambSrc = new AudioSource[2];
    int ambActive;
    string ambName;

    AudioSource footstep;
    float footTarget;

    bool ducked;
    Coroutine musicFade;
    Coroutine ambFade;
    float musicTarget = 0.32f;
    float ambTarget = 0.4f;

    bool prevGrounded = true;
    float lastAirVy;
    float airTime;
    Collider2D prevGround;
    bool wasWater;

    static readonly bool Verbose = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-audio-verbose") >= 0;

    public static AudioClip Clip(string name)
    {
        if (I == null || string.IsNullOrEmpty(name)) return null;
        AudioClip clip;
        if (I.cache.TryGetValue(name, out clip)) return clip;
        clip = Resources.Load<AudioClip>(RES_DIR + name);
        I.cache[name] = clip;
        return clip;
    }

    void Awake()
    {
        I = this;
        pool = new AudioSource[POOL_SIZE];
        for (int i = 0; i < POOL_SIZE; i++)
            pool[i] = MakeSource("sfx_" + i, false);
        for (int i = 0; i < 2; i++)
        {
            musicSrc[i] = MakeSource("music_" + i, true);
            ambSrc[i] = MakeSource("amb_" + i, true);
        }
        footstep = MakeSource("footsteps", true);
        var all = Resources.LoadAll<AudioClip>("Audio");
        Debug.Log("[AUDIO] clips=" + all.Length);
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-audio-report") >= 0)
            StartCoroutine(ReportAudio());
    }

    IEnumerator ReportAudio()
    {
        yield return new WaitForSecondsRealtime(1.2f);
        for (int i = 0; i < 4; i++)
        {
            var m = musicSrc[musicActive];
            var a = ambSrc[ambActive];
            Debug.Log("[AUDIORT] music=" + musicName + " playing=" + (m != null && m.isPlaying) +
                      " vol=" + (m != null ? m.volume.ToString("0.00") : "-") +
                      " len=" + (m != null && m.clip != null ? m.clip.length.ToString("0.0") : "-") +
                      " | amb=" + ambName + " playing=" + (a != null && a.isPlaying) +
                      " vol=" + (a != null ? a.volume.ToString("0.00") : "-") +
                      " | listener=" + AudioListener.volume.ToString("0.00"));
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    AudioSource MakeSource(string n, bool loop)
    {
        var go = new GameObject(n);
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.volume = 0f;
        src.spatialBlend = 0f;
        return src;
    }

    /* ------------------------------------------------------------ one-shots */
    public static void Play(string name, float volume = 1f, float pitchJitter = 0f)
    {
        var clip = Clip(name);
        if (clip == null || I == null) return;
        var src = I.pool[I.nextIndex];
        I.nextIndex = (I.nextIndex + 1) % POOL_SIZE;
        src.Stop();
        src.spatialBlend = 0f;
        src.clip = clip;
        src.volume = Mathf.Clamp01(volume) * (I.ducked ? 0.5f : 1f);
        src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        src.Play();
        if (Verbose) Debug.Log("[AUDIO] " + name);
    }

    public static void PlayAt(string name, Vector3 position, float volume = 1f, float maxDistance = 18f)
    {
        var clip = Clip(name);
        if (clip == null || I == null) return;
        var src = I.pool[I.nextIndex];
        I.nextIndex = (I.nextIndex + 1) % POOL_SIZE;
        src.Stop();
        src.transform.position = position;
        src.spatialBlend = 0.85f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 2f;
        src.maxDistance = maxDistance;
        src.clip = clip;
        src.volume = Mathf.Clamp01(volume) * (I.ducked ? 0.5f : 1f);
        src.pitch = 1f;
        src.Play();
        if (Verbose) Debug.Log("[AUDIO] " + name + " @" + position.x.ToString("0.0"));
    }

    /* ---------------------------------------------------------------- loops */
    public static AudioSource Loop(string name, Transform parent, float volume = 0.6f, float maxDistance = 0f, float fadeIn = 0.35f)
    {
        if (parent == null || I == null) return null;
        string key = parent.GetInstanceID() + ":" + name;
        AudioSource src;
        if (I.loops.TryGetValue(key, out src) && src != null)
        {
            if (!src.isPlaying) I.StartCoroutine(I.FadeTo(src, Mathf.Clamp01(volume), fadeIn));
            return src;
        }
        var go = new GameObject("loop_" + name);
        go.transform.SetParent(parent, false);
        src = go.AddComponent<AudioSource>();
        src.clip = Clip(name);
        src.loop = true;
        src.playOnAwake = false;
        src.volume = 0f;
        if (maxDistance > 0f)
        {
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 2f;
            src.maxDistance = maxDistance;
        }
        I.loops[key] = src;
        if (src.clip != null)
        {
            src.Play();
            I.StartCoroutine(I.FadeTo(src, Mathf.Clamp01(volume), fadeIn));
            if (Verbose) Debug.Log("[AUDIO] loop " + name);
        }
        return src;
    }

    public static void FadeOut(AudioSource src, float duration = 0.3f)
    {
        if (src == null || I == null) return;
        I.StartCoroutine(I.FadeStop(src, duration));
    }

    public static void FadeIn(AudioSource src, float target, float duration = 0.3f)
    {
        if (src == null || I == null) return;
        I.StartCoroutine(I.FadeTo(src, Mathf.Clamp01(target), duration));
    }

    IEnumerator FadeTo(AudioSource src, float target, float duration)
    {
        float from = src.volume;
        float t = 0f;
        while (t < duration && src != null)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(from, target, Mathf.Clamp01(t / duration));
            yield return null;
        }
        if (src != null) src.volume = target;
    }

    IEnumerator FadeStop(AudioSource src, float duration)
    {
        float from = src.volume;
        float t = 0f;
        while (t < duration && src != null)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(from, 0f, Mathf.Clamp01(t / duration));
            yield return null;
        }
        if (src != null) { src.Stop(); src.volume = 0f; }
    }

    /* ----------------------------------------------------------------- music */
    public static void PlayMusic(string name, float volume = 0.5f, float fade = 0.9f)
    {
        if (I == null) return;
        if (I.musicName == name)
        {
            I.musicTarget = volume;
            return;
        }
        I.musicName = name;
        var clip = Clip(name);
        if (clip == null) { AudioManager.StopMusic(fade); return; }
        if (I.musicFade != null) I.StopCoroutine(I.musicFade);
        I.musicFade = I.StartCoroutine(I.SwapMusic(name, volume, fade));
        if (Verbose) Debug.Log("[AUDIO] music " + name);
    }

    IEnumerator SwapMusic(string name, float volume, float fade)
    {
        int old = musicActive;
        int next = 1 - old;
        var incoming = musicSrc[next];
        incoming.clip = Clip(name);
        incoming.volume = 0f;
        incoming.Play();
        musicActive = next;
        musicTarget = volume;
        float t = 0f;
        float outFrom = musicSrc[old].volume;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fade);
            incoming.volume = Mathf.Lerp(0f, volume, k) * (ducked ? 0.35f : 1f);
            musicSrc[old].volume = Mathf.Lerp(outFrom, 0f, k);
            yield return null;
        }
        incoming.volume = volume * (ducked ? 0.35f : 1f);
        musicSrc[old].Stop();
        musicSrc[old].volume = 0f;
    }

    // 兼容旧调用
    public static void StopMusic(float fade = 0.6f)
    {
        if (I == null) return;
        I.musicName = null;
        if (I.musicFade != null) I.StopCoroutine(I.musicFade);
        I.musicFade = I.StartCoroutine(I.FadeStop(I.musicSrc[I.musicActive], fade));
    }

    /* ------------------------------------------------------------- ambience */
    public static void PlayAmbience(string name, float volume = 0.5f, float fade = 1.0f)
    {
        if (I == null) return;
        if (I.ambName == name) { I.ambTarget = volume; return; }
        I.ambName = name;
        var clip = Clip(name);
        if (clip == null) { AudioManager.StopAmbience(fade); return; }
        if (I.ambFade != null) I.StopCoroutine(I.ambFade);
        I.ambFade = I.StartCoroutine(I.SwapAmb(name, volume, fade));
        if (Verbose) Debug.Log("[AUDIO] ambience " + name);
    }

    public static void StopAmbience(float fade = 0.8f)
    {
        if (I == null) return;
        I.ambName = null;
        if (I.ambFade != null) I.StopCoroutine(I.ambFade);
        I.ambFade = I.StartCoroutine(I.FadeStop(I.ambSrc[I.ambActive], fade));
    }

    IEnumerator SwapAmb(string name, float volume, float fade)
    {
        int old = ambActive;
        int next = 1 - old;
        var incoming = ambSrc[next];
        incoming.clip = Clip(name);
        incoming.volume = 0f;
        incoming.Play();
        ambActive = next;
        float t = 0f;
        float outFrom = ambSrc[old].volume;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fade);
            incoming.volume = Mathf.Lerp(0f, volume, k) * (ducked ? 0.4f : 1f);
            ambSrc[old].volume = Mathf.Lerp(outFrom, 0f, k);
            yield return null;
        }
        incoming.volume = volume * (ducked ? 0.4f : 1f);
        ambSrc[old].Stop();
        ambSrc[old].volume = 0f;
        ambTarget = volume;
    }

    /* ---------------------------------------------------------------- pause */
    public static void SetDucked(bool value)
    {
        if (I == null || I.ducked == value) return;
        I.ducked = value;
        I.StartCoroutine(I.DuckRoutine(value ? 0.35f : 1f, 0.25f));
    }

    IEnumerator DuckRoutine(float factor, float fade)
    {
        var srcs = new[] { musicSrc[musicActive], ambSrc[ambActive] };
        float[] from = new float[srcs.Length];
        for (int i = 0; i < srcs.Length; i++) from[i] = srcs[i].volume;
        float t = 0f;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fade);
            for (int i = 0; i < srcs.Length; i++)
            {
                float baseVol = i == 0 ? musicTarget : ambTarget;
                srcs[i].volume = Mathf.Lerp(from[i], baseVol * factor, k);
            }
            yield return null;
        }
    }

    /* -------------------------------------------------------------- per-frame */
    void Update()
    {
        var gm = GameManager.I;
        if (gm == null) return;

        // 暂停时压低音乐与环境声
        SetDucked(gm.Paused);

        if (gm.PlayerMotor == null)
        {
            footTarget = 0f;
            SmoothFootsteps();
            return;
        }
        var pm = gm.PlayerMotor;
        if (gm.Paused || gm.Respawning)
        {
            prevGrounded = pm.Grounded;
            prevGround = pm.GroundCollider;
            footTarget = 0f;
            SmoothFootsteps();
            return;
        }

        // 落地（弱-强-弱由音频包络负责，这里只区分轻重）
        if (!pm.Grounded)
        {
            lastAirVy = pm.Velocity.y;
            airTime += Time.deltaTime;
        }
        else if (!prevGrounded)
        {
            if (airTime > 0.08f)
                Play(lastAirVy < -14f ? "land_heavy" : "land_light", lastAirVy < -14f ? 0.9f : 0.65f, 0.05f);
            airTime = 0f;
        }

        // 入水 / 出水：只在跨越水边界时触发
        var ground = pm.GroundCollider;
        if (ground != prevGround)
        {
            bool isWater = ground != null && gm.LevelIndex == 7 && ground.GetComponent<Conveyor>() != null;
            if (isWater && !wasWater) Play("splash_in", 0.6f, 0.06f);
            else if (!isWater && wasWater) Play("splash_out", 0.45f, 0.05f);
            wasWater = isWater;
        }

        // 脚步循环：音量与速度平滑跟随
        float speed = Mathf.Abs(pm.Velocity.x);
        footTarget = pm.Grounded && speed > 1.6f ? Mathf.Clamp(0.08f + speed * 0.022f, 0.08f, 0.3f) : 0f;
        if (footTarget > 0.02f && !footstep.isPlaying)
        {
            footstep.clip = Clip("footstep_loop");
            if (footstep.clip != null)
            {
                footstep.time = Random.Range(0f, footstep.clip.length * 0.8f);
                footstep.pitch = 0.95f + Mathf.Min(0.1f, speed * 0.006f);
                footstep.Play();
                if (Verbose) Debug.Log("[AUDIO] footstep");
            }
        }
        SmoothFootsteps();

        prevGrounded = pm.Grounded;
        prevGround = ground;
    }

    void SmoothFootsteps()
    {
        if (footstep == null) return;
        footstep.volume = Mathf.MoveTowards(footstep.volume, footTarget, Time.unscaledDeltaTime * 1.2f);
        if (footstep.volume <= 0.001f && footstep.isPlaying) footstep.Stop();
    }
}
