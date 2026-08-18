using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemySpawnConfig
{
    public GameObject Prefab;
}

public class EnemyManager : MonoBehaviour
{
    public MapManager MapManager;
    public List<EnemySpawnConfig> EnemyConfigs;
    public float GenerateInterval = 1.4f;
    public float MinGenerateInterval = 0.55f;
    public float DifficultyRampDuration = 90f;
    public float BatchSpacing = 1.35f;
    private float _lastGenerateTime = 0f;
    private float _elapsed = 0f;
    private bool _isValid;

    void Awake()
    {
        if (MapManager == null)
            MapManager = FindObjectOfType<MapManager>();

        _isValid = EnemyConfigs != null && EnemyConfigs.Exists(config => config != null && config.Prefab != null);
        if (!_isValid)
            Debug.LogError("EnemyManager requires at least one valid EnemySpawnConfig.", this);
    }

    void Update()
    {
        if (!_isValid)
            return;

        _elapsed += Time.deltaTime;
        float rampDuration = Mathf.Max(0.01f, DifficultyRampDuration);
        float initialInterval = Mathf.Max(0.01f, GenerateInterval);
        float minimumInterval = Mathf.Max(0.01f, MinGenerateInterval);
        float currentInterval = Mathf.Max(0.01f, Mathf.Lerp(initialInterval, minimumInterval, Mathf.Clamp01(_elapsed / rampDuration)));

        if (Time.time - _lastGenerateTime >= currentInterval)
        {
            GenerateEnemyBatch();
            _lastGenerateTime = Time.time;
        }
    }

    private void GenerateEnemyBatch()
    {
        // 随难度推进，单次生成的敌人数量上限逐步提高，保证开局也有连续目标。
        var camera = Camera.main;
        if (camera == null)
            return;

        var validConfigs = EnemyConfigs.FindAll(config => config != null && config.Prefab != null);
        if (validConfigs.Count == 0)
            return;

        float difficultyT = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, DifficultyRampDuration));
        int maxBatch = 2 + Mathf.FloorToInt(difficultyT * 3f);
        int count = Random.Range(2, maxBatch + 1);

        float baseX = camera.transform.position.x + 10f;
        for (int i = 0; i < count; i++)
        {
            // 高度由预制体内部的 BodyRoot/Shadow 本地位置决定，所有敌人统一从 y=0 生成。
            EnemySpawnConfig config = validConfigs[Random.Range(0, validConfigs.Count)];
            Vector3 spawnPosition = new Vector3(baseX + i * BatchSpacing, 0f, 0f);
            Instantiate(config.Prefab, spawnPosition, Quaternion.identity);
        }
    }
}


