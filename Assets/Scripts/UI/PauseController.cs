using UnityEngine;

// 暂停按钮/暂停面板的显示与交互
public class PauseController : MonoBehaviour
{
    public GameObject PausePanel;
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

    public void OnPauseButtonClicked() => GameManager.Instance?.TogglePause();

    public void OnResumeButtonClicked() => GameManager.Instance?.TogglePause();

    private void HandlePauseChanged(bool paused)
    {
        if (PausePanel != null)
            PausePanel.SetActive(paused);
    }

    private void TrySubscribe()
    {
        var manager = GameManager.Instance;
        if (manager == null || _subscribedManager == manager)
            return;

        Unsubscribe();
        _subscribedManager = manager;
        _subscribedManager.OnPauseChanged += HandlePauseChanged;
        HandlePauseChanged(_subscribedManager.State == GameState.Paused);
    }

    private void Unsubscribe()
    {
        if (_subscribedManager == null)
            return;

        _subscribedManager.OnPauseChanged -= HandlePauseChanged;
        _subscribedManager = null;
    }
}
