using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    [Header("── HUD ──")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI levelText;

    [Header("── Countdown ──")]
    public float countdownDuration = 180f;
    public float warningThreshold = 30f;
    public Color normalColor = Color.white;
    public Color warningColor = Color.red;

    [Header("── Complete Panel (tuỳ chọn) ──")]
    public CanvasGroup completePanelGroup;
    public RectTransform completePanelRect;
    public TextMeshProUGUI completeTitleText;
    public Button nextLevelButton;

    [Header("── Dim Overlay ──")]
    public Image dimOverlay;

    [Header("── Lose Panel ──")]
    public CanvasGroup losePanelGroup;
    public RectTransform losePanelRect;
    public Button restartButtonLose;
    [Tooltip("Text 'LOSE' bên trong lose panel (tuỳ chọn — dùng để shake riêng)")]
    public RectTransform loseTitleRect;

    [Header("── Main Menu ──")]
    public Button homeButton;

    [Header("── Lose Panel Animation ──")]
    public float loseDimTargetAlpha = 0.62f;
    public float loseTitleInDuration = 0.42f;
    public float loseButtonDelay = 0.08f;
    public float loseButtonInDuration = 0.36f;
    public float loseButtonPulseDuration = 0.7f;
    [Tooltip("Camera bị shake khi thua — để trống nếu không dùng")]
    public Camera shakeCamera;

    [Header("── Confetti (tuỳ chọn) ──")]
    public ConfettiController confetti;

    [Header("── Timing ──")]
    public float dimDuration = 0.4f;
    public float dimTargetAlpha = 0.55f;
    public float popupDelay = 0.25f;
    public float popupInDuration = 0.45f;

    private float _timeRemaining;
    private bool _timerRunning = false;
    private bool _isWarning = false;
    private System.Action _onTimeUp;

    private void Awake()
    {
        if (completePanelGroup != null)
        {
            completePanelGroup.alpha = 0f;
            completePanelGroup.interactable = false;
            completePanelGroup.blocksRaycasts = false;
        }
        if (losePanelGroup != null)
        {
            losePanelGroup.alpha = 0f;
            losePanelGroup.interactable = false;
            losePanelGroup.blocksRaycasts = false;
        }
        if (dimOverlay != null)
        {
            Color c = dimOverlay.color; c.a = 0f;
            dimOverlay.color = c;
            // Dim overlay chỉ là visual — không được block raycast bao giờ
            dimOverlay.raycastTarget = false;
        }
        if (timerText != null)
            timerText.color = normalColor;

        if (restartButtonLose != null)
        {
            restartButtonLose.onClick.AddListener(OnRestartClicked);
            Debug.Log("[UIManager] Awake: restartButtonLose listener gắn OK");
        }
        else
        {
            Debug.LogError("[UIManager] Awake: restartButtonLose là NULL — chưa gán trong Inspector!");
        }

        if (homeButton != null)
        {
            homeButton.onClick.AddListener(ExitToMainMenu);
            Debug.Log("[UIManager] Awake: homeButton listener gắn OK");
        }
    }

    void OnRestartClicked()
    {
        Debug.Log("[UIManager] OnRestartClicked — NÚT ĐÃ ĐƯỢC BẤM!");

        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.UpdateHeartsRecovery();
            if (PlayerDataManager.Instance.Data.hearts <= 0)
            {
                Debug.LogWarning("[UIManager] Hết tim! Không thể restart màn chơi. Quay lại Main Menu.");
                ExitToMainMenu();
                return;
            }
        }

        // Dừng idle pulse và ẩn lose panel
        if (restartButtonLose != null)
            restartButtonLose.transform.DOKill(false);

        if (losePanelGroup != null)
        {
            losePanelGroup.DOKill(false);
            losePanelGroup.alpha = 0f;
            losePanelGroup.interactable = false;
            losePanelGroup.blocksRaycasts = false;
        }

        // Reset dim overlay
        if (dimOverlay != null)
            dimOverlay.DOFade(0f, 0.25f).SetEase(Ease.OutQuad);

        LevelManager.Instance?.RestartLevel();
    }

    public void ExitToMainMenu()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.SaveData();
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
    }

    private void Update()
    {
        if (!_timerRunning) return;

        _timeRemaining -= Time.deltaTime;

        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            _timerRunning = false;
            UpdateTimerDisplay();
            _onTimeUp?.Invoke();
            return;
        }

        // Cảnh báo khi gần hết giờ
        if (!_isWarning && _timeRemaining <= warningThreshold)
        {
            _isWarning = true;
            if (timerText != null)
            {
                timerText.color = warningColor;
                // Pulse animation
                timerText.transform.DOKill(false);
                timerText.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, vibrato: 3)
                         .SetLoops(-1, LoopType.Restart);
            }
        }

        UpdateTimerDisplay();
    }

    public void StartCountdown(float duration, System.Action onTimeUp)
    {
        _timeRemaining = duration;
        _onTimeUp = onTimeUp;
        _timerRunning = true;
        _isWarning = false;

        if (timerText != null)
        {
            timerText.color = normalColor;
            timerText.transform.DOKill(false);
            timerText.transform.localScale = Vector3.one;
        }

        // Reset lose panel
        if (losePanelGroup != null)
        {
            losePanelGroup.DOKill(false);
            losePanelGroup.alpha = 0f;
            losePanelGroup.interactable = false;
            losePanelGroup.blocksRaycasts = false;
        }

        if (restartButtonLose != null)
            restartButtonLose.transform.DOKill(false);

        UpdateTimerDisplay();
    }

    public void StopTimer()
    {
        _timerRunning = false;
        if (timerText != null)
        {
            timerText.transform.DOKill(false);
            timerText.transform.localScale = Vector3.one;
        }
    }

    public float GetRemainingSeconds() => _timeRemaining;

    public void ShowLevelComplete()
    {
        StopTimer();

        Sequence seq = DOTween.Sequence();

        if (dimOverlay != null)
            seq.Append(dimOverlay.DOFade(dimTargetAlpha, dimDuration).SetEase(Ease.OutQuad));

        seq.AppendInterval(popupDelay);
        seq.AppendCallback(() =>
        {
            if (confetti != null) confetti.Play();
            if (completePanelRect != null)
                completePanelRect.localScale = Vector3.one * 0.7f;
        });

        if (completePanelGroup != null)
        {
            seq.Append(completePanelGroup.DOFade(1f, popupInDuration).SetEase(Ease.OutQuad));
            if (completePanelRect != null)
                seq.Join(completePanelRect.DOScale(1f, popupInDuration).SetEase(Ease.OutBack));
        }

        seq.OnComplete(() =>
        {
            if (completePanelGroup != null)
            {
                completePanelGroup.interactable = true;
                completePanelGroup.blocksRaycasts = true;
            }
        });
    }

    public void ShowLosePanel()
    {
        if (losePanelGroup == null)
        {
            Debug.LogWarning("[UIManager] losePanelGroup chưa gán trong Inspector!");
            return;
        }

        // Đảm bảo trạng thái ban đầu sạch
        losePanelGroup.DOKill(false);
        losePanelGroup.alpha = 0f;
        losePanelGroup.interactable = false;
        losePanelGroup.blocksRaycasts = false;

        if (losePanelRect != null)
            losePanelRect.localScale = Vector3.one * 0.7f;

        if (loseTitleRect != null)
        {
            loseTitleRect.localScale = Vector3.zero;
            loseTitleRect.DOKill(false);
        }

        if (restartButtonLose != null)
        {
            restartButtonLose.transform.DOKill(false);
            CanvasGroup btnCG = restartButtonLose.GetComponent<CanvasGroup>();
            if (btnCG == null) btnCG = restartButtonLose.gameObject.AddComponent<CanvasGroup>();
            btnCG.alpha = 0f;
            btnCG.interactable = true;
            btnCG.blocksRaycasts = true;
            restartButtonLose.transform.localScale = Vector3.one * 0.85f;
        }

        Sequence seq = DOTween.Sequence();

        // 1. Camera shake + dim overlay đồng thời
        seq.AppendCallback(() =>
        {
            if (shakeCamera != null)
                shakeCamera.DOShakePosition(0.35f, 0.12f, 18, 90f, false);
        });

        if (dimOverlay != null)
            seq.Append(dimOverlay.DOFade(loseDimTargetAlpha, dimDuration).SetEase(Ease.OutQuad));
        else
            seq.AppendInterval(dimDuration);

        seq.AppendInterval(popupDelay);

        // 2. Panel hiện ra
        seq.Append(losePanelGroup.DOFade(1f, 0.15f).SetEase(Ease.OutQuad));
        if (losePanelRect != null)
            seq.Join(losePanelRect.DOScale(1f, loseTitleInDuration).SetEase(Ease.OutBack));

        // 3. LOSE title: scale 0 → 1.18 → 1 + shake ngang
        if (loseTitleRect != null)
        {
            seq.Join(loseTitleRect
                .DOScale(1.18f, loseTitleInDuration * 0.7f)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    loseTitleRect
                        .DOScale(1f, loseTitleInDuration * 0.3f)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            // Shake ngang nhẹ sau khi settle
                            loseTitleRect.DOPunchPosition(
                                new Vector3(8f, 0f, 0f), 0.3f, vibrato: 6, elasticity: 0.5f);
                        });
                }));
        }

        // 4. Restart button trượt lên + fade in
        seq.AppendInterval(loseButtonDelay);
        seq.AppendCallback(() =>
        {
            if (restartButtonLose == null) return;

            CanvasGroup btnCG = restartButtonLose.GetComponent<CanvasGroup>();
            RectTransform btnRect = restartButtonLose.GetComponent<RectTransform>();

            // Slide up
            if (btnRect != null)
            {
                Vector2 origin = btnRect.anchoredPosition + Vector2.down * 16f;
                btnRect.anchoredPosition = origin;
                btnRect
                    .DOAnchorPosY(origin.y + 16f, loseButtonInDuration)
                    .SetEase(Ease.OutBack);
            }

            restartButtonLose.transform
                .DOScale(1f, loseButtonInDuration)
                .SetEase(Ease.OutBack);

            if (btnCG != null)
                btnCG.DOFade(1f, loseButtonInDuration * 0.8f).SetEase(Ease.OutQuad);
        });

        // 5. Sau khi xong → bật tương tác + idle pulse
        seq.AppendInterval(loseButtonInDuration);
        seq.OnComplete(() =>
        {
            Debug.Log("[UIManager] ShowLosePanel OnComplete — bật interactable");
            losePanelGroup.interactable = true;
            losePanelGroup.blocksRaycasts = true;

            // Scan tất cả parent CanvasGroup — nếu có cái nào interactable=false
            // thì button con bên trong sẽ KHÔNG bao giờ bấm được dù child đã set true.
            if (losePanelGroup != null)
            {
                CanvasGroup[] parents = losePanelGroup.GetComponentsInParent<CanvasGroup>(true);
                foreach (var cg in parents)
                {
                    if (!cg.interactable)
                    {
                        Debug.LogWarning($"[UIManager] Parent CanvasGroup '{cg.gameObject.name}' đang chặn tương tác! → Tự động sửa thành interactable=true");
                        cg.interactable = true;
                    }
                    if (!cg.blocksRaycasts)
                    {
                        Debug.LogWarning($"[UIManager] Parent CanvasGroup '{cg.gameObject.name}' blocksRaycasts=false! → Tự động sửa");
                        cg.blocksRaycasts = true;
                    }
                }
            }

            // Đảm bảo CanvasGroup của button không block click
            if (restartButtonLose != null)
            {
                CanvasGroup btnCG = restartButtonLose.GetComponent<CanvasGroup>();
                if (btnCG != null)
                {
                    btnCG.interactable = true;
                    btnCG.blocksRaycasts = true;
                    btnCG.alpha = 1f;
                }

                // Idle pulse nhẹ nhàng trên nút Restart
                restartButtonLose.transform.DOKill(false);
                restartButtonLose.transform
                    .DOScale(1.05f, loseButtonPulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        });
    }

    private void UpdateTimerDisplay()
    {
        if (timerText != null)
            timerText.text = FormatTime(_timeRemaining);
    }

    private static string FormatTime(float seconds)
    {
        int min = Mathf.FloorToInt(seconds / 60f);
        int sec = Mathf.FloorToInt(seconds % 60f);
        return $"{min:00}:{sec:00}";
    }

    public void SetLevelText(int levelIndex)
    {
        if (levelText != null)
        {
            levelText.text = $"Level {levelIndex + 1}";
        }
    }
}