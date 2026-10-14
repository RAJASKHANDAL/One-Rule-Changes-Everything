#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only scripted play-through used to verify the game loop. Runs only when EditorPrefs "OneRule.AutoTest" is true.
/// Results are written to the Console with the "[AUTOTEST]" prefix and screenshots to the temp folder.
/// </summary>
public class AutoTester : MonoBehaviour
{
    GameManager gm;
    PlayerController pc;
    int passes, fails;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (EditorPrefs.GetBool("OneRule.AutoTest", false)) new GameObject("AutoTester").AddComponent<AutoTester>();
    }

    void Start()
    {
        if (!EditorPrefs.GetBool("OneRule.AutoTest", false)) { enabled = false; return; }
        EditorPrefs.SetBool("OneRule.AutoTest", false);
        gm = GameManager.Instance; pc = gm.player;
        StartCoroutine(Run());
    }

    void Check(string name, bool ok, string detail = "")
    {
        if (ok) passes++; else fails++;
        Debug.Log("[AUTOTEST] " + (ok ? "PASS " : "FAIL ") + name + (detail.Length > 0 ? " - " + detail : ""));
    }

    IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetTempPath(), "onerule_" + name + ".png"));
        yield return null;
    }

    IEnumerator Run()
    {
        yield return new WaitForSeconds(1.2f);
        Check("starts in MainMenu", gm.State == GameState.MainMenu);
        yield return Shot("1_menu");

        gm.StartRun();
        yield return new WaitForSeconds(1.0f);
        Check("PLAY -> Playing", gm.State == GameState.Playing);
        Check("player grounded on spawn platform", pc.IsGrounded, "y=" + pc.transform.position.y.ToString("F2"));
        yield return Shot("2_play");

        // movement (camera-relative) and diagonal speed
        var p0 = pc.transform.position;
        pc.debugMove = Vector2.up;
        yield return new WaitForSeconds(0.6f);
        float straight = pc.HorizontalSpeed;
        pc.debugMove = Vector2.zero;
        yield return new WaitForSeconds(0.4f);
        Check("W moves player", (pc.transform.position - p0).magnitude > 1.5f, "moved " + (pc.transform.position - p0).magnitude.ToString("F2"));
        pc.debugMove = new Vector2(1, 1);
        yield return new WaitForSeconds(0.5f);
        Check("diagonal not faster", pc.HorizontalSpeed <= straight + 0.2f, straight.ToString("F2") + " vs " + pc.HorizontalSpeed.ToString("F2"));
        pc.debugMove = Vector2.zero;

        // jump
        gm.StartRun(); // no-op while playing
        yield return new WaitForSeconds(1.0f);
        float groundY = pc.transform.position.y;
        pc.debugJump = true;
        float maxY = groundY;
        for (int i = 0; i < 40; i++) { yield return null; maxY = Mathf.Max(maxY, pc.transform.position.y); }
        Check("jump height ~2", maxY - groundY > 1.6f && maxY - groundY < 2.4f, (maxY - groundY).ToString("F2"));
        yield return new WaitForSeconds(0.8f);
        Check("lands again", pc.IsGrounded);

        // pause / resume
        float t0 = gm.SurvivalTime;
        gm.Pause();
        yield return new WaitForSecondsRealtime(0.6f);
        Check("Pause stops time + timer", gm.State == GameState.Paused && Time.timeScale == 0f && Mathf.Approximately(t0, gm.SurvivalTime));
        yield return Shot("3_pause");
        Check("rules cannot start while paused", !gm.rules.DebugStart("never_stop"));
        gm.Resume();
        yield return new WaitForSeconds(0.3f);
        Check("Resume restores play", gm.State == GameState.Playing && Time.timeScale == 1f);

        // each rule
        foreach (var id in new[] { "never_stop", "red_is_dead", "controls_reversed", "keep_jumping", "floor_is_lava", "gravity_flip" })
        {
            gm.StartRun();
            gm.GoToMainMenu(); gm.StartRun();
            yield return new WaitForSeconds(0.5f);
            bool started = gm.rules.DebugStart(id);
            Check(id + " queued", started);
            yield return new WaitForSeconds(0.8f);
            yield return Shot("4_warn_" + id);
            yield return new WaitForSeconds(2.6f);
            Check(id + " active", gm.rules.IsRuleActive(id));
            yield return Shot("5_active_" + id);
            int hp0 = gm.health.Current;

            switch (id)
            {
                case "never_stop":
                    yield return new WaitForSeconds(1.6f);
                    Check("never_stop damages idle player once", gm.health.Current == hp0 - 1, "hp " + hp0 + "->" + gm.health.Current);
                    break;
                case "red_is_dead":
                    {
                        var tile = gm.arena.hazardTiles[0];
                        Check("red tiles active", tile.State == HazardState.Active);
                        gm.player.Body.position = tile.transform.position + Vector3.up * 0.9f;
                        yield return new WaitForSeconds(1.0f);
                        Check("red tile damages player", gm.health.Current < hp0, "hp " + hp0 + "->" + gm.health.Current);
                        break;
                    }
                case "controls_reversed":
                    {
                        Check("input reversed", InputModifier.Reversed);
                        var q = pc.transform.position;
                        pc.debugMove = Vector2.up;
                        yield return new WaitForSeconds(0.5f);
                        pc.debugMove = Vector2.zero;
                        float dot = Vector3.Dot(pc.transform.position - q, gm.cameraRig.FlatForward);
                        Check("W moves backwards under reversed controls", dot < -0.5f, dot.ToString("F2"));
                        break;
                    }
                case "keep_jumping":
                    yield return new WaitForSeconds(1.6f);
                    Check("keep_jumping damages grounded player", gm.health.Current < hp0, "hp " + hp0 + "->" + gm.health.Current);
                    break;
                case "floor_is_lava":
                    {
                        Check("lava overlay shown", gm.arena.lava.State == HazardState.Active);
                        // stand on the open floor
                        gm.player.Body.position = new Vector3(-1.5f, 0.9f, 10.5f);
                        yield return new WaitForSeconds(1.6f);
                        Check("lava damages on floor", gm.health.Current < hp0, "hp " + hp0 + "->" + gm.health.Current);
                        break;
                    }
                case "gravity_flip":
                    {
                        float deadline = Time.time + 8f;
                        while (!gm.gravity.Inverted && Time.time < deadline) yield return null;
                        Check("gravity inverts", gm.gravity.Inverted);
                        yield return new WaitForSeconds(1.2f);
                        Check("player reaches ceiling", pc.transform.position.y > 4.5f && pc.IsGrounded, "y=" + pc.transform.position.y.ToString("F2") + " grounded=" + pc.IsGrounded);
                        yield return Shot("6_ceiling");
                        break;
                    }
            }

            // cleanup check
            gm.rules.ResetAll();
            yield return new WaitForSeconds(0.2f);
            Check(id + " cleaned up", !gm.gravity.Inverted && !InputModifier.Reversed && gm.arena.hazardTiles[0].State == HazardState.Dormant && gm.arena.lava.State == HazardState.Dormant && StatusBus.Keys.Count == 0);
        }

        // combinations: only allow-listed pairs may overlap; incompatible / unlisted ones never do
        gm.GoToMainMenu(); gm.StartRun();
        yield return new WaitForSeconds(0.3f);
        gm.rules.DebugStart("gravity_flip");
        yield return new WaitForSeconds(2.8f);
        Check("gravity flip blocks lava and reversed controls", !gm.rules.DebugStart("floor_is_lava") && !gm.rules.DebugStart("controls_reversed"));
        Check("gravity flip + keep jumping allowed", gm.rules.DebugStart("keep_jumping"));
        gm.GoToMainMenu(); gm.StartRun();
        yield return new WaitForSeconds(0.3f);
        gm.rules.DebugStart("never_stop");
        yield return new WaitForSeconds(2.8f);
        Check("never_stop + keep_jumping blocked (not allow-listed)", !gm.rules.DebugStart("keep_jumping"));
        Check("never_stop + red_is_dead allowed", gm.rules.DebugStart("red_is_dead"));
        yield return new WaitForSeconds(3f);
        Check("two rules active together", gm.rules.IsRuleActive("never_stop") && gm.rules.IsRuleActive("red_is_dead"));
        yield return Shot("7_overlap");

        // intensity: starts calm, rises smoothly, freezes while paused, resets on restart
        gm.GoToMainMenu(); gm.StartRun();
        yield return new WaitForSeconds(0.5f);
        float i0 = gm.intensity.Value;
        Check("intensity starts low", i0 >= 0.09f && i0 <= 0.2f, i0.ToString("F2"));
        yield return new WaitForSeconds(4f);
        float i1 = gm.intensity.Value;
        Check("intensity rises with time", i1 > i0, i0.ToString("F3") + " -> " + i1.ToString("F3"));
        gm.Pause();
        float frozenI = gm.intensity.Value;
        yield return new WaitForSecondsRealtime(1f);
        Check("intensity frozen while paused", Mathf.Approximately(frozenI, gm.intensity.Value));
        gm.Resume();
        gm.StartRun(); // no-op while playing
        gm.GoToMainMenu(); gm.StartRun();
        yield return new WaitForSeconds(0.3f);
        Check("restart resets intensity", gm.intensity.Value < i1 && gm.intensity.Peak < 0.15f, gm.intensity.Value.ToString("F3"));
        Check("curve: 90s is around 0.85", Mathf.Abs(gm.intensity.Evaluate(90f) - 0.85f) < 0.02f && Mathf.Abs(gm.intensity.Evaluate(15f) - 0.2f) < 0.02f);

        // ---- RULE MUTATION: trigger exactly at 150s of current-run survival time
        var mm = gm.mutations;
        gm.GoToMainMenu(); gm.StartRun();
        yield return new WaitForSeconds(0.3f);
        Check("fresh run is NORMAL_SURVIVAL", mm.Phase == MutationPhase.NormalSurvival && !mm.RealityBreakTriggered);
        gm.DebugSetSurvivalTime(148.6f);
        gm.Pause();
        yield return new WaitForSecondsRealtime(1f);
        float pausedTime = gm.SurvivalTime;
        yield return new WaitForSecondsRealtime(0.5f);
        Check("paused: survival time frozen, no mutation", Mathf.Approximately(pausedTime, gm.SurvivalTime) && mm.Phase == MutationPhase.NormalSurvival);
        gm.Resume();
        yield return new WaitForSeconds(0.5f); // ~149.1s
        Check("149s: mutations not active", gm.SurvivalTime < 150f && mm.Phase == MutationPhase.NormalSurvival && mm.ActiveMutation == null);
        float mDeadline = Time.time + 3f;
        while (!mm.RealityBreakTriggered && Time.time < mDeadline) yield return null;
        Check("150s: Reality Break begins", mm.RealityBreakTriggered && gm.SurvivalTime >= 150f && mm.Phase != MutationPhase.NormalSurvival, "t=" + gm.SurvivalTime.ToString("F2"));
        float tAtBreak = gm.SurvivalTime;
        yield return Shot("10_realitybreak");
        mDeadline = Time.time + 25f;
        while (mm.Phase == MutationPhase.RealityBreakWarning && Time.time < mDeadline) yield return null;
        Check("Reality Break completes, phase RULE_MUTATION", mm.Phase != MutationPhase.RealityBreakWarning && mm.Phase != MutationPhase.NormalSurvival);
        Check("survival timer not reset by the transition", gm.SurvivalTime >= tAtBreak);
        Check("normal rule scheduling resumed", !gm.rules.SchedulingPaused);

        foreach (var mid in new[] { MutationId.ShiftingBlue, MutationId.ChargingEnergy, MutationId.CursedCoins, MutationId.LandingShockwave })
        {
            mm.AbortAll();
            gm.rules.ResetAll();
            yield return new WaitForSeconds(0.3f);
            bool queued = mm.DebugStartMutation(mid);
            Check(mid + ": warning starts", queued);
            if (!queued) continue;
            yield return Shot("11_warn_" + mid);
            mDeadline = Time.time + 8f;
            while (mm.ActiveMutation == null && Time.time < mDeadline) yield return null;
            Check(mid + ": activates after its warning", mm.ActiveMutation != null && mm.ActiveMutation.id == mid);
            if (mm.ActiveMutation != null && !string.IsNullOrEmpty(mm.ActiveMutation.baseRuleId))
                Check(mid + ": base rule still active", gm.rules.IsRuleActive(mm.ActiveMutation.baseRuleId));
            yield return new WaitForSeconds(1.5f);
            yield return Shot("12_active_" + mid);
            mm.AbortAll();
            Check(mid + ": cleans up", mm.ActiveMutation == null || !mm.ActiveMutation.IsActive);
        }
        // restart: back to a fresh normal run with a fresh threshold
        gm.StartRun();
        yield return new WaitForSeconds(0.3f);
        Check("restart: NORMAL_SURVIVAL, flag cleared, timer fresh", mm.Phase == MutationPhase.NormalSurvival && !mm.RealityBreakTriggered && gm.SurvivalTime < 2f);
        // death before 150s never starts the mutation phase
        gm.health.TakeDamage(1, "test"); yield return new WaitForSeconds(1.1f);
        gm.health.TakeDamage(1, "test"); yield return new WaitForSeconds(1.1f);
        gm.health.TakeDamage(1, "test"); yield return new WaitForSeconds(0.3f);
        Check("game over before 150s: mutation phase never started", gm.State == GameState.GameOver && !mm.RealityBreakTriggered);
        gm.StartRun();
        yield return new WaitForSeconds(0.3f);

        // game over, stats, retry
        gm.GoToMainMenu(); gm.StartRun();
        yield return new WaitForSeconds(2.2f);
        gm.AddCoin();
        for (int i = 0; i < 3; i++) { gm.health.TakeDamage(1, "test"); yield return new WaitForSeconds(1.1f); }
        Check("health zero -> GameOver once", gm.State == GameState.GameOver && gm.health.IsDead);
        float frozen = gm.SurvivalTime;
        yield return new WaitForSeconds(2.0f);
        Check("stats frozen after death", Mathf.Approximately(frozen, gm.SurvivalTime));
        Check("best score saved", PlayerPrefs.GetInt(GameManager.BestScoreKey) == gm.Score && gm.NewBest, "score " + gm.Score);
        yield return Shot("8_gameover");
        gm.StartRun();
        yield return new WaitForSeconds(0.8f);
        Check("retry gives clean run", gm.State == GameState.Playing && gm.health.Current == gm.health.maxHealth && gm.Coins == 0 && gm.SurvivalTime < 1.5f && pc.IsGrounded);
        yield return Shot("9_retry");

        Debug.Log("[AUTOTEST] DONE passes=" + passes + " fails=" + fails);
        EditorApplication.isPlaying = false;
    }
}
#endif
