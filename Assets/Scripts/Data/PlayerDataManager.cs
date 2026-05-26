using System;
using System.IO;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    private static PlayerDataManager _instance;

    public static PlayerDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<PlayerDataManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("PlayerDataManager");
                    _instance = go.AddComponent<PlayerDataManager>();
                }
            }
            return _instance;
        }
    }

    [System.Serializable]
    public class PlayerData
    {
        public int coins = 500;
        public int hearts = 5;
        public int currentLevelIndex = 0;
        public string lastHeartUpdateTime = "";
    }

    private PlayerData _data;
    private string _savePath;

    public PlayerData Data => _data;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        _savePath = Path.Combine(Application.persistentDataPath, "player_data.json");
        LoadData();
    }

    public void LoadData()
    {
        if (File.Exists(_savePath))
        {
            try
            {
                string json = File.ReadAllText(_savePath);
                _data = JsonUtility.FromJson<PlayerData>(json);
                Debug.Log($"[PlayerDataManager] Data loaded. Coins: {_data.coins}, Hearts: {_data.hearts}, Level: {_data.currentLevelIndex + 1}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerDataManager] Error loading data: {e.Message}. Creating new.");
                CreateNewData();
            }
        }
        else
        {
            CreateNewData();
        }

        UpdateHeartsRecovery();
    }

    private void CreateNewData()
    {
        _data = new PlayerData
        {
            coins = 500,
            hearts = 5,
            currentLevelIndex = 0,
            lastHeartUpdateTime = DateTime.UtcNow.ToString("O")
        };
        SaveData();
        Debug.Log("[PlayerDataManager] New player data created.");
    }

    public void SaveData()
    {
        if (_data == null) return;
        try
        {
            string json = JsonUtility.ToJson(_data, true);
            File.WriteAllText(_savePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerDataManager] Error saving data: {e.Message}");
        }
    }

    public void UpdateHeartsRecovery()
    {
        if (_data == null) return;
        if (_data.hearts >= 5)
        {
            return;
        }

        if (DateTime.TryParse(_data.lastHeartUpdateTime, out DateTime lastTime))
        {
            TimeSpan elapsed = DateTime.UtcNow - lastTime;
            double elapsedSeconds = elapsed.TotalSeconds;

            if (elapsedSeconds >= 600) // 10 minutes
            {
                int heartsToRestore = Mathf.FloorToInt((float)(elapsedSeconds / 600.0));
                _data.hearts = Mathf.Min(5, _data.hearts + heartsToRestore);

                if (_data.hearts >= 5)
                {
                    _data.lastHeartUpdateTime = DateTime.UtcNow.ToString("O");
                }
                else
                {
                    DateTime newTime = lastTime.AddSeconds(heartsToRestore * 600);
                    _data.lastHeartUpdateTime = newTime.ToString("O");
                }
                SaveData();
                Debug.Log($"[PlayerDataManager] Restored {heartsToRestore} hearts. Current hearts: {_data.hearts}");
            }
        }
        else
        {
            _data.lastHeartUpdateTime = DateTime.UtcNow.ToString("O");
            SaveData();
        }
    }

    public float GetSecondsToNextHeart()
    {
        if (_data == null || _data.hearts >= 5) return 0f;

        if (DateTime.TryParse(_data.lastHeartUpdateTime, out DateTime lastTime))
        {
            TimeSpan elapsed = DateTime.UtcNow - lastTime;
            double elapsedSeconds = elapsed.TotalSeconds;
            double secondsLeft = 600.0 - elapsedSeconds;
            return secondsLeft > 0 ? (float)secondsLeft : 0f;
        }
        return 0f;
    }

    public void AddCoins(int amount)
    {
        if (_data == null) return;
        _data.coins += amount;
        SaveData();
        Debug.Log($"[PlayerDataManager] Added {amount} coins. New balance: {_data.coins}");
    }

    public bool DeductHeart()
    {
        if (_data == null) return false;

        UpdateHeartsRecovery(); // Ensure any offline hearts are restored first

        if (_data.hearts > 0)
        {
            if (_data.hearts == 5)
            {
                _data.lastHeartUpdateTime = DateTime.UtcNow.ToString("O");
            }
            _data.hearts--;
            SaveData();
            Debug.Log($"[PlayerDataManager] Deducted 1 heart. Current hearts: {_data.hearts}");
            return true;
        }
        return false;
    }

    public void SetLevel(int index)
    {
        if (_data == null) return;
        _data.currentLevelIndex = index;
        SaveData();
        Debug.Log($"[PlayerDataManager] Active level set to: {index + 1}");
    }
}
