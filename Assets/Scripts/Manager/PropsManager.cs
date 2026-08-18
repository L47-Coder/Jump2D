using UnityEngine;

// 随进度在场景前方生成武器道具，需要玩家跳跃拾取
public class PropsManager : MonoBehaviour
{
    public GameObject PropsPrefab;
    public float SpawnY = 1f;
    public float MinInterval = 8f;
    public float MaxInterval = 14f;
    public float IntervalRandomMinMultiplier = 0.8f;
    public float IntervalRandomMaxMultiplier = 1.2f;
    public float CornSpawnChance = 0.5f;
    private float _nextSpawnTime;
    private bool _isValid;

    private static bool IsPlayingState()
    {
        var manager = GameManager.Instance;
        return manager == null || manager.State == GameState.Playing;
    }

    void Awake()
    {
        _isValid = TryValidatePropsPrefab();
    }

    private bool TryValidatePropsPrefab()
    {
        if (PropsPrefab == null)
        {
            Debug.LogError("PropsManager requires a PropsPrefab.", this);
            return false;
        }

        if (PropsPrefab.GetComponent<Props>() == null)
        {
            Debug.LogError(
                $"PropsManager PropsPrefab '{PropsPrefab.name}' must contain a Props component on its root GameObject.",
                this);
            return false;
        }

        return true;
    }

    void Start()
    {
        if (!_isValid)
        {
            enabled = false;
            return;
        }

        ScheduleNext();
    }

    void Update()
    {
        if (!_isValid || !IsPlayingState())
            return;

        if (Time.time >= _nextSpawnTime)
        {
            SpawnProps();
            ScheduleNext();
        }
    }

    private void ScheduleNext()
    {
        float t = GameManager.GetDifficultyProgressOrDefault();
        float minInterval = Mathf.Max(0.1f, Mathf.Min(MinInterval, MaxInterval));
        float maxInterval = Mathf.Max(minInterval, Mathf.Max(MinInterval, MaxInterval));
        float interval = Mathf.Lerp(maxInterval, minInterval, t);
        float randomMin = Mathf.Min(IntervalRandomMinMultiplier, IntervalRandomMaxMultiplier);
        float randomMax = Mathf.Max(IntervalRandomMinMultiplier, IntervalRandomMaxMultiplier);
        _nextSpawnTime = Time.time + Random.Range(interval * randomMin, interval * randomMax);
    }

    private void SpawnProps()
    {
        var cameraManager = CameraManager.Instance;
        if (PropsPrefab == null || cameraManager == null ||
            !cameraManager.TryGetSpawnPosition(SpawnY, out var spawnPosition))
            return;

        GameObject obj = Instantiate(PropsPrefab, spawnPosition, Quaternion.identity);
        var props = obj.GetComponent<Props>();
        if (props == null)
        {
            Debug.LogError(
                $"PropsManager instantiated PropsPrefab '{PropsPrefab.name}' without a root Props component.",
                obj);
            Destroy(obj);
            return;
        }

        WeaponType type = Random.value < Mathf.Clamp01(CornSpawnChance)
            ? WeaponType.Corn
            : WeaponType.MachineGun;
        props.SetWeaponType(type);
    }
}
