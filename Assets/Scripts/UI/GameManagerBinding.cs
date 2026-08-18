using UnityEngine;

// 统一 UI 对 GameManager 的生命周期绑定，确保切换管理器时不会重复订阅。
public abstract class GameManagerBinding : MonoBehaviour
{
    protected GameManager Manager { get; private set; }

    protected virtual void OnEnable()
    {
        TryBindManager();
    }

    protected virtual void Start()
    {
        TryBindManager();
    }

    protected virtual void OnDisable()
    {
        UnbindManager();
    }

    protected abstract void Subscribe(GameManager manager);
    protected abstract void Unsubscribe(GameManager manager);

    protected virtual void OnManagerBound(GameManager manager)
    {
    }

    private void TryBindManager()
    {
        var manager = GameManager.Instance;
        if (manager == null || Manager == manager)
            return;

        UnbindManager();
        Manager = manager;
        Subscribe(manager);
        OnManagerBound(manager);
    }

    private void UnbindManager()
    {
        if (Manager == null)
            return;

        Unsubscribe(Manager);
        Manager = null;
    }
}
