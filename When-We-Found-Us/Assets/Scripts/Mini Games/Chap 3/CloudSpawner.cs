using System.Collections.Generic;
using UnityEngine;

public class CloudSpawner : MonoBehaviour
{
    [Header("Cloud Sprites")]
    public List<Sprite> cloudSprites = new List<Sprite>();

    [Header("X Spacing Between Clouds")]
    public float minSpacingX = 4f;
    public float maxSpacingX = 10f;

    [Header("Y Range")]
    public float minY = 2f;
    public float maxY = 5f;

    [Header("Depth")]
    public float cloudZ = 1f;
    public string sortingLayerName = "Background";
    public int sortingOrder = -30;

    [Header("Scale")]
    public float minScaleX = 0.8f;
    public float maxScaleX = 2.0f;

    public float minScaleY = 0.6f;
    public float maxScaleY = 1.2f;

    [Header("Spawn / Despawn")]
    public float spawnAheadX = 20f;
    public float despawnBehindX = 10f;

    private Camera _cam;
    private float  _nextSpawnX;
    private readonly List<GameObject> _active = new List<GameObject>();

    // Initialize camera reference and pre-spawn clouds to fill screen
    void Start()
    {
        _cam = Camera.main;
        if (_cam == null)
            _cam = FindFirstObjectByType<Camera>();

        _nextSpawnX = _cam.transform.position.x + spawnAheadX * 0.5f;

        while (_nextSpawnX < _cam.transform.position.x + spawnAheadX)
            SpawnCloud();
    }

    // Continuously spawn new clouds ahead and remove old ones
    void Update()
    {
        if (_cam == null || cloudSprites == null || cloudSprites.Count == 0) return;

        float camX = _cam.transform.position.x;

        while (_nextSpawnX < camX + spawnAheadX)
            SpawnCloud();

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

    private void SpawnCloud()
    {
        Sprite sprite = cloudSprites[Random.Range(0, cloudSprites.Count)];

        float x      = _nextSpawnX;
        float y      = Random.Range(minY, maxY);
        float scaleX = Random.Range(minScaleX, maxScaleX);
        float scaleY = Random.Range(minScaleY, maxScaleY);

        _nextSpawnX += Random.Range(minSpacingX, maxSpacingX);

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
    private void OnDrawGizmosSelected()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        float camX   = _cam.transform.position.x;
        float midY   = (minY + maxY) / 2f;
        float height = Mathf.Max(0.1f, maxY - minY);

        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawCube(
            new Vector3(camX + spawnAheadX - 1f, midY, cloudZ),
            new Vector3(2f, height, 0.1f));

        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawCube(
            new Vector3(camX - despawnBehindX + 1f, midY, cloudZ),
            new Vector3(2f, height, 0.1f));

        Gizmos.color = new Color(0f, 0.5f, 1f, 0.12f);
        Gizmos.DrawCube(
            new Vector3(camX, midY, cloudZ),
            new Vector3(spawnAheadX + despawnBehindX, height, 0.1f));
    }
#endif
}
