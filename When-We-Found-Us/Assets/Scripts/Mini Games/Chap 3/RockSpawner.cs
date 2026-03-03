using UnityEngine;
using System.Collections.Generic;

public class RockSpawner : MonoBehaviour
{
    [Header("Rock Prefabs (assign all 4 sizes)")]
    public GameObject[] rockPrefabs;

    [Header("Spawn Region (World X)")]
    public float spawnStartX = 8f;
    public float spawnEndX   = 45f;

    [Header("Rock Count")]
    public int rockCount = 12;

    [Header("Spacing")]
    public float minSpacingX = 3f;

    public int maxPlacementAttempts = 50;

    private readonly List<float> spawnedXPositions = new List<float>();

    void Start()
    {
        SpawnAllRocks();
    }

    // Instantiates random rock prefabs at valid positions
    private void SpawnAllRocks()
    {
        spawnedXPositions.Clear();
        int spawned = 0;

        for (int i = 0; i < rockCount; i++)
        {
            float chosenX = TryGetValidX(maxPlacementAttempts);

            if (float.IsNaN(chosenX))
            {             
                continue;
            }

            GameObject prefab = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
            Vector3 pos = new Vector3(chosenX, prefab.transform.position.y, 0f);

            Instantiate(prefab, pos, Quaternion.identity, this.transform);
            spawnedXPositions.Add(chosenX);
            spawned++;
        }
    }

    // Attempts to find a non-overlapping X coordinate
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

    // Checks if the candidate position is far enough from existing rocks
    private bool IsPositionValid(float x)
    {
        foreach (float existing in spawnedXPositions)
        {
            if (Mathf.Abs(x - existing) < minSpacingX)
                return false;
        }
        return true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        float height = 4f;
        float width  = spawnEndX - spawnStartX;
        Gizmos.DrawCube(new Vector3((spawnStartX + spawnEndX) * 0.5f, 0f, 0f),
                        new Vector3(width, height, 0.1f));

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(spawnStartX, -height * 0.5f, 0), new Vector3(spawnStartX, height * 0.5f, 0));
        Gizmos.DrawLine(new Vector3(spawnEndX,   -height * 0.5f, 0), new Vector3(spawnEndX,   height * 0.5f, 0));
    }
}
