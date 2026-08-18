using UnityEngine;
using UnityEngine.UI;

// 监听分数变化刷新 UI，并做一次数值弹跳反馈
public class ScoreHUD : MonoBehaviour
{
    public Text ScoreText;
    private GameManager _subscribedManager;
    private Vector3 _restScale = Vector3.one;

    void OnEnable()
    {
        TrySubscribe();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void Awake()
    {
        if (ScoreText == null)
            ScoreText = GetComponentInChildren<Text>(true);

        if (ScoreText != null)
            _restScale = ScoreText.transform.localScale;
    }

    void Start()
    {
        TrySubscribe();
        if (ScoreText != null && _subscribedManager == null)
            ScoreText.text = "0";
    }

    private void HandleScoreChanged(int score)
    {
        if (ScoreText == null)
            return;

        ScoreText.text = score.ToString();
        Tween.Punch(this, ScoreText.transform, _restScale, 1.3f, 0.2f);
    }

    private void TrySubscribe()
    {
        var manager = GameManager.Instance;
        if (manager == null || _subscribedManager == manager)
            return;

        Unsubscribe();
        _subscribedManager = manager;
        _subscribedManager.OnScoreChanged += HandleScoreChanged;
        HandleScoreChanged(_subscribedManager.Score);
    }

    private void Unsubscribe()
    {
        if (_subscribedManager == null)
            return;

        _subscribedManager.OnScoreChanged -= HandleScoreChanged;
        _subscribedManager = null;
    }
}
