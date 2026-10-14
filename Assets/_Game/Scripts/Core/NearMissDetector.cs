using UnityEngine;

/// <summary>
/// Awards a small bonus when the player grazes an active hazard tile without being hurt. Each approach is a unique event
/// (per tile, with a global cooldown), so lingering next to a hazard can never farm points.
/// </summary>
public class NearMissDetector : MonoBehaviour
{
    public int bonusPoints = 15;
    [Tooltip("Distance outside the tile footprint that counts as 'close'.")]
    public float nearRange = 0.7f;
    public float minExposure = 0.15f;
    public float cooldown = 1.5f;

    const float Footprint = 1.7f; // same footprint the hazard rules use (tile half size + player radius)

    float[] exposure;
    int[] damageAtEnter;
    bool[] inZone;
    float lastAward = -99f;

    public int Count { get; private set; }

    void Start()
    {
        int n = GameManager.Instance.arena.hazardTiles.Length;
        exposure = new float[n]; damageAtEnter = new int[n]; inZone = new bool[n];
    }

    public void ResetRun()
    {
        Count = 0; lastAward = -99f;
        if (exposure == null) return;
        for (int i = 0; i < exposure.Length; i++) { exposure[i] = 0f; inZone[i] = false; }
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing || exposure == null) return;
        var tiles = gm.arena.hazardTiles;
        var p = gm.player.transform.position;
        float dt = Time.deltaTime;

        for (int i = 0; i < tiles.Length; i++)
        {
            var tile = tiles[i];
            if (tile.State != HazardState.Active || p.y > 2.4f) { inZone[i] = false; exposure[i] = 0f; continue; }
            var q = tile.transform.position;
            float cheb = Mathf.Max(Mathf.Abs(p.x - q.x), Mathf.Abs(p.z - q.z));

            if (cheb < Footprint) { inZone[i] = false; exposure[i] = 0f; continue; }          // stepped on it: no near miss
            if (cheb < Footprint + nearRange)
            {
                if (!inZone[i]) { inZone[i] = true; exposure[i] = 0f; damageAtEnter[i] = gm.health.DamageEvents; }
                exposure[i] += dt;
            }
            else if (inZone[i] && cheb > Footprint + nearRange + 0.25f)                          // left the close zone safely
            {
                inZone[i] = false;
                if (exposure[i] >= minExposure && gm.health.DamageEvents == damageAtEnter[i] && Time.time - lastAward >= cooldown)
                {
                    lastAward = Time.time;
                    Count++;
                    gm.AddBonus("NEAR MISS!", bonusPoints);
                    AudioManager.Play(Sfx.NearMiss);
                }
                exposure[i] = 0f;
            }
        }
    }
}
