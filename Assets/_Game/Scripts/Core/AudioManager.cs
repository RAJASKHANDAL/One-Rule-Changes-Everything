using System.Collections.Generic;
using UnityEngine;

public enum Sfx { Click, Coin, Jump, Land, Hurt, Warning, RuleStart, RuleEnd, GameOver, Flip, LowHealth, Bonus, Combo, NearMiss, MissionDone, NewBest, RealityBreak, Mutation }

/// <summary>
/// Sound effects plus an intensity-driven three-layer music bed. Everything is generated procedurally so no audio assets
/// are required; assign clips to <see cref="musicLayerClips"/> later to replace the generated layers.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Optional: replace the generated music (3 layers: calm / energy / drive; loops must share one length)")]
    public AudioClip[] musicLayerClips;
    [Range(0f, 1f)] public float musicBaseVolume = 0.5f;

    AudioSource sfxSource, warnSource;
    readonly AudioSource[] layers = new AudioSource[3];
    readonly float[] layerVolume = new float[3];
    readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
    float heartbeatTimer;
    float pitch = 1f;
    const int Rate = 44100;
    const int MusicRate = 22050;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        warnSource = gameObject.AddComponent<AudioSource>();
        warnSource.playOnAwake = false;
        GameSettings.Apply();
        BuildSfx();
        BuildMusic();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public static void Play(Sfx sfx, float volume = 1f)
    {
        if (Instance == null || !Instance.clips.TryGetValue(sfx, out var clip)) return;
        var gm = GameManager.Instance;
        if (sfx == Sfx.Warning && gm != null && gm.intensity != null)
        {
            // warnings get slightly more urgent (higher pitch) as intensity grows
            Instance.warnSource.pitch = Mathf.Lerp(1f, 1.25f, gm.intensity.Value);
            Instance.warnSource.PlayOneShot(clip, volume * GameSettings.Sfx);
            return;
        }
        Instance.sfxSource.PlayOneShot(clip, volume * GameSettings.Sfx);
    }

    // ------------------------------------------------------------------ music mixing

    void Update()
    {
        UpdateMusic(Time.unscaledDeltaTime);
        UpdateHeartbeat(Time.unscaledDeltaTime);
    }

    void UpdateMusic(float dt)
    {
        if (layers[0] == null) return;
        var gm = GameManager.Instance;
        float t0 = 0.55f, t1 = 0f, t2 = 0f, targetPitch = 1f;

        if (gm != null && gm.State != GameState.MainMenu)
        {
            float i = gm.intensity != null ? gm.intensity.Value : 0f;
            bool low = gm.health != null && gm.health.IsLow && gm.State != GameState.GameOver;
            t0 = 0.6f;
            t1 = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.5f, i)) * 0.55f;
            t2 = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.85f, i)) * 0.85f;
            if (low) { t0 += 0.1f; t1 = Mathf.Max(t1, 0.4f); }   // restrained urgency boost on the last heart
            targetPitch = 1f + 0.02f * i + (low ? 0.025f : 0f);

            if (gm.State == GameState.Paused) { t0 *= 0.35f; t1 *= 0.35f; t2 *= 0.35f; }       // duck while paused
            else if (gm.State == GameState.GameOver) { t0 = 0.25f; t1 = 0f; t2 = 0f; targetPitch = 1f; }
        }

        pitch = Mathf.MoveTowards(pitch, targetPitch, dt * 0.05f);
        float master = GameSettings.Music * musicBaseVolume;
        layerVolume[0] = Mathf.MoveTowards(layerVolume[0], t0, dt * 0.4f);
        layerVolume[1] = Mathf.MoveTowards(layerVolume[1], t1, dt * 0.4f);
        layerVolume[2] = Mathf.MoveTowards(layerVolume[2], t2, dt * 0.4f);
        for (int k = 0; k < 3; k++)
        {
            layers[k].volume = layerVolume[k] * master;
            layers[k].pitch = pitch;
        }
    }

    void UpdateHeartbeat(float dt)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing || gm.health == null || !gm.health.IsLow) { heartbeatTimer = 1.2f; return; }
        heartbeatTimer -= dt;
        if (heartbeatTimer > 0f) return;
        Play(Sfx.LowHealth, 0.45f);
        heartbeatTimer = Mathf.Lerp(2.4f, 1.6f, gm.intensity != null ? gm.intensity.Value : 0f);
    }

    // ------------------------------------------------------------------ generation

    struct Note { public float Dur, F0, F1; public bool Square; public Note(float d, float a, float b, bool sq = false) { Dur = d; F0 = a; F1 = b; Square = sq; } }

    void BuildSfx()
    {
        clips[Sfx.Click] = Make("click", new Note(0.06f, 900, 700));
        clips[Sfx.Coin] = Make("coin", new Note(0.07f, 988, 988), new Note(0.16f, 1319, 1319));
        clips[Sfx.Jump] = Make("jump", new Note(0.14f, 320, 640));
        clips[Sfx.Land] = Make("land", new Note(0.08f, 150, 70));
        clips[Sfx.Hurt] = Make("hurt", new Note(0.25f, 300, 90, true));
        clips[Sfx.Warning] = Make("warn", new Note(0.1f, 700, 700, true), new Note(0.1f, 0, 0), new Note(0.1f, 700, 700, true));
        clips[Sfx.RuleStart] = Make("rule", new Note(0.1f, 440, 440), new Note(0.1f, 554, 554), new Note(0.2f, 740, 740));
        clips[Sfx.RuleEnd] = Make("ruleend", new Note(0.1f, 740, 740), new Note(0.2f, 554, 554));
        clips[Sfx.GameOver] = Make("over", new Note(0.2f, 392, 392, true), new Note(0.2f, 330, 330, true), new Note(0.45f, 220, 110, true));
        clips[Sfx.Flip] = Make("flip", new Note(0.3f, 200, 900));
        clips[Sfx.LowHealth] = Make("lowhealth", new Note(0.09f, 90, 60), new Note(0.08f, 0, 0), new Note(0.12f, 80, 50));
        clips[Sfx.Bonus] = Make("bonus", new Note(0.06f, 784, 784), new Note(0.06f, 988, 988), new Note(0.14f, 1319, 1319));
        clips[Sfx.Combo] = Make("combo", new Note(0.05f, 880, 880), new Note(0.05f, 1109, 1109), new Note(0.05f, 1319, 1319), new Note(0.12f, 1760, 1760));
        clips[Sfx.NearMiss] = Make("nearmiss", new Note(0.12f, 600, 1500));
        clips[Sfx.MissionDone] = Make("mission", new Note(0.08f, 523, 523), new Note(0.08f, 659, 659), new Note(0.08f, 784, 784), new Note(0.2f, 1047, 1047));
        clips[Sfx.RealityBreak] = Make("realitybreak", new Note(0.35f, 110, 220, true), new Note(0.35f, 220, 440), new Note(0.45f, 440, 1200));
        clips[Sfx.Mutation] = Make("mutation", new Note(0.12f, 330, 330, true), new Note(0.12f, 392, 392, true), new Note(0.12f, 494, 494, true), new Note(0.3f, 660, 990));
        clips[Sfx.NewBest] = Make("newbest", new Note(0.1f, 659, 659), new Note(0.1f, 784, 784), new Note(0.1f, 988, 988), new Note(0.1f, 784, 784), new Note(0.3f, 1319, 1319));
    }

    static AudioClip Make(string name, params Note[] notes)
    {
        float total = 0; foreach (var n in notes) total += n.Dur;
        var data = new float[Mathf.CeilToInt(total * Rate)];
        int idx = 0; float phase = 0;
        foreach (var n in notes)
        {
            int count = Mathf.CeilToInt(n.Dur * Rate);
            for (int i = 0; i < count && idx < data.Length; i++, idx++)
            {
                float t = (float)i / count;
                if (n.F0 <= 0) { data[idx] = 0; continue; }
                float f = Mathf.Lerp(n.F0, n.F1, t);
                phase += f / Rate; phase -= Mathf.Floor(phase);
                float w = n.Square ? (phase < 0.5f ? 0.6f : -0.6f) : Mathf.Sin(phase * Mathf.PI * 2f);
                float env = Mathf.Min(1f, i / (Rate * 0.004f)) * Mathf.Pow(1f - t, 1.6f);
                data[idx] = w * env * 0.35f;
            }
        }
        var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void BuildMusic()
    {
        var clipsToUse = new AudioClip[3];
        bool custom = musicLayerClips != null && musicLayerClips.Length > 0 && musicLayerClips[0] != null;
        if (custom)
        {
            for (int k = 0; k < 3; k++) clipsToUse[k] = k < musicLayerClips.Length ? musicLayerClips[k] : null;
        }
        else
        {
            var generated = GenerateMusicLayers();
            for (int k = 0; k < 3; k++) clipsToUse[k] = generated[k];
        }

        for (int k = 0; k < 3; k++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false; src.loop = true; src.volume = 0f; src.clip = clipsToUse[k];
            layers[k] = src;
        }
        double start = AudioSettings.dspTime + 0.3; // start together so the layers stay in sync
        foreach (var src in layers) if (src.clip != null) src.PlayScheduled(start);
    }

    /// <summary>Am - F - C - G loop at 104 BPM: pad (calm), bass + plucks (energy), fast arpeggio + hats (drive).</summary>
    static AudioClip[] GenerateMusicLayers()
    {
        const float bpm = 104f;
        float beat = 60f / bpm;
        int length = Mathf.RoundToInt(16f * beat * MusicRate);
        var pad = new float[length]; var energy = new float[length]; var drive = new float[length];

        float[] roots = { 110f, 87.31f, 130.81f, 98f };
        float[][] chords =
        {
            new[] { 220f, 261.63f, 329.63f }, new[] { 174.61f, 220f, 261.63f },
            new[] { 261.63f, 329.63f, 392f }, new[] { 196f, 246.94f, 293.66f }
        };
        var rnd = new System.Random(7);

        for (int bar = 0; bar < 4; bar++)
        {
            float barStart = bar * 4f * beat;
            foreach (float f in chords[bar]) Tone(pad, barStart, 4f * beat, f, 0.045f, 0, 0.6f, 0.7f);
            Tone(pad, barStart, 4f * beat, roots[bar], 0.06f, 0, 0.5f, 0.7f);

            for (int e = 0; e < 8; e++) // bass eighths
                Tone(energy, barStart + e * beat * 0.5f, beat * 0.42f, roots[bar], e % 2 == 0 ? 0.16f : 0.1f, 1, 0.006f, 0.16f);
            for (int b = 0; b < 4; b++) // soft plucks on the beat
                Tone(energy, barStart + b * beat, beat * 0.9f, chords[bar][b % 3] * 2f, 0.045f, 1, 0.004f, beat * 0.8f);

            for (int s = 0; s < 16; s++) // sixteenth arpeggio
                Tone(drive, barStart + s * beat * 0.25f, beat * 0.2f, chords[bar][s % 3] * 4f, 0.032f, 2, 0.003f, beat * 0.17f);
            for (int h = 0; h < 8; h++) // offbeat hats
                if (h % 2 == 1) Hat(drive, barStart + h * beat * 0.5f, 0.05f, 0.05f, rnd);
        }

        var result = new AudioClip[3];
        var arrays = new[] { pad, energy, drive };
        string[] names = { "music_calm", "music_energy", "music_drive" };
        for (int k = 0; k < 3; k++)
        {
            result[k] = AudioClip.Create(names[k], length, 1, MusicRate, false);
            result[k].SetData(arrays[k], 0);
        }
        return result;
    }

    static void Tone(float[] buf, float start, float dur, float freq, float amp, int wave, float attack, float release)
    {
        int s0 = Mathf.RoundToInt(start * MusicRate), n = Mathf.RoundToInt(dur * MusicRate);
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)MusicRate;
            float env = Mathf.Clamp01(t / attack) * Mathf.Clamp01((dur - t) / release);
            phase += freq / MusicRate; phase -= Mathf.Floor(phase);
            float w;
            switch (wave)
            {
                case 1: w = 1f - 4f * Mathf.Abs(phase - 0.5f) + 0.0f; break; // triangle
                case 2: w = Mathf.Sin(phase * Mathf.PI * 2f) + 0.3f * Mathf.Sin(phase * Mathf.PI * 6f); break; // soft square-ish
                default: w = Mathf.Sin(phase * Mathf.PI * 2f); break;
            }
            buf[(s0 + i) % buf.Length] += w * env * amp;
        }
    }

    static void Hat(float[] buf, float start, float dur, float amp, System.Random rnd)
    {
        int s0 = Mathf.RoundToInt(start * MusicRate), n = Mathf.RoundToInt(dur * MusicRate);
        for (int i = 0; i < n; i++)
        {
            float env = 1f - i / (float)n;
            buf[(s0 + i) % buf.Length] += ((float)rnd.NextDouble() * 2f - 1f) * env * env * amp;
        }
    }
}
