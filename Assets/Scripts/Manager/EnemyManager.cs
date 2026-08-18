using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public List<GameObject> EnemyConfigs;
    public float GenerateInterval = 1.4f;
    public float MinGenerateInterval = 0.55f;
    public float BatchSpacing = 1.35f;
    public int MinimumBatchCount = 2;
    public int AdditionalBatchCountAtMaxDifficulty = 3;
    private float _lastGenerateTime = 0f;
    private readonly List<GameObject> _validEnemyPrefabs = new();
    private bool _isValid;

    void Awake()
    {
        _validEnemyPrefabs.Clear();
        if (EnemyConfigs != null)
        {
            foreach (var prefab in EnemyConfigs)
            {
                if (prefab != null)
                    _validEnemyPrefabs.Add(prefab);
            }
        }

        _isValid = _validEnemyPrefabs.Count > 0;
        if (!_isValid)
            Debug.LogError("EnemyManager requires at least one valid enemy prefab.", this);
    }

    void Update()
    {
        if (!_isValid)
            return;

        float difficultyT = GameManager.Instance != null
            ? GameManager.Instance.DifficultyProgress
            : 0f;
        float initialInterval = Mathf.Max(0.01f, GenerateInterval);
        float minimumInterval = Mathf.Max(0.01f, MinGenerateInterval);
        float currentInterval = Mathf.Max(0.01f, Mathf.Lerp(initialInterval, minimumInterval, difficultyT));

        if (Time.time - _lastGenerateTime >= currentInterval)
        {
            GenerateEnemyBatch();
            _lastGenerateTime = Time.time;
        }
    }

    private void GenerateEnemyBatch()
    {
        // 随难度推进，单次生成的敌人数量上限逐步提高，保证开局也有连续目标。
        var cameraManager = CameraManager.Instance;
        if (cameraManager == null || !cameraManager.TryGetSpawnPosition(0f, out var spawnAnchor))
            return;

        if (_validEnemyPrefabs.Count == 0)
            return;

        float difficultyT = GameManager.Instance != null
            ? GameManager.Instance.DifficultyProgress
            : 0f;
        int minimumBatchCount = Mathf.Max(1, MinimumBatchCount);
        int maxBatch = minimumBatchCount + Mathf.FloorToInt(
            difficultyT * Mathf.Max(0, AdditionalBatchCountAtMaxDifficulty));
        int count = Random.Range(minimumBatchCount, maxBatch + 1);

        for (int i = 0; i < count; i++)
        {
            // 高度由预制体内部的 BodyRoot/Shadow 本地位置决定，所有敌人统一从 y=0 生成。
            GameObject prefab = _validEnemyPrefabs[Random.Range(0, _validEnemyPrefabs.Count)];
            Vector3 spawnPosition = spawnAnchor + Vector3.right * (i * BatchSpacing);
            Instantiate(prefab, spawnPosition, Quaternion.identity);
        }
    }
}
