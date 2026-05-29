using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TimeFreezeController : MonoBehaviour
{
    public static TimeFreezeController Instance { get; private set; }

    [Header("── UI Elements ──")]
    public Button freezeButton;

    public GameObject lockIndicator;

    public RectTransform clockIcon;

    [Header("── Level 3 Unlock ──")]
    public TimeFreezeRewardPanel rewardPanel;
    public TutorialFingerUI tutorialFinger;

    public RectTransform snowflakeTransform;
    public CanvasGroup snowflakeGroup;

    public CanvasGroup iceOverlayGroup;

    [Header("── Countdown Progress ──")]
    public Slider countdownSlider;

    public Image countdownFillImage;

    [Header("── Configuration ──")]
    public float freezeDuration = 10f;

    [Header("── Audio ──")]
    public AudioSource audioSource;
    public AudioClip   freezeActivateClip;  // âm thanh khi bấm nút đóng băng thời gian

    private bool _isFrozenActive = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Gán sự kiện cho nút bấm
        if (freezeButton != null)
        {
            freezeButton.onClick.AddListener(OnFreezeButtonClicked);
        }

        // Đảm bảo ban đầu trạng thái được reset gọn gàng
        ResetState();
        RefreshLockState();
    }

    private void OnDestroy()
    {
        if (freezeButton != null)
        {
            freezeButton.onClick.RemoveListener(OnFreezeButtonClicked);
        }
    }

    private void OnDisable()
    {
        ResetState();
    }

    public void RefreshLockState()
    {
        ResetState();

        int currentLevelIndex = 0;
        if (PlayerDataManager.Instance != null)
        {
            currentLevelIndex = PlayerDataManager.Instance.Data.currentLevelIndex;
        }
        else if (LevelManager.Instance != null)
        {
            currentLevelIndex = LevelManager.Instance.GetCurrentLevelIndex();
        }

        // Level 3 tương đương index >= 2
        bool isUnlocked = currentLevelIndex >= 2;

        if (lockIndicator != null)
        {
            lockIndicator.SetActive(!isUnlocked);
        }

        if (freezeButton != null)
        {
            // Nếu đã mở khóa thì cho phép tương tác, nếu chưa thì tắt tương tác
            freezeButton.interactable = isUnlocked;
        }
    }

    private void OnFreezeButtonClicked()
    {
        if (_isFrozenActive) return;

        // SFX: bấm nút đóng băng thời gian
        if (audioSource != null && freezeActivateClip != null)
            audioSource.PlayOneShot(freezeActivateClip);

        // Ẩn ngón tay tutorial nếu đang hiện
        if (tutorialFinger != null)
            tutorialFinger.Hide();

        // Bắt đầu chuỗi hiệu ứng đóng băng
        StartFreezeSequence();
    }

    public void PlayUnlockAnimation(RectTransform clockSource)
    {
        // Lưu unlock state vào JSON (PlayerDataManager)
        if (PlayerDataManager.Instance != null)
            PlayerDataManager.Instance.SetTimeFreezeUnlocked();
        else
        {
            // Fallback PlayerPrefs
            PlayerPrefs.SetInt(TimeFreezeRewardPanel.PrefKey, 1);
            PlayerPrefs.Save();
        }

        // Nếu không có lockIndicator hoặc source thì unlock thẳng
        if (clockSource == null || lockIndicator == null)
        {
            FinishUnlock();
            return;
        }

        // Tạo bản sao icon đồng hồ bay trên Canvas
        Canvas canvas = clockSource.GetComponentInParent<Canvas>();
        if (canvas == null) { FinishUnlock(); return; }

        // Tạo ghost icon để bay (dùng Image clone)
        UnityEngine.UI.Image srcImage = clockSource.GetComponent<UnityEngine.UI.Image>();
        GameObject ghost = new GameObject("ClockFlyGhost");
        ghost.transform.SetParent(canvas.transform, false);
        ghost.transform.SetAsLastSibling();

        RectTransform ghostRect = ghost.AddComponent<RectTransform>();
        ghostRect.sizeDelta = clockSource.sizeDelta;
        ghostRect.position  = clockSource.position;
        ghostRect.localScale = clockSource.lossyScale;

        if (srcImage != null)
        {
            UnityEngine.UI.Image ghostImage = ghost.AddComponent<UnityEngine.UI.Image>();
            ghostImage.sprite = srcImage.sprite;
            ghostImage.color  = srcImage.color;
            ghostImage.preserveAspect = true;
        }

        // Đích bay đến = vị trí lockIndicator
        Vector3 targetPos = lockIndicator.transform.position;

        // Sequence: bay đến lockIndicator → punch lockIndicator → ẩn lockIndicator → unlock
        Sequence seq = DOTween.Sequence();

        // Bay theo arc nhẹ dùng path
        Vector3 startPos   = ghostRect.position;
        Vector3 midPos     = (startPos + targetPos) * 0.5f + Vector3.up * 80f;
        Vector3[] path     = new Vector3[] { midPos, targetPos };

        seq.Append(
            ghostRect
                .DOPath(path, 0.55f, PathType.CatmullRom)
                .SetEase(Ease.InQuad)
        );
        seq.Join(
            ghostRect.DOScale(0.5f, 0.55f).SetEase(Ease.InQuad)
        );

        // Khi đến nơi: punch lockIndicator rồi ẩn
        seq.AppendCallback(() =>
        {
            Destroy(ghost);

            if (lockIndicator != null)
            {
                RectTransform lockRect = lockIndicator.GetComponent<RectTransform>();
                if (lockRect != null)
                {
                    lockRect.DOPunchScale(Vector3.one * 0.35f, 0.4f, vibrato: 8, elasticity: 1f)
                        .OnComplete(() => lockIndicator.SetActive(false));
                }
                else
                {
                    lockIndicator.SetActive(false);
                }
            }
        });

        // Đợi punch xong rồi finish unlock
        seq.AppendInterval(0.45f);
        seq.AppendCallback(() => FinishUnlock());
    }

    private void FinishUnlock()
    {
        // Ẩn lockIndicator (phòng trường hợp chưa ẩn)
        if (lockIndicator != null)
            lockIndicator.SetActive(false);

        // Mở khóa freeze button
        if (freezeButton != null)
        {
            freezeButton.interactable = true;

            // Glow pulse để thu hút chú ý
            freezeButton.transform.DOKill(false);
            freezeButton.transform
                .DOPunchScale(Vector3.one * 0.25f, 0.5f, vibrato: 6, elasticity: 1f);
        }

        // Hiện tutorial finger
        if (tutorialFinger != null && freezeButton != null)
        {
            RectTransform btnRect = freezeButton.GetComponent<RectTransform>();
            if (btnRect != null)
                tutorialFinger.Show(btnRect);
        }
    }

    private void StartFreezeSequence()
    {
        _isFrozenActive = true;

        if (freezeButton != null)
        {
            freezeButton.interactable = false; // Tạm thời khóa nút trong lúc đang đóng băng
        }

        // Tìm kiếm UIManager trong scene
        UIManager ui = FindFirstObjectByType<UIManager>();

        // Tạo Sequence chính để chạy tuần tự các bước hiệu ứng
        Sequence mainSequence = DOTween.Sequence();

        // Bước 1: Xuất hiện icon đồng hồ lắc lắc ở giữa màn hình rồi ẩn đi
        if (clockIcon != null)
        {
            mainSequence.AppendCallback(() =>
            {
                clockIcon.gameObject.SetActive(true);
                clockIcon.localScale = Vector3.one;
                clockIcon.localRotation = Quaternion.identity;
                clockIcon.DOKill();

                // Hiệu ứng giật kích thước và lắc xoay cơ học
                clockIcon.DOPunchRotation(new Vector3(0f, 0f, 25f), 0.7f, vibrato: 14, elasticity: 1f);
                clockIcon.DOPunchScale(Vector3.one * 0.22f, 0.7f, vibrato: 10, elasticity: 1f);
            });

            // Chờ cho hiệu ứng lắc lắc chạy hết
            mainSequence.AppendInterval(0.7f);

            // Ẩn biểu tượng đồng hồ đi
            mainSequence.AppendCallback(() =>
            {
                clockIcon.gameObject.SetActive(false);
            });
        }

        // Bước 2: Hiệu ứng bông tuyết phóng to từ nhỏ đến lớn mờ dần
        if (snowflakeTransform != null && snowflakeGroup != null)
        {
            mainSequence.AppendCallback(() =>
            {
                snowflakeTransform.gameObject.SetActive(true);
                snowflakeTransform.DOKill();
                snowflakeGroup.DOKill();

                snowflakeTransform.localScale = Vector3.one * 0.1f;
                snowflakeGroup.alpha = 1f;

                // Phóng to bông tuyết lên 3.5 lần
                snowflakeTransform.DOScale(3.5f, 0.85f).SetEase(Ease.OutQuad);
                // Đồng thời làm mờ dần bông tuyết về 0
                snowflakeGroup.DOFade(0f, 0.85f).SetEase(Ease.InCubic).OnComplete(() =>
                {
                    snowflakeTransform.gameObject.SetActive(false);
                });
            });

            // Chờ hiệu ứng bông tuyết chạy hết
            mainSequence.AppendInterval(0.85f);
        }

        // Bước 3: Thêm nền background có viền băng mờ dần hiện lên & chạy đếm ngược 10s
        mainSequence.AppendCallback(() =>
        {
            // Viền băng hiện lên
            if (iceOverlayGroup != null)
            {
                iceOverlayGroup.gameObject.SetActive(true);
                iceOverlayGroup.DOKill();
                iceOverlayGroup.alpha = 0f;
                iceOverlayGroup.DOFade(1f, 0.35f).SetEase(Ease.OutQuad);
            }

            // Thanh đếm ngược chạy
            if (countdownSlider != null)
            {
                countdownSlider.gameObject.SetActive(true);
                countdownSlider.DOKill();
                countdownSlider.value = 1f;
                countdownSlider.DOValue(0f, freezeDuration).SetEase(Ease.Linear);
            }

            if (countdownFillImage != null)
            {
                countdownFillImage.gameObject.SetActive(true);
                countdownFillImage.DOKill();
                countdownFillImage.fillAmount = 1f;
                countdownFillImage.DOFillAmount(0f, freezeDuration).SetEase(Ease.Linear);
            }

            // Đóng băng bộ đếm thời gian chính của màn chơi
            if (ui != null)
            {
                ui.SetTimeFrozen(true);
            }
        });

        // Chờ trong 10 giây thời gian đóng băng
        mainSequence.AppendInterval(freezeDuration);

        // Bước 4: Hết thời gian thì giải phóng đóng băng và khôi phục bình thường
        mainSequence.AppendCallback(() =>
        {
            EndFreeze(ui);
        });

        // Đặt ID cho sequence để có thể quản lý hoặc tắt sớm khi cần
        mainSequence.SetId("FreezeTimerDelay");
    }

    private void EndFreeze(UIManager ui)
    {
        // Ẩn nền viền băng mờ dần
        if (iceOverlayGroup != null)
        {
            iceOverlayGroup.DOKill();
            iceOverlayGroup.DOFade(0f, 0.45f).SetEase(Ease.InQuad).OnComplete(() =>
            {
                iceOverlayGroup.gameObject.SetActive(false);
            });
        }

        // Ẩn các thanh đếm ngược
        if (countdownSlider != null)
        {
            countdownSlider.gameObject.SetActive(false);
        }
        if (countdownFillImage != null)
        {
            countdownFillImage.gameObject.SetActive(false);
        }

        // Giải phóng đóng băng bộ đếm thời gian chính
        if (ui == null)
        {
            ui = FindFirstObjectByType<UIManager>();
        }
        if (ui != null)
        {
            ui.SetTimeFrozen(false);
        }

        _isFrozenActive = false;

        // Cho phép bấm lại nút đóng băng bình thường
        if (freezeButton != null)
        {
            freezeButton.interactable = true;
        }
    }

    public void ResetState()
    {
        DOTween.Kill("FreezeTimerDelay");

        if (clockIcon != null)
        {
            clockIcon.DOKill();
            clockIcon.gameObject.SetActive(false);
        }
        if (snowflakeTransform != null) snowflakeTransform.DOKill();
        if (snowflakeGroup != null) snowflakeGroup.DOKill();
        if (iceOverlayGroup != null) iceOverlayGroup.DOKill();
        if (countdownSlider != null) countdownSlider.DOKill();
        if (countdownFillImage != null) countdownFillImage.DOKill();

        if (snowflakeTransform != null)
        {
            snowflakeTransform.gameObject.SetActive(false);
        }

        if (iceOverlayGroup != null)
        {
            iceOverlayGroup.alpha = 0f;
            iceOverlayGroup.gameObject.SetActive(false);
        }

        if (countdownSlider != null)
        {
            countdownSlider.gameObject.SetActive(false);
        }

        if (countdownFillImage != null)
        {
            countdownFillImage.gameObject.SetActive(false);
        }

        UIManager ui = FindFirstObjectByType<UIManager>();
        if (ui != null)
        {
            ui.SetTimeFrozen(false);
        }

        _isFrozenActive = false;

        if (freezeButton != null)
        {
            freezeButton.interactable = true;
        }
    }
}
