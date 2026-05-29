using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class TimeFreezeRewardPanel : MonoBehaviour
{
    public static TimeFreezeRewardPanel Instance { get; private set; }

    [Header("── Panel Root ──")]
    public CanvasGroup panelGroup;

    public RectTransform panelRect;

    [Header("── Dim Overlay ──")]
    public Image dimOverlay;
    [Range(0f, 1f)] public float dimTargetAlpha = 0.55f;

    [Header("── Icon & Button ──")]
    public RectTransform clockIconInPanel;

    public Button claimButton;

    [Header("── Animation Settings ──")]
    public float openDuration  = 0.45f;
    public float closeDuration = 0.3f;
    public float dimDuration   = 0.3f;

    public const string PrefKey = "TimeFreezeUnlocked";


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Ẩn panel ngay từ đầu
        ForceHide();

        if (claimButton != null)
            claimButton.onClick.AddListener(OnClaimClicked);
    }

    private void OnDestroy()
    {
        if (claimButton != null)
            claimButton.onClick.RemoveListener(OnClaimClicked);
    }

    public static bool ShouldShow()
    {
        // Ưu tiên kiểm tra JSON (PlayerDataManager)
        if (PlayerDataManager.Instance != null)
            return !PlayerDataManager.Instance.IsTimeFreezeUnlocked();

        // Fallback: PlayerPrefs (dự phòng)
        return PlayerPrefs.GetInt(PrefKey, 0) == 0;
    }

    public void Show()
    {
        if (panelGroup == null) return;

        // Reset trạng thái ban đầu
        panelGroup.alpha          = 0f;
        panelGroup.interactable   = false;
        panelGroup.blocksRaycasts = true; // Block raycast ngay để người chơi không tương tác game phía sau

        if (panelRect != null)
            panelRect.localScale = Vector3.one * 0.72f;

        if (dimOverlay != null)
        {
            Color c = dimOverlay.color; c.a = 0f;
            dimOverlay.color = c;
            dimOverlay.raycastTarget = true;
        }

        // Animation chuỗi
        Sequence seq = DOTween.Sequence();

        // 1. Fade dim overlay
        if (dimOverlay != null)
            seq.Append(dimOverlay.DOFade(dimTargetAlpha, dimDuration).SetEase(Ease.OutQuad));
        else
            seq.AppendInterval(dimDuration * 0.5f);

        // 2. Fade + scale panel
        seq.Append(panelGroup.DOFade(1f, openDuration).SetEase(Ease.OutQuad));
        if (panelRect != null)
            seq.Join(panelRect.DOScale(1f, openDuration).SetEase(Ease.OutBack));

        // 3. Idle pulse nhẹ trên claim button
        seq.OnComplete(() =>
        {
            panelGroup.interactable = true;
            AnimateClaimButtonIdle();
        });
    }

    private void OnClaimClicked()
    {
        if (claimButton != null)
        {
            claimButton.interactable = false;

            // Punch scale trên nút
            claimButton.transform.DOKill(false);
            claimButton.transform
                .DOPunchScale(Vector3.one * 0.18f, 0.2f, vibrato: 5)
                .OnComplete(() => StartClaimSequence());
        }
        else
        {
            StartClaimSequence();
        }
    }

    private void StartClaimSequence()
    {
        // Dừng idle pulse của claim button
        if (claimButton != null)
            claimButton.transform.DOKill(false);

        // Đóng panel trước, rồi trigger animation unlock
        ClosePanel(afterClose: () =>
        {
            // Gọi TimeFreezeController để chạy unlock animation
            if (TimeFreezeController.Instance != null)
            {
                TimeFreezeController.Instance.PlayUnlockAnimation(clockIconInPanel);
            }
            else
            {
                // Fallback nếu không có controller: chỉ lưu unlock state
                PlayerPrefs.SetInt(PrefKey, 1);
                PlayerPrefs.Save();
            }
        });
    }

    private void ClosePanel(System.Action afterClose = null)
    {
        if (panelGroup == null) { afterClose?.Invoke(); return; }

        panelGroup.interactable   = false;
        panelGroup.blocksRaycasts = false;

        Sequence seq = DOTween.Sequence();

        // Fade + scale out panel
        seq.Append(panelGroup.DOFade(0f, closeDuration).SetEase(Ease.InQuad));
        if (panelRect != null)
            seq.Join(panelRect.DOScale(0.72f, closeDuration).SetEase(Ease.InBack));

        // Fade out dim overlay
        if (dimOverlay != null)
            seq.Append(dimOverlay.DOFade(0f, dimDuration * 0.7f).SetEase(Ease.OutQuad));

        seq.OnComplete(() =>
        {
            if (dimOverlay != null)
                dimOverlay.raycastTarget = false;
            afterClose?.Invoke();
        });
    }

    private void ForceHide()
    {
        if (panelGroup != null)
        {
            panelGroup.alpha          = 0f;
            panelGroup.interactable   = false;
            panelGroup.blocksRaycasts = false;
        }
        if (dimOverlay != null)
        {
            Color c = dimOverlay.color; c.a = 0f;
            dimOverlay.color = c;
            dimOverlay.raycastTarget = false;
        }
    }

    private void AnimateClaimButtonIdle()
    {
        if (claimButton == null) return;
        claimButton.transform.DOKill(false);
        claimButton.transform
            .DOScale(1.06f, 0.65f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

}
