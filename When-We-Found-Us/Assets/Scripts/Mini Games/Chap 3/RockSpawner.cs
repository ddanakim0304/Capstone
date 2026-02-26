using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns rock obstacles randomly along the road at game start.
///
/// SETUP
/// ─────
/// • Assign 1-4 rock prefabs (one per size) to the rockPrefabs array.
/// • Each prefab should have a Collider2D (or Collider) so the car collides with it.
/// • Set spawnStartX / spawnEndX to the X range where rocks should appear.
///   (Leave a gap at the very start so the car isn't immediately blocked.)
/// • Adjust minSpacingX so rocks are never too close together.
/// • RockCount controls total rocks spawned.
/// </summary>
public class RockSpawner : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Rock Prefabs (assign all 4 sizes)")]
    [Tooltip("Drag your rock prefabs here (small → large). At least one required.")]
    public GameObject[] rockPrefabs;

    [Header("Spawn Region (World X)")]
    [Tooltip("Leftmost X where rocks can appear. Recommend leaving a clear gap from the car start.")]
    public float spawnStartX = 8f;
    [Tooltip("Rightmost X where rocks can appear. Should be less than CarMiniGameManager.endPositionX.")]
    public float spawnEndX   = 45f;

    [Header("Rock Count")]
    [Tooltip("Total number of rocks to spawn across the whole road.")]
    public int rockCount = 12;

    [Header("Spacing")]
    [Tooltip("Minimum horizontal distance between any two rocks (prevents impossible clusters).")]
    public float minSpacingX = 3f;

    [Tooltip("Maximum attempts to find a valid position before giving up on a rock.")]
    public int maxPlacementAttempts = 50;

    // ── Private ───────────────────────────────────────────────────────────────
    private readonly List<float> spawnedXPositions = new List<float>();

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (rockPrefabs == null || rockPrefabs.Length == 0)
        {
            Debug.LogError("[RockSpawner] No rock prefabs assigned!");
            return;
        }

        SpawnAllRocks();
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void SpawnAllRocks()
    {
        spawnedXPositions.Clear();
        int spawned = 0;

        for (int i = 0; i < rockCount; i++)
        {
            float chosenX = TryGetValidX(maxPlacementAttempts);

            if (float.IsNaN(chosenX))
            {
                Debug.LogWarning($"[RockSpawner] Could not place rock {i + 1} after {maxPlacementAttempts} attempts – skipping.");
                continue;
            }

            // Pick a random rock prefab
            GameObject prefab = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
            // Use the prefab's own Y so each rock sits exactly where it was positioned in the prefab
            Vector3 pos = new Vector3(chosenX, prefab.transform.position.y, 0f);

            Instantiate(prefab, pos, Quaternion.identity, this.transform);
            spawnedXPositions.Add(chosenX);
            spawned++;
        }

        Debug.Log($"[RockSpawner] Spawned {spawned} / {rockCount} rocks.");
    }

    /// <summary>
    /// Attempts to find an X position within [spawnStartX, spawnEndX] that
    /// respects the minimum spacing from all already-placed rocks.
    /// Returns NaN if no valid position is found within the attempt limit.
    /// </summary>
    private float TryGetValidX(int maxAttempts)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float candidateX = Random.Range(spawnStartX, spawnEndX);

            if (IsPositionValid(candidateX))
                return candidateX;
        }
        return float.NaN;
    }

    private bool IsPositionValid(float x)
    {
        foreach (float existing in spawnedXPositions)
        {
            if (Mathf.Abs(x - existing) < minSpacingX)
                return false;
        }
        return true;
    }

    // ── Editor Gizmos ─────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        // Spawn region (yellow band) – Y drawn at 0 since each prefab carries its own Y
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        float height = 4f;
        float width  = spawnEndX - spawnStartX;
        Gizmos.DrawCube(new Vector3((spawnStartX + spawnEndX) * 0.5f, 0f, 0f),
                        new Vector3(width, height, 0.1f));

        // Borders
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(spawnStartX, -height * 0.5f, 0), new Vector3(spawnStartX, height * 0.5f, 0));
        Gizmos.DrawLine(new Vector3(spawnEndX,   -height * 0.5f, 0), new Vector3(spawnEndX,   height * 0.5f, 0));
    }
}
