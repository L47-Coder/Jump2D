using System.Collections;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    public MapManager MapManager;
    public GameObject CameraObj;
    public float CameraSpeed;
    public float MaxCameraSpeed = 5f;
    public float SpawnAheadDistance = 10f;
    public float DefaultShakeDuration = 0.15f;
    public float DefaultShakeMagnitude = 0.15f;
    private bool _isValid;
    private float _baseSpeed;
    private Vector3 _basePosition;
    private Vector3 _shakeOffset = Vector3.zero;
    private Coroutine _shakeRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        if (CameraObj == null)
            Debug.LogError("CameraObj is null in CameraManager");
        if (MapManager == null)
            Debug.LogError("MapManager is null in CameraManager");

        _baseSpeed = CameraSpeed;
        if (CameraObj == null || MapManager == null)
        {
            enabled = false;
            return;
        }

        _basePosition = CameraObj.transform.position;
        _isValid = true;
    }

    private bool TryGetWorldAnchorPosition(out Vector3 position)
    {
        if (!_isValid || CameraObj == null)
        {
            position = default;
            return false;
        }

        position = _basePosition;
        return true;
    }

    public bool TryGetSpawnPosition(float y, out Vector3 position)
    {
        if (!TryGetWorldAnchorPosition(out var worldAnchorPosition))
        {
            position = default;
            return false;
        }

        position = new Vector3(worldAnchorPosition.x + SpawnAheadDistance, y, 0f);
        return true;
    }

    void Update()
    {
        if (!_isValid)
            return;

        float difficultyT = GameManager.GetDifficultyProgressOrDefault();
        float currentSpeed = Mathf.Lerp(_baseSpeed, MaxCameraSpeed, difficultyT);
        _basePosition += currentSpeed * Time.deltaTime * Vector3.right;
        CameraObj.transform.position = _basePosition + _shakeOffset;

        if (_basePosition.x >= MapManager.BackgroundLength)
            MapManager.CreateBackground();
    }

    // 命中反馈用的小幅屏幕震动，使用非缩放时间以便在暂停/结算瞬间也能播放
    public void Shake(float duration = -1f, float magnitude = -1f)
    {
        if (duration < 0f)
            duration = DefaultShakeDuration;
        if (magnitude < 0f)
            magnitude = DefaultShakeMagnitude;
        if (!_isValid || duration <= 0f || magnitude <= 0f)
            return;

        if (_shakeRoutine != null)
            StopCoroutine(_shakeRoutine);
        _shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _shakeOffset = Random.insideUnitCircle * magnitude;
            yield return null;
        }
        _shakeOffset = Vector3.zero;
        _shakeRoutine = null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
