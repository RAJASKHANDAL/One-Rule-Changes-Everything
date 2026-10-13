using System.Collections.Generic;
using UnityEngine;

/// <summary>Owns the coin pool. Collected coins respawn after a delay at a free, pre-validated spawn point.</summary>
public class CoinSpawner : MonoBehaviour
{
    public Coin[] coins;
    public ParticleSystem burst;
    public float respawnDelay = 9f;
    public float minDistanceFromPlayer = 2f;

    Vector3[] spawnPoints;
    readonly List<(Coin coin, float time)> pending = new List<(Coin, float)>();

    void Awake()
    {
        spawnPoints = new Vector3[coins.Length];
        for (int i = 0; i < coins.Length; i++)
        {
            spawnPoints[i] = coins[i].transform.position;
            coins[i].HomePosition = spawnPoints[i];
            coins[i].spawner = this;
        }
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            if (Time.time < pending[i].time) continue;
            if (TryRespawn(pending[i].coin)) pending.RemoveAt(i);
            else pending[i] = (pending[i].coin, Time.time + 1f);
        }
    }

    public void OnCollected(Coin coin)
    {
        if (burst != null)
        {
            burst.transform.position = coin.transform.position;
            burst.Emit(14);
        }
        coin.gameObject.SetActive(false);
        pending.Add((coin, Time.time + respawnDelay));
    }

    bool TryRespawn(Coin coin)
    {
        var player = GameManager.Instance.player.transform.position;
        int start = Random.Range(0, spawnPoints.Length);
        for (int n = 0; n < spawnPoints.Length; n++)
        {
            var point = spawnPoints[(start + n) % spawnPoints.Length];
            if ((point - player).sqrMagnitude < minDistanceFromPlayer * minDistanceFromPlayer) continue;
            bool occupied = false;
            foreach (var other in coins)
                if (other.gameObject.activeSelf && (other.HomePosition - point).sqrMagnitude < 0.25f) { occupied = true; break; }
            if (occupied) continue;
            coin.HomePosition = point;
            coin.transform.position = point;
            coin.gameObject.SetActive(true);
            return true;
        }
        return false;
    }

    public void ResetCoins()
    {
        pending.Clear();
        for (int i = 0; i < coins.Length; i++)
        {
            coins[i].HomePosition = spawnPoints[i];
            coins[i].transform.position = spawnPoints[i];
            coins[i].gameObject.SetActive(true);
        }
    }
}
