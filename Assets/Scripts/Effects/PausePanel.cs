using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class PausePanel : MonoBehaviour
{
    [Header("── Pause Button (nút mở panel) ──")]
    public Button pauseButton;

    [Header("── Panel References ──")]
    public CanvasGroup panelGroup;
    public RectTransform panelRect;

    [Header("── Dim Overlay ──")]
    public Image dimOverlay;
    [Range(0f, 1f)] public float dimTargetAlpha = 0.55f;

    [Header("── Buttons trong Panel ──")]
    public Button homeButton;
    public Button restartButton;
    public Button closeButton;

    [Header("── Toggle Buttons (Music / Sound / Shake) ──")]
    public Button musicToggleButton;
    public Button soundToggleButton;
    public Button shakeToggleButton;

    [Header("── Toggle Icons ──")]
    public Image musicOnIcon;
    public Image musicOffIcon;
    public Image soundOnIcon;
    public Image soundOffIcon;
    public Image shakeOnIcon;
    public Image shakeOffIcon;

    [Header("── Animation Settings ──")]
    public float openDuration  = 0.38f;
    public float closeDuration = 0.28f;
    public float dimDuration   = 0.25f;
    public float panelStartScale = 0.75f;

    [Header("── PlayerPrefs Keys ──")]
    public string musicPrefKey = "MusicEnabled";
    public string soundPrefKey = "SoundEnabled";
    public string shakePrefKey = "ShakeEnabled";

    [Header("── UIManager Reference ──")]
    public UIManager uiManager;

    private bool _isPaused      = false;
    private bool _musicEnabled  = true;
    private bool _soundEnabled  = true;
    private bool _shakeEnabled  = true;

    private Sequence _panelSeq;

    private void Awake()
    {
        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>();

        _musicEnabled = PlayerPrefs.GetInt(musicPrefKey, 1) == 1;
        _soundEnabled = PlayerPrefs.GetInt(soundPrefKey, 1) == 1;
        _shakeEnabled = PlayerPrefs.GetInt(shakePrefKey, 1) == 1;

        InitPanelHidden();

        if (pauseButton   != null) pauseButton.onClick.AddListener(OpenPanel);
        if (homeButton    != null) homeButton.onClick.AddListener(OnHomeClicked);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
        if (closeButton   != null) closeButton.onClick.AddListener(ClosePanel);

        if (musicToggleButton != null) musicToggleButton.onClick.AddListener(ToggleMusic);
        if (soundToggleButton != null) soundToggleButton.onClick.AddListener(ToggleSound);
        if (shakeToggleButton != null) shakeToggleButton.onClick.AddListener(ToggleShake);

        RefreshToggleUI();

        Debug.Log("[PausePanel] Awake — khởi tạo xong.");
    }

    public void OpenPanel()
    {
        if (_isPaused) return;
        _isPaused = true;

        SfxManager.Instance?.PlayButtonClick();

        uiManager?.StopTimer();

        // Đóng băng thời gian game
        Time.timeScale = 0f;

        // Kill sequence cũ nếu còn
        _panelSeq?.Kill(false);

        // Đặt lại trạng thái bắt đầu của panel
        if (panelRect  != null) panelRect.localScale  = Vector3.one * panelStartScale;
        if (panelGroup != null)
        {
            panelGroup.alpha          = 0f;
            panelGroup.interactable   = false;
            panelGroup.blocksRaycasts = false;
        }

        _panelSeq = DOTween.Sequence().SetUpdate(true); // SetUpdate(true) → không bị ảnh hưởng bởi timeScale = 0

        // 1. Fade dim overlay
        if (dimOverlay != null)
            _panelSeq.Append(dimOverlay.DOFade(dimTargetAlpha, dimDuration)
                                       .SetUpdate(true)
                                       .SetEase(Ease.OutQuad));
        else
            _panelSeq.AppendInterval(0f);

        // 2. Fade + scale panel đồng thời
        if (panelGroup != null)
            _panelSeq.Append(panelGroup.DOFade(1f, openDuration)
                                       .SetUpdate(true)
                                       .SetEase(Ease.OutQuad));

        if (panelRect != null)
            _panelSeq.Join(panelRect.DOScale(1f, openDuration)
                                    .SetUpdate(true)
                                    .SetEase(Ease.OutBack));

        // 3. Bật tương tác sau khi animation xong
        _panelSeq.OnComplete(() =>
        {
            if (panelGroup != null)
            {
                panelGroup.interactable   = true;
                panelGroup.blocksRaycasts = true;
            }
            Debug.Log("[PausePanel] Panel đã mở.");
        });

        // Bounce nhẹ cho pause button khi bấm
        if (pauseButton != null)
            pauseButton.transform
                .DOPunchScale(Vector3.one * 0.15f, 0.25f, vibrato: 5)
                .SetUpdate(true);
    }

    public void ClosePanel()
    {
        if (!_isPaused) return;

        SfxManager.Instance?.PlayButtonClick();

        // Kill sequence cũ
        _panelSeq?.Kill(false);

        _panelSeq = DOTween.Sequence().SetUpdate(true);

        // 1. Fade + scale panel out đồng thời
        if (panelGroup != null)
            _panelSeq.Append(panelGroup.DOFade(0f, closeDuration)
                                       .SetUpdate(true)
                                       .SetEase(Ease.InQuad));

        if (panelRect != null)
            _panelSeq.Join(panelRect.DOScale(panelStartScale, closeDuration)
                                    .SetUpdate(true)
                                    .SetEase(Ease.InBack));

        // 2. Fade dim overlay out
        if (dimOverlay != null)
            _panelSeq.Append(dimOverlay.DOFade(0f, dimDuration)
                                       .SetUpdate(true)
                                       .SetEase(Ease.OutQuad));

        _panelSeq.OnComplete(() =>
        {
            if (panelGroup != null)
            {
                panelGroup.interactable   = false;
                panelGroup.blocksRaycasts = false;
            }

            // Khôi phục thời gian
            Time.timeScale = 1f;
            _isPaused = false;

            // Tiếp tục countdown trong UIManager
            uiManager?.ResumeTimer();
            Debug.Log("[PausePanel] Panel đã đóng — timeScale = 1.");
        });
    }

    private void OnHomeClicked()
    {
        SfxManager.Instance?.PlayButtonClick();

        // Khôi phục timeScale trước khi chuyển scene
        Time.timeScale = 1f;
        _isPaused = false;

        // Bounce animation cho nút (unscaled)
        if (homeButton != null)
            homeButton.transform
                .DOPunchScale(Vector3.one * 0.18f, 0.2f, vibrato: 5)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    // Dùng hàm ExitToMainMenu của UIManager
                    uiManager?.ExitToMainMenu();
                    // Fallback nếu không có UIManager
                    if (uiManager == null)
                        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
                });
    }

    private void OnRestartClicked()
    {
        SfxManager.Instance?.PlayButtonClick();

        // Khôi phục timeScale trước
        Time.timeScale = 1f;
        _isPaused = false;

        // Bounce animation cho nút (unscaled)
        if (restartButton != null)
            restartButton.transform
                .DOPunchScale(Vector3.one * 0.18f, 0.2f, vibrato: 5)
                .SetUpdate(true)
                .OnComplete(() => DoRestart());
        else
            DoRestart();
    }

    private void DoRestart()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.UpdateHeartsRecovery();
            bool deducted = PlayerDataManager.Instance.DeductHeart();

            if (!deducted || PlayerDataManager.Instance.Data.hearts < 0)
            {
                Debug.LogWarning("[PausePanel] Hết tim! Không thể restart — quay về Main Menu.");
                uiManager?.ExitToMainMenu();
                if (uiManager == null)
                    UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
                return;
            }
        }

        // Ẩn panel ngay (không animation)
        InitPanelHidden();

        // Đặt lại dim overlay
        if (dimOverlay != null)
        {
            Color c = dimOverlay.color; c.a = 0f;
            dimOverlay.color = c;
        }

        LevelManager.Instance?.RestartLevel();
        Debug.Log("[PausePanel] Restart level — trừ 1 tim.");
    }

    private void ToggleMusic()
    {
        _musicEnabled = !_musicEnabled;
        PlayerPrefs.SetInt(musicPrefKey, _musicEnabled ? 1 : 0);
        PlayerPrefs.Save();

        SfxManager.Instance?.PlayButtonClick();

        // Điều chỉnh âm lượng AudioMixer / AudioSource toàn cục
        ApplyMusicSetting();
        RefreshToggleUI();

        // Bounce nhẹ nút
        AnimateToggleButton(musicToggleButton);
        Debug.Log($"[PausePanel] Music: {(_musicEnabled ? "ON" : "OFF")}");
    }

    private void ToggleSound()
    {
        _soundEnabled = !_soundEnabled;
        PlayerPrefs.SetInt(soundPrefKey, _soundEnabled ? 1 : 0);
        PlayerPrefs.Save();

        SfxManager.Instance?.PlayButtonClick();

        ApplySoundSetting();
        RefreshToggleUI();

        AnimateToggleButton(soundToggleButton);
        Debug.Log($"[PausePanel] Sound: {(_soundEnabled ? "ON" : "OFF")}");
    }

    private void ToggleShake()
    {
        _shakeEnabled = !_shakeEnabled;
        PlayerPrefs.SetInt(shakePrefKey, _shakeEnabled ? 1 : 0);
        PlayerPrefs.Save();

        SfxManager.Instance?.PlayButtonClick();

        RefreshToggleUI();

        AnimateToggleButton(shakeToggleButton);
        Debug.Log($"[PausePanel] Shake: {(_shakeEnabled ? "ON" : "OFF")}");
    }

    public bool IsMusicEnabled => _musicEnabled;
    public bool IsSoundEnabled => _soundEnabled;
    public bool IsShakeEnabled => _shakeEnabled;

    private void ApplyMusicSetting()
    {
        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (var src in sources)
        {
            if (src.loop)
            {
                src.mute = !_musicEnabled;
            }
        }
    }

    private void ApplySoundSetting()
    {
        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (var src in sources)
        {
            if (!src.loop)
            {
                src.mute = !_soundEnabled;
            }
        }
    }

    private void InitPanelHidden()
    {
        if (panelGroup != null)
        {
            panelGroup.alpha          = 0f;
            panelGroup.interactable   = false;
            panelGroup.blocksRaycasts = false;
        }
        if (panelRect != null)
            panelRect.localScale = Vector3.one * panelStartScale;
        if (dimOverlay != null)
        {
            Color c = dimOverlay.color; c.a = 0f;
            dimOverlay.color = c;
            dimOverlay.raycastTarget = false;
        }
    }

    private void RefreshToggleUI()
    {
        SetToggleIcons(musicOnIcon,  musicOffIcon,  _musicEnabled);
        SetToggleIcons(soundOnIcon,  soundOffIcon,  _soundEnabled);
        SetToggleIcons(shakeOnIcon,  shakeOffIcon,  _shakeEnabled);
    }

    private static void SetToggleIcons(Image onIcon, Image offIcon, bool isOn)
    {
        if (onIcon  != null) onIcon.gameObject.SetActive(isOn);
        if (offIcon != null) offIcon.gameObject.SetActive(!isOn);
    }

    private static void AnimateToggleButton(Button btn)
    {
        if (btn == null) return;
        btn.transform.DOKill(false);
        btn.transform
            .DOPunchScale(Vector3.one * 0.2f, 0.22f, vibrato: 6)
            .SetUpdate(true);
    }
}
