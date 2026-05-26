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
    [SerializeField] private Button settingsButton;

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

    private void Start()
    {
        // Setup PlayerDataManager
        if (PlayerDataManager.Instance == null)
        {
            GameObject pdmObj = new GameObject("PlayerDataManager");
            pdmObj.AddComponent<PlayerDataManager>();
        }

        // Bind buttons
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayPressed);

        if (settingsButton != null && settingsPanel != null)
            settingsButton.onClick.AddListener(() => settingsPanel.SetActive(true));

        if (closeSettingsButton != null && settingsPanel != null)
            closeSettingsButton.onClick.AddListener(() => settingsPanel.SetActive(false));

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (closeNoHeartsButton != null && noHeartsPanel != null)
            closeNoHeartsButton.onClick.AddListener(() => noHeartsPanel.SetActive(false));

        if (noHeartsPanel != null)
            noHeartsPanel.SetActive(false);

        UpdateHUD();
        GenerateLevelList();
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

        if (heartText != null)
            heartText.text = data.hearts.ToString();

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

        // Generate nodes vertically from bottom to top (completed levels are skipped)
        // Only levels from currentActiveLevel and up are generated, pushing the active level to the bottom
        for (int i = allLevels.Length - 1; i >= currentActiveLevel; i--)
        {
            int levelIndex = i;
            GameObject node = Instantiate(levelNodePrefab, levelListContainer);
            node.name = $"LevelNode_{levelIndex + 1}";

            // Setup Text
            TextMeshProUGUI nodeText = node.GetComponentInChildren<TextMeshProUGUI>();
            if (nodeText != null)
                nodeText.text = (levelIndex + 1).ToString();

            // Setup active (Green) and inactive (Grey) images if they exist as children
            Transform activeImgTrans = node.transform.Find("ActiveImage");
            Transform inactiveImgTrans = node.transform.Find("InactiveImage");

            // Look for common alternatives in child naming
            if (activeImgTrans == null) activeImgTrans = node.transform.Find("Active");
            if (activeImgTrans == null) activeImgTrans = node.transform.Find("GreenImage");
            if (activeImgTrans == null) activeImgTrans = node.transform.Find("Green");

            if (inactiveImgTrans == null) inactiveImgTrans = node.transform.Find("Inactive");
            if (inactiveImgTrans == null) inactiveImgTrans = node.transform.Find("GreyImage");
            if (inactiveImgTrans == null) inactiveImgTrans = node.transform.Find("Grey");

            if (activeImgTrans != null && inactiveImgTrans != null)
            {
                if (levelIndex == currentActiveLevel)
                {
                    activeImgTrans.gameObject.SetActive(true);
                    inactiveImgTrans.gameObject.SetActive(false);
                    if (nodeText != null) nodeText.color = Color.white;
                }
                else
                {
                    activeImgTrans.gameObject.SetActive(false);
                    inactiveImgTrans.gameObject.SetActive(true);
                    if (nodeText != null) nodeText.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                }
            }
            else
            {
                // Fallback to modifying the node's main Image color if child objects are not found
                Image nodeImage = node.GetComponent<Image>();
                if (nodeImage != null)
                {
                    if (levelIndex == currentActiveLevel)
                    {
                        nodeImage.color = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
                        if (nodeText != null) nodeText.color = Color.white;
                    }
                    else
                    {
                        nodeImage.color = new Color(0.7f, 0.7f, 0.7f, 1f); // Grey
                        if (nodeText != null) nodeText.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                    }
                }
            }

            // Bind button component to select this level
            Button nodeButton = node.GetComponent<Button>();
            if (nodeButton != null)
            {
                nodeButton.onClick.AddListener(() =>
                {
                    if (PlayerDataManager.Instance != null)
                    {
                        PlayerDataManager.Instance.SetLevel(levelIndex);
                        UpdateHUD();
                        GenerateLevelList(); // Re-render to update visual colors
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
            if (noHeartsPanel != null)
            {
                noHeartsPanel.SetActive(true);
            }
            return;
        }

        SceneManager.LoadScene("GameScene");
    }
}
