using UnityEngine;

// 暂停按钮/暂停面板的显示与交互
public class PauseController : GameManagerBinding
{
    public GameObject PausePanel;

    public void OnPauseToggleClicked() => Manager?.TogglePause();

    private void HandlePauseChanged(bool paused)
    {
        if (PausePanel != null)
            PausePanel.SetActive(paused);
    }

    protected override void Subscribe(GameManager manager)
    {
        manager.OnPauseChanged += HandlePauseChanged;
    }

    protected override void Unsubscribe(GameManager manager)
    {
        manager.OnPauseChanged -= HandlePauseChanged;
    }

    protected override void OnManagerBound(GameManager manager)
    {
        HandlePauseChanged(manager.State == GameState.Paused);
    }
}
