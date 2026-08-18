using UnityEngine;
using UnityEngine.UI;

// 监听分数变化刷新 UI，并做一次数值弹跳反馈
public class ScoreHUD : GameManagerBinding
{
    public Text ScoreText;
    public float ScorePunchScale = 1.3f;
    public float ScorePunchDuration = 0.2f;
    private Vector3 _restScale = Vector3.one;

    void Awake()
    {
        if (ScoreText == null)
            ScoreText = GetComponentInChildren<Text>(true);

        if (ScoreText != null)
            _restScale = ScoreText.transform.localScale;
    }

    private void HandleScoreChanged(int score)
    {
        if (ScoreText == null)
            return;

        ScoreText.text = score.ToString();
        Tween.Punch(this, ScoreText.transform, _restScale, ScorePunchScale, ScorePunchDuration);
    }

    protected override void Subscribe(GameManager manager)
    {
        manager.OnScoreChanged += HandleScoreChanged;
    }

    protected override void Unsubscribe(GameManager manager)
    {
        manager.OnScoreChanged -= HandleScoreChanged;
    }

    protected override void OnManagerBound(GameManager manager)
    {
        HandleScoreChanged(manager.Score);
    }
}
