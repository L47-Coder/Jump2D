using UnityEngine;
using UnityEngine.UI;

// 游戏结束面板：展示最终分数并处理重开按钮
public class GameOverPanel : GameManagerBinding
{
    public GameObject Panel;
    public Text FinalScoreText;

    public void OnRestartButtonClicked() => Manager?.Restart();

    private void HandleGameOver()
    {
        if (FinalScoreText != null && Manager != null)
            FinalScoreText.text = Manager.Score.ToString();
        if (Panel != null)
            Panel.SetActive(true);
    }

    protected override void Subscribe(GameManager manager)
    {
        manager.OnGameOver += HandleGameOver;
    }

    protected override void Unsubscribe(GameManager manager)
    {
        manager.OnGameOver -= HandleGameOver;
    }

    protected override void OnManagerBound(GameManager manager)
    {
        if (manager.State == GameState.GameOver)
            HandleGameOver();
    }
}
