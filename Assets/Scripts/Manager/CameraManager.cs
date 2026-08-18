using System.Collections;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    public MapManager MapManager;
    public GameObject CameraObj;
    public float CameraSpeed;
    public float MaxCameraSpeed = 5f;
    public float DifficultyRampDuration = 90f;
    public float DefaultShakeDuration = 0.15f;
    public float DefaultShakeMagnitude = 0.15f;
    private bool _isValid;
    private float _elapsed = 0f;
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

    public bool TryGetPosition(out Vector3 position)
    {
        if (!_isValid || CameraObj == null)
        {
            position = default;
            return false;
        }

        position = CameraObj.transform.position;
        return true;
    }

    public bool TryGetSpawnPosition(float aheadDistance, float y, out Vector3 position)
    {
        if (!TryGetPosition(out var cameraPosition))
        {
            position = default;
            return false;
        }

        position = new Vector3(cameraPosition.x + aheadDistance, y, 0f);
        return true;
    }

    void Update()
    {
        if (!_isValid)
            return;

        _elapsed += Time.deltaTime;
        float rampDuration = Mathf.Max(0.01f, DifficultyRampDuration);
        float currentSpeed = Mathf.Lerp(_baseSpeed, MaxCameraSpeed, Mathf.Clamp01(_elapsed / rampDuration));
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

