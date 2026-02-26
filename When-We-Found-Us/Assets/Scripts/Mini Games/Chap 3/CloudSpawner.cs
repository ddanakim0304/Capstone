using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns cloud sprites randomly ahead of the camera as it scrolls,
/// and removes them once they pass behind. All spacing / range values
/// are configurable from the Inspector.
/// </summary>
public class CloudSpawner : MonoBehaviour
{
    // ── Sprites ──────────────────────────────────────────────────────────────
    [Header("Cloud Sprites")]
    [Tooltip("Pool of sprites to randomly pick from when spawning a cloud.")]
    public List<Sprite> cloudSprites = new List<Sprite>();

    // ── Spacing ───────────────────────────────────────────────────────────────
    [Header("X Spacing Between Clouds")]
    [Tooltip("Minimum horizontal distance (world units) between consecutive clouds.")]
    public float minSpacingX = 4f;
    [Tooltip("Maximum horizontal distance (world units) between consecutive clouds.")]
    public float maxSpacingX = 10f;

    // ── Vertical range ────────────────────────────────────────────────────────
    [Header("Y Range")]
    [Tooltip("Minimum Y (world units) for spawned clouds.")]
    public float minY = 2f;
    [Tooltip("Maximum Y (world units) for spawned clouds.")]
    public float maxY = 5f;

    // ── Depth / layer ─────────────────────────────────────────────────────────
    [Header("Depth")]
    [Tooltip("Z position of spawned clouds (negative = behind everything in 2-D).")]
    public float cloudZ = 1f;
    [Tooltip("Sorting layer name for cloud sprites.")]
    public string sortingLayerName = "Background";
    [Tooltip("Order in the sorting layer.")]
    public int sortingOrder = 0;

    // ── Scale ─────────────────────────────────────────────────────────────────
    [Header("Scale")]
    [Tooltip("Minimum X scale (width) applied to each cloud.")]
    public float minScaleX = 0.8f;
    [Tooltip("Maximum X scale (width) applied to each cloud.")]
    public float maxScaleX = 2.0f;

    [Tooltip("Minimum Y scale (height) applied to each cloud.")]
    public float minScaleY = 0.6f;
    [Tooltip("Maximum Y scale (height) applied to each cloud.")]
    public float maxScaleY = 1.2f;

    // ── Spawn / despawn distances ─────────────────────────────────────────────
    [Header("Spawn / Despawn")]
    [Tooltip("How far ahead of the camera (in X) to spawn the next cloud.")]
    public float spawnAheadX = 20f;
    [Tooltip("How far behind the camera (in X) a cloud must be before it is destroyed.")]
    public float despawnBehindX = 10f;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private Camera _cam;
    private float  _nextSpawnX;          // world X at which the next cloud should appear
    private readonly List<GameObject> _active = new List<GameObject>();

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        _cam = Camera.main;
        if (_cam == null)
            _cam = FindObjectOfType<Camera>();

        // Seed the first spawn position just ahead of the initial camera view
        _nextSpawnX = _cam.transform.position.x + spawnAheadX * 0.5f;

        // Pre-fill the visible area so there are clouds from the very first frame
        while (_nextSpawnX < _cam.transform.position.x + spawnAheadX)
            SpawnCloud();
    }

    // ─────────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (_cam == null || cloudSprites == null || cloudSprites.Count == 0) return;

        float camX = _cam.transform.position.x;

        // Spawn new clouds ahead of the camera
        while (_nextSpawnX < camX + spawnAheadX)
            SpawnCloud();

        // Remove clouds that have scrolled far behind the camera
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (_active[i] == null)
            {
                _active.RemoveAt(i);
                continue;
            }
            if (_active[i].transform.position.x < camX - despawnBehindX)
            {
                Destroy(_active[i]);
                _active.RemoveAt(i);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void SpawnCloud()
    {
        // Pick a random sprite from the list
        Sprite sprite = cloudSprites[Random.Range(0, cloudSprites.Count)];

        // Build position
        float x      = _nextSpawnX;
        float y      = Random.Range(minY, maxY);
        float scaleX = Random.Range(minScaleX, maxScaleX);
        float scaleY = Random.Range(minScaleY, maxScaleY);

        // Advance the next-spawn cursor
        _nextSpawnX += Random.Range(minSpacingX, maxSpacingX);

        // Create the cloud object
        GameObject go = new GameObject("Cloud");
        go.transform.position   = new Vector3(x, y, cloudZ);
        go.transform.localScale = new Vector3(scaleX, scaleY, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite             = sprite;
        sr.sortingLayerName   = sortingLayerName;
        sr.sortingOrder       = sortingOrder;

        _active.Add(go);
    }

#if UNITY_EDITOR
    // Draw a simple gizmo rectangle showing the spawn / despawn window
    private void OnDrawGizmosSelected()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        float camX   = _cam.transform.position.x;
        float midY   = (minY + maxY) / 2f;
        float height = Mathf.Max(0.1f, maxY - minY);

        // Spawn zone (green)
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawCube(
            new Vector3(camX + spawnAheadX - 1f, midY, cloudZ),
            new Vector3(2f, height, 0.1f));

        // Despawn zone (red)
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawCube(
            new Vector3(camX - despawnBehindX + 1f, midY, cloudZ),
            new Vector3(2f, height, 0.1f));

        // Y range band (blue)
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.12f);
        Gizmos.DrawCube(
            new Vector3(camX, midY, cloudZ),
            new Vector3(spawnAheadX + despawnBehindX, height, 0.1f));
    }
#endif
}
