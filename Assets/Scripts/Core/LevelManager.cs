using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level List")]
    [SerializeField] LevelData[] allLevels;

    [Header("References")]
    [SerializeField] LevelSpawner spawner;

    [Header("── UI ──")]
    [SerializeField] UIManager uiManager;

    [Header("── Victory ──")]
    [SerializeField] VictorySequenceController victorySequence;

    [SerializeField] int rewardCoinsPerLevel = 20;

    int currentIndex = 0;
    BoardController[] currentBoards = new BoardController[0];
    bool levelComplete = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        int levelToLoad = 0;
        if (PlayerDataManager.Instance != null)
        {
            levelToLoad = PlayerDataManager.Instance.Data.currentLevelIndex;
        }

        if (allLevels != null && allLevels.Length > 0)
        {
            if (levelToLoad < 0 || levelToLoad >= allLevels.Length)
                levelToLoad = 0;
            LoadLevel(levelToLoad);
        }
    }

    public void LoadLevel(int index)
    {
        if (allLevels == null || index < 0 || index >= allLevels.Length)
        {
            Debug.LogWarning($"[LevelManager] Level {index} khong ton tai.");
            return;
        }

        levelComplete = false;

        if (victorySequence != null)
            victorySequence.ResetState();

        currentIndex = index;
        currentBoards = spawner.SpawnLevel(allLevels[index]);

        if (uiManager != null)
        {
            uiManager.SetLevelText(currentIndex);
            uiManager.StartCountdown(uiManager.countdownDuration, OnTimeUp);
        }

        if (TimeFreezeController.Instance != null)
        {
            TimeFreezeController.Instance.RefreshLockState();
        }

        // Level 3 (index == 2): hiện reward panel lần đầu tiên
        if (index == 2 && TimeFreezeRewardPanel.ShouldShow())
        {
            if (TimeFreezeRewardPanel.Instance != null)
            {
                TimeFreezeRewardPanel.Instance.Show();
            }
            else
            {
                Debug.LogWarning("[LevelManager] TimeFreezeRewardPanel.Instance là null — hãy thêm TimeFreezeRewardPanel vào scene!");
            }
        }

        // Level 4 (index == 3): hiện giới thiệu sand block lần đầu tiên
        if (index == 3 && SandBlockIntroPanel.ShouldShow())
        {
            if (SandBlockIntroPanel.Instance != null)
            {
                SandBlockIntroPanel.Instance.Show();
            }
            else
            {
                Debug.LogWarning("[LevelManager] SandBlockIntroPanel.Instance là null — hãy thêm SandBlockIntroPanel vào scene!");
            }
        }

        Debug.Log($"[LevelManager] Loaded Level {index + 1}");
    }

    public void LoadNextLevel()
    {
        int next = currentIndex + 1;
        if (next >= allLevels.Length)
            next = 0; // Hết level → quay về level 1
        
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.SetLevel(next);
        }

        LoadLevel(next);
    }
    public void RestartLevel() => LoadLevel(currentIndex);

    public void CheckWinCondition()
    {
        if (levelComplete || currentBoards == null) return;

        foreach (var board in currentBoards)
        {
            if (board == null) continue;
            if (!board.AllBlocksFree()) return;
        }

        levelComplete = true;
        OnLevelComplete();
    }

    void OnLevelComplete()
    {
        Debug.Log($"[LevelManager] LEVEL {currentIndex + 1} COMPLETE!");

        if (uiManager != null)
            uiManager.StopTimer();

        // Kích hoạt toàn bộ victory sequence
        if (victorySequence != null)
        {
            string levelName = $"Level {currentIndex + 1}";
            int coins = GetRewardCoins();
            victorySequence.TriggerVictory(coins, levelName);
        }
        else
        {
            if (uiManager != null)
                uiManager.ShowLevelComplete();
            else
                Invoke(nameof(LoadNextLevel), 1.5f);
        }
    }

    int GetRewardCoins()
    {
        return rewardCoinsPerLevel;
    }

    void OnTimeUp()
    {
        Debug.Log($"[LevelManager] LEVEL {currentIndex + 1} — HET GIO! Hien Lose Panel.");
        levelComplete = true; // Chặn CheckWinCondition sau khi hết giờ

        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.DeductHeart();
        }

        if (uiManager != null)
            uiManager.ShowLosePanel(currentIndex);
    }

    public int GetCurrentLevelIndex() => currentIndex;
    public int GetTotalLevels() => allLevels != null ? allLevels.Length : 0;
    public LevelData GetCurrentLevel() => (allLevels != null && currentIndex < allLevels.Length)
                                                ? allLevels[currentIndex] : null;
}