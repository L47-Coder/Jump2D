using UnityEngine;

// 将 Canvas 根节点限制在手机安全区内，避免刘海、圆角和系统手势区域遮挡按钮。
[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaLayout : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Rect _lastSafeArea;
    private int _lastScreenWidth;
    private int _lastScreenHeight;
    private ScreenOrientation _lastOrientation;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        ApplySafeArea(true);
    }

    private void OnEnable()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();

        ApplySafeArea(true);
    }

    private void Update()
    {
        ApplySafeArea(false);
    }

    private void ApplySafeArea(bool force)
    {
        if (_rectTransform == null || Screen.width <= 0 || Screen.height <= 0)
            return;

        Rect safeArea = Screen.safeArea;
        if (!force && safeArea == _lastSafeArea &&
            _lastScreenWidth == Screen.width &&
            _lastScreenHeight == Screen.height &&
            _lastOrientation == Screen.orientation)
            return;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        _rectTransform.anchorMin = anchorMin;
        _rectTransform.anchorMax = anchorMax;
        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;

        _lastSafeArea = safeArea;
        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
        _lastOrientation = Screen.orientation;
    }
}
