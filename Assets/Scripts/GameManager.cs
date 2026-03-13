using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton game manager that persists across scenes.
/// Handles game state, score tracking, and scene transitions.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        Victory
    }

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.MainMenu;

    [Header("Score")]
    [SerializeField] private int score;
    [SerializeField] private int highScore;

    public GameState CurrentState => currentState;
    public int Score => score;
    public int HighScore => highScore;

    public event System.Action<GameState> OnStateChanged;
    public event System.Action<int> OnScoreChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadHighScore();
    }

    private void Update()
    {
        if (currentState == GameState.Playing && Input.GetKeyDown(KeyCode.Escape))
            PauseGame();
        else if (currentState == GameState.Paused && Input.GetKeyDown(KeyCode.Escape))
            ResumeGame();
    }

    public void StartGame()
    {
        score = 0;
        ChangeState(GameState.Playing);
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainScene");
    }

    public void PauseGame()
    {
        ChangeState(GameState.Paused);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        ChangeState(GameState.Playing);
        Time.timeScale = 1f;
    }

    public void GameOver()
    {
        ChangeState(GameState.GameOver);
        Time.timeScale = 0f;
        SaveHighScore();
    }

    public void Victory()
    {
        ChangeState(GameState.Victory);
        SaveHighScore();
    }

    public void AddScore(int points)
    {
        score += points;
        if (score > highScore)
            highScore = score;
        OnScoreChanged?.Invoke(score);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        ChangeState(GameState.MainMenu);
        SceneManager.LoadScene("MainMenu");
    }

    private void ChangeState(GameState newState)
    {
        currentState = newState;
        OnStateChanged?.Invoke(currentState);
    }

    private void SaveHighScore()
    {
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }
    }

    private void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }
}
