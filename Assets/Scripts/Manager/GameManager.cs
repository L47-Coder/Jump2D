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

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Time.timeScale = 1f;
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
        Time.timeScale = 0f;
        OnGameOver?.Invoke();
    }

    public void TogglePause()
    {
        if (State == GameState.GameOver)
            return;

        if (State == GameState.Playing)
        {
            State = GameState.Paused;
            Time.timeScale = 0f;
            OnPauseChanged?.Invoke(true);
        }
        else
        {
            State = GameState.Playing;
            Time.timeScale = 1f;
            OnPauseChanged?.Invoke(false);
        }
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1f;
        }
    }
}
