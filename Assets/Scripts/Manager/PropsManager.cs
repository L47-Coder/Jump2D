using UnityEngine;

// 随进度在场景前方生成武器道具，需要玩家跳跃拾取
public class PropsManager : MonoBehaviour
{
    public GameObject PropsPrefab;
    public float SpawnY = 1.2f;
    public float MinInterval = 8f;
    public float MaxInterval = 14f;
    public float DifficultyRampDuration = 90f;
    private float _elapsed;
    private float _nextSpawnTime;
    private bool _isValid;

    void Awake()
    {
        _isValid = PropsPrefab != null;
        if (!_isValid)
            Debug.LogError("PropsManager requires a PropsPrefab.", this);
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
        if (!_isValid)
            return;

        _elapsed += Time.deltaTime;
        if (Time.time >= _nextSpawnTime)
        {
            SpawnProps();
            ScheduleNext();
        }
    }

    private void ScheduleNext()
    {
        float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, DifficultyRampDuration));
        float minInterval = Mathf.Max(0.1f, Mathf.Min(MinInterval, MaxInterval));
        float maxInterval = Mathf.Max(minInterval, Mathf.Max(MinInterval, MaxInterval));
        float interval = Mathf.Lerp(maxInterval, minInterval, t);
        _nextSpawnTime = Time.time + Random.Range(interval * 0.8f, interval * 1.2f);
    }

    private void SpawnProps()
    {
        if (PropsPrefab == null || Camera.main == null)
            return;

        Vector3 spawnPosition = new Vector3(Camera.main.transform.position.x + 10f, SpawnY, 0);
        GameObject obj = Instantiate(PropsPrefab, spawnPosition, Quaternion.identity);
        var props = obj.GetComponent<Props>();
        if (props != null)
        {
            WeaponType type = Random.value < 0.5f ? WeaponType.MachineGun : WeaponType.Corn;
            props.SetWeaponType(type);
        }
    }
}
