using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    Paused,
    GameOver
}

// 全局游戏状态：分数、暂停、结束、重开
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action<int> OnScoreChanged;
    public event Action OnGameOver;
    public event Action<bool> OnPauseChanged;

    public GameState State { get; private set; } = GameState.Playing;
    public int Score { get; private set; }
    public float DifficultyRampDuration = 90f;
    public float DifficultyProgress => Mathf.Clamp01(
        _difficultyElapsed / Mathf.Max(0.01f, DifficultyRampDuration));

    private float _difficultyElapsed;

    public static float GetDifficultyProgressOrDefault()
    {
        return Instance != null ? Instance.DifficultyProgress : 0f;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

#if UNITY_ANDROID && !UNITY_EDITOR
        // 移动端默认通常以 30 FPS 运行；游戏目标为 60 FPS。
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
#endif

        ApplyTimeScale(GameState.Playing);
    }

    private void Update()
    {
        _difficultyElapsed += Time.deltaTime;
    }

    public void AddScore(int amount)
    {
        if (State != GameState.Playing)
            return;

        Score += amount;
        OnScoreChanged?.Invoke(Score);
    }

    public void TriggerGameOver()
    {
        if (State == GameState.GameOver)
            return;

        State = GameState.GameOver;
        ApplyTimeScale(State);
        AudioManager.PlaySfx(SfxId.GameOver);
        OnGameOver?.Invoke();
    }

    public void TogglePause()
    {
        if (State == GameState.GameOver)
            return;

        if (State == GameState.Playing)
        {
            State = GameState.Paused;
            ApplyTimeScale(State);
            AudioManager.PlaySfx(SfxId.Pause);
            OnPauseChanged?.Invoke(true);
        }
        else
        {
            State = GameState.Playing;
            ApplyTimeScale(State);
            AudioManager.PlaySfx(SfxId.Resume);
            OnPauseChanged?.Invoke(false);
        }
    }

    public void Restart()
    {
        // Restart must release the old scene's pause/game-over time scale before loading.
        AudioManager.StopAllSfx();
        ApplyTimeScale(GameState.Playing);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private static void ApplyTimeScale(GameState state)
    {
        Time.timeScale = state == GameState.Playing ? 1f : 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            ApplyTimeScale(GameState.Playing);
        }
    }
}
