using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("── HUD References ──")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI heartText;
    [SerializeField] private TextMeshProUGUI heartTimerText;

    [Header("── Buttons ──")]
    [SerializeField] private Button playButton;
    [SerializeField] private GameObject hardPlayButtonObject; 
    [SerializeField] private Button settingsButton;

    private const int HARD_LEVEL_START_INDEX = 4;

    [Header("── Levels Configuration ──")]
    [SerializeField] private LevelData[] allLevels;
    [SerializeField] private RectTransform levelListContainer;
    [SerializeField] private GameObject levelNodePrefab;

    [Header("── Settings Panel (Optional) ──")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button closeSettingsButton;

    [Header("── Heart Warning Panel (Optional) ──")]
    [SerializeField] private GameObject noHeartsPanel;
    [SerializeField] private Button closeNoHeartsButton;
    [SerializeField] private Button watchAdButton;      // +1 tim khi xem quảng cáo
    [SerializeField] private Button buyHeartsButton;    // -150 xu để max tim

    private int _lastHearts = -1;
    private int _lastCoins  = -1;

    private MainMenuUIEffects _fx;

    private void Start()
    {
        // Tìm hoặc tạo UIEffects
        _fx = GetComponent<MainMenuUIEffects>();

        // Setup PlayerDataManager
        if (PlayerDataManager.Instance == null)
        {
            GameObject pdmObj = new GameObject("PlayerDataManager");
            pdmObj.AddComponent<PlayerDataManager>();
        }

        // Bind buttons
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayPressed);

        // Hard play button (viền đỏ) cũng dẫn đến cùng hàm OnPlayPressed
        if (hardPlayButtonObject != null)
        {
            Button hardBtn = hardPlayButtonObject.GetComponent<Button>();
            if (hardBtn != null)
                hardBtn.onClick.AddListener(OnPlayPressed);
        }

        if (settingsButton != null && settingsPanel != null)
            settingsButton.onClick.AddListener(OnOpenSettings);

        if (closeSettingsButton != null && settingsPanel != null)
            closeSettingsButton.onClick.AddListener(OnCloseSettings);

        // Ẩn panel qua SetActive chỉ khi không có _fx
        if (settingsPanel != null && _fx == null)
            settingsPanel.SetActive(false);

        if (closeNoHeartsButton != null)
            closeNoHeartsButton.onClick.AddListener(OnCloseNoHearts);

        if (watchAdButton != null)
            watchAdButton.onClick.AddListener(OnWatchAdPressed);

        if (buyHeartsButton != null)
            buyHeartsButton.onClick.AddListener(OnBuyHeartsPressed);

        if (noHeartsPanel != null && _fx == null)
            noHeartsPanel.SetActive(false);

        // Đăng ký punch effect cho tất cả button nếu có _fx
        if (_fx != null)
        {
            _fx.AddPunchOnClick(playButton);
            _fx.AddPunchOnClick(settingsButton);
            _fx.AddPunchOnClick(closeSettingsButton);
            _fx.AddPunchOnClick(closeNoHeartsButton);
            _fx.AddPunchOnClick(watchAdButton);
            _fx.AddPunchOnClick(buyHeartsButton);
        }

        UpdateHUD();
        GenerateLevelList();
        RefreshPlayButton();
    }

    private void Update()
    {
        UpdateHeartTimer();
    }

    private void UpdateHUD()
    {
        if (PlayerDataManager.Instance == null) return;

        var data = PlayerDataManager.Instance.Data;

        if (coinText != null)
            coinText.text = data.coins.ToString("N0");

        if (heartText != null)
            heartText.text = data.hearts.ToString();
    }

    private void UpdateHeartTimer()
    {
        if (PlayerDataManager.Instance == null) return;

        PlayerDataManager.Instance.UpdateHeartsRecovery();
        var data = PlayerDataManager.Instance.Data;

        if (_lastHearts != data.hearts)
        {
            if (_lastHearts >= 0 && _fx != null)
                _fx.PlayHeartChangeEffect();

            _lastHearts = data.hearts;

            if (heartText != null)
                heartText.text = data.hearts.ToString();
        }

        if (_lastCoins != data.coins)
        {
            if (_lastCoins >= 0 && _fx != null && coinText != null)
                _fx.PlayCoinChangeEffect(coinText.transform);

            _lastCoins = data.coins;

            if (coinText != null)
                coinText.text = data.coins.ToString("N0");
        }

        if (data.hearts >= 5)
        {
            if (heartTimerText != null)
                heartTimerText.text = "MAX";
        }
        else
        {
            float secondsLeft = PlayerDataManager.Instance.GetSecondsToNextHeart();
            if (secondsLeft > 0)
            {
                int minutes = Mathf.FloorToInt(secondsLeft / 60f);
                int seconds = Mathf.FloorToInt(secondsLeft % 60f);
                if (heartTimerText != null)
                    heartTimerText.text = $"{minutes:00}:{seconds:00}";
            }
            else
            {
                if (heartTimerText != null)
                    heartTimerText.text = "00:00";
            }
        }

        if (watchAdButton != null)
            watchAdButton.interactable = (data.hearts < 5);

        if (buyHeartsButton != null)
            buyHeartsButton.interactable = (data.hearts < 5 && data.coins >= 150);
    }

    private void OnOpenSettings()
    {
        if (_fx != null)
            _fx.OpenSettingsPanel();
        else if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    private void OnCloseSettings()
    {
        if (_fx != null)
            _fx.CloseSettingsPanel();
        else if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void ShowNoHeartsPanel()
    {
        if (_fx != null)
            _fx.OpenNoHeartsPanel();
        else if (noHeartsPanel != null)
            noHeartsPanel.SetActive(true);
    }

    private void OnCloseNoHearts()
    {
        if (_fx != null)
            _fx.CloseNoHeartsPanel();
        else if (noHeartsPanel != null)
            noHeartsPanel.SetActive(false);
    }

    /// <summary>Đổi giữa nút Play thường và nút Play hard (viền đỏ) tùy theo level hiện tại.</summary>
    private void RefreshPlayButton()
    {
        if (PlayerDataManager.Instance == null) return;
        bool isHard = PlayerDataManager.Instance.Data.currentLevelIndex >= HARD_LEVEL_START_INDEX;

        if (playButton != null)
            playButton.gameObject.SetActive(!isHard);

        if (hardPlayButtonObject != null)
            hardPlayButtonObject.SetActive(isHard);
    }

    private void GenerateLevelList()
    {
        if (levelListContainer == null || levelNodePrefab == null || allLevels == null) return;

        // Clear existing children
        foreach (Transform child in levelListContainer)
        {
            Destroy(child.gameObject);
        }

        int currentActiveLevel = 0;
        if (PlayerDataManager.Instance != null)
        {
            currentActiveLevel = PlayerDataManager.Instance.Data.currentLevelIndex;
        }

        for (int i = allLevels.Length - 1; i >= currentActiveLevel; i--)
        {
            int levelIndex = i;
            GameObject node = Instantiate(levelNodePrefab, levelListContainer);
            node.name = $"LevelNode_{levelIndex + 1}";

            // Setup Text
            TextMeshProUGUI nodeText = node.GetComponentInChildren<TextMeshProUGUI>();
            if (nodeText != null)
                nodeText.text = (levelIndex + 1).ToString();

            // Tìm tất cả 3 loại image trong node prefab
            Transform activeImgTrans   = node.transform.Find("ActiveImage");
            Transform inactiveImgTrans = node.transform.Find("InactiveImage");
            Transform hardImgTrans     = node.transform.Find("HardImage");

            // Fallback naming cho ActiveImage
            if (activeImgTrans == null) activeImgTrans = node.transform.Find("Active");
            if (activeImgTrans == null) activeImgTrans = node.transform.Find("GreenImage");
            if (activeImgTrans == null) activeImgTrans = node.transform.Find("Green");

            // Fallback naming cho InactiveImage
            if (inactiveImgTrans == null) inactiveImgTrans = node.transform.Find("Inactive");
            if (inactiveImgTrans == null) inactiveImgTrans = node.transform.Find("GreyImage");
            if (inactiveImgTrans == null) inactiveImgTrans = node.transform.Find("Grey");

            bool isActiveNode = (levelIndex == currentActiveLevel);
            bool isHardNode   = isActiveNode && (levelIndex >= HARD_LEVEL_START_INDEX);

            if (activeImgTrans != null || inactiveImgTrans != null || hardImgTrans != null)
            {
                // Tắt tất cả trước, rồi bật đúng loại
                if (activeImgTrans   != null) activeImgTrans.gameObject.SetActive(false);
                if (inactiveImgTrans != null) inactiveImgTrans.gameObject.SetActive(false);
                if (hardImgTrans     != null) hardImgTrans.gameObject.SetActive(false);

                if (isActiveNode)
                {
                    if (isHardNode && hardImgTrans != null)
                    {
                        // Level hard đang active → hiện HardImage (đỏ)
                        hardImgTrans.gameObject.SetActive(true);
                    }
                    else if (activeImgTrans != null)
                    {
                        // Level thường đang active → hiện ActiveImage (xanh)
                        activeImgTrans.gameObject.SetActive(true);
                    }
                    if (nodeText != null) nodeText.color = Color.white;
                }
                else
                {
                    // Level chưa active → hiện InactiveImage (xám)
                    if (inactiveImgTrans != null) inactiveImgTrans.gameObject.SetActive(true);
                    if (nodeText != null) nodeText.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                }
            }
            else
            {
                // Fallback: đổi màu Image gốc của node
                Image nodeImage = node.GetComponent<Image>();
                if (nodeImage != null)
                {
                    if (isHardNode)
                    {
                        nodeImage.color = new Color(0.9f, 0.2f, 0.2f, 1f); // Đỏ (hard)
                        if (nodeText != null) nodeText.color = Color.white;
                    }
                    else if (isActiveNode)
                    {
                        nodeImage.color = new Color(0.2f, 0.8f, 0.2f, 1f); // Xanh (active)
                        if (nodeText != null) nodeText.color = Color.white;
                    }
                    else
                    {
                        nodeImage.color = new Color(0.7f, 0.7f, 0.7f, 1f); // Xám (inactive)
                        if (nodeText != null) nodeText.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                    }
                }
            }

            // Bind button component to select this level + punch effect
            Button nodeButton = node.GetComponent<Button>();
            if (nodeButton != null)
            {
                if (_fx != null) _fx.AddPunchOnClick(nodeButton);

                nodeButton.onClick.AddListener(() =>
                {
                    if (PlayerDataManager.Instance != null)
                    {
                        PlayerDataManager.Instance.SetLevel(levelIndex);
                        UpdateHUD();
                        GenerateLevelList(); // Re-render để cập nhật màu sắc
                        RefreshPlayButton(); // Cập nhật nút Play theo level đã chọn
                    }
                });
            }
        }
    }

    private void OnPlayPressed()
    {
        if (PlayerDataManager.Instance == null) return;

        PlayerDataManager.Instance.UpdateHeartsRecovery();
        if (PlayerDataManager.Instance.Data.hearts <= 0)
        {
            Debug.LogWarning("[MainMenuController] Hết tim! Không thể chơi.");
            ShowNoHeartsPanel();
            return;
        }

        SceneManager.LoadScene("GameScene");
    }

    private void OnWatchAdPressed()
    {
        if (PlayerDataManager.Instance == null) return;

        bool success = PlayerDataManager.Instance.AddHeartFromAd();
        if (success)
        {
            Debug.Log("[MainMenuController] +1 tim từ quảng cáo.");
            UpdateHUD();

            // Tự đóng panel nếu tim đã đầy
            if (PlayerDataManager.Instance.Data.hearts >= 5)
                OnCloseNoHearts();
        }
    }

    private void OnBuyHeartsPressed()
    {
        if (PlayerDataManager.Instance == null) return;

        bool success = PlayerDataManager.Instance.BuyHeartsWithCoins();
        if (success)
        {
            Debug.Log("[MainMenuController] Mua tim thành công (-150 xu, tim MAX).");
            UpdateHUD();

            // Tự đóng panel sau khi mua thành công
            OnCloseNoHearts();
        }
        else
        {
            // Không đủ xu — có thể hiện thị feedback nếu cần
            Debug.LogWarning("[MainMenuController] Mua tim thất bại: không đủ xu hoặc tim đã đầy.");
        }
    }
}
