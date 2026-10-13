using UnityEngine;

public class Coin : MonoBehaviour
{
    public float spinSpeed = 140f;
    public float bobHeight = 0.12f;
    public float bobSpeed = 2.5f;

    [HideInInspector] public CoinSpawner spawner;
    public Vector3 HomePosition { get; set; }

    bool collected;
    float phase;

    void OnEnable() { collected = false; phase = Random.value * 6.28f; }

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        var p = HomePosition;
        p.y += Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight;
        transform.position = p;
    }

    void OnTriggerEnter(Collider other)
    {
        var gm = GameManager.Instance;
        if (collected || gm == null || gm.State != GameState.Playing || other.attachedRigidbody != gm.player.Body) return;
        collected = true;
        gm.AddCoin();
        AudioManager.Play(Sfx.Coin);
        if (spawner != null) spawner.OnCollected(this);
        else gameObject.SetActive(false);
    }
}
