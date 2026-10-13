using UnityEditor;

/// <summary>
/// Keeps the player loop ticking in Play Mode even when the Unity window is not focused
/// (needed for unattended automated play-throughs) and exposes the automated play-through as a menu item.
/// </summary>
[InitializeOnLoad]
static class PlayModeTicker
{
    static PlayModeTicker()
    {
        EditorApplication.update += () =>
        {
            if (EditorApplication.isPlaying && !EditorApplication.isPaused)
                EditorApplication.QueuePlayerLoopUpdate();
        };
    }

    [MenuItem("Tools/One Rule/Run Automated Play-Through")]
    static void RunAutoTest()
    {
        EditorPrefs.SetBool("OneRule.AutoTest", true);
        if (!EditorApplication.isPlaying) EditorApplication.EnterPlaymode();
    }
}
