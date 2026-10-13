using UnityEngine;

/// <summary>Persistent player settings (PlayerPrefs).</summary>
public static class GameSettings
{
    const string MasterKey = "OneRule.Master", SfxKey = "OneRule.Sfx", MusicKey = "OneRule.Music", ShakeKey = "OneRule.Shake";

    public static float Master
    {
        get => PlayerPrefs.GetFloat(MasterKey, 0.8f);
        set { PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value)); Apply(); }
    }
    public static float Sfx
    {
        get => PlayerPrefs.GetFloat(SfxKey, 0.8f);
        set => PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));
    }
    public static float Music
    {
        get => PlayerPrefs.GetFloat(MusicKey, 0.7f);
        set => PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value));
    }
    public static bool Shake
    {
        get => PlayerPrefs.GetInt(ShakeKey, 1) == 1;
        set => PlayerPrefs.SetInt(ShakeKey, value ? 1 : 0);
    }

    public static void Apply() => AudioListener.volume = Master;
    public static void Save() => PlayerPrefs.Save();
}
