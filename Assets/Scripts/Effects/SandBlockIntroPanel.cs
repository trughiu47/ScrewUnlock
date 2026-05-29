using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SandBlockIntroPanel : MonoBehaviour
{
    public static SandBlockIntroPanel Instance { get; private set; }

    [Header("── Panel Root ──")]
    public CanvasGroup panelGroup;
    public RectTransform panelRect;

    [Header("── Dim Overlay ──")]
    public Image dimOverlay;
    [Range(0f, 1f)] public float dimTargetAlpha = 0.55f;

    [Header("── Button ──")]
    public Button okButton;

    [Header("── Animation Settings ──")]
    public float openDuration  = 0.45f;
    public float closeDuration = 0.3f;
    public float dimDuration   = 0.3f;

    public const string PrefKey = "SandBlockIntroduced";


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Ẩn panel ngay từ đầu
        ForceHide();

        if (okButton != null)
            okButton.onClick.AddListener(OnOkClicked);
    }

    private void OnDestroy()
    {
        if (okButton != null)
            okButton.onClick.RemoveListener(OnOkClicked);
    }

    public static bool ShouldShow()
    {
        // Ưu tiên kiểm tra JSON (PlayerDataManager)
        if (PlayerDataManager.Instance != null)
            return !PlayerDataManager.Instance.IsSandBlockIntroduced();

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

        // 3. Idle pulse nhẹ trên ok button
        seq.OnComplete(() =>
        {
            panelGroup.interactable = true;
            AnimateOkButtonIdle();
        });
    }

    private void OnOkClicked()
    {
        if (okButton != null)
        {
            okButton.interactable = false;

            // Punch scale trên nút
            okButton.transform.DOKill(false);
            okButton.transform
                .DOPunchScale(Vector3.one * 0.18f, 0.2f, vibrato: 5)
                .OnComplete(() => StartCloseSequence());
        }
        else
        {
            StartCloseSequence();
        }
    }

    private void StartCloseSequence()
    {
        // Dừng idle pulse của ok button
        if (okButton != null)
            okButton.transform.DOKill(false);

        // Đóng panel trước, rồi lưu trạng thái đã xem
        ClosePanel(afterClose: () =>
        {
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.SetSandBlockIntroduced();
            }
            else
            {
                // Fallback nếu không có manager: chỉ lưu prefs
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

    private void AnimateOkButtonIdle()
    {
        if (okButton == null) return;
        okButton.transform.DOKill(false);
        okButton.transform
            .DOScale(1.06f, 0.65f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }
}
