using UnityEngine;
using UnityEngine.UI;

// 游戏结束面板：展示最终分数并处理重开按钮
public class GameOverPanel : MonoBehaviour
{
    public GameObject Panel;
    public Text FinalScoreText;
    private GameManager _subscribedManager;

    void OnEnable()
    {
        TrySubscribe();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void Start()
    {
        TrySubscribe();
    }

    public void OnRestartButtonClicked() => GameManager.Instance?.Restart();

    private void HandleGameOver()
    {
        if (FinalScoreText != null && _subscribedManager != null)
            FinalScoreText.text = _subscribedManager.Score.ToString();
        if (Panel != null)
            Panel.SetActive(true);
    }

    private void TrySubscribe()
    {
        var manager = GameManager.Instance;
        if (manager == null || _subscribedManager == manager)
            return;

        Unsubscribe();
        _subscribedManager = manager;
        _subscribedManager.OnGameOver += HandleGameOver;
        if (_subscribedManager.State == GameState.GameOver)
            HandleGameOver();
    }

    private void Unsubscribe()
    {
        if (_subscribedManager == null)
            return;

        _subscribedManager.OnGameOver -= HandleGameOver;
        _subscribedManager = null;
    }
}
