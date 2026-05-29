using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class MainMenuUIEffects : MonoBehaviour
{
    [Header("── HUD Entry Animation ──")]
    public RectTransform[] hudElements;
    public float hudStagger      = 0.08f;
    public float hudSlideDist    = 60f;   // pixel slide từ trên xuống
    public float hudDuration     = 0.45f;

    [Header("── Panel Pop ──")]
    public CanvasGroup settingsPanelGroup;
    public RectTransform settingsPanelRect;

    public CanvasGroup noHeartsPanelGroup;
    public RectTransform noHeartsPanelRect;

    [Header("── Dim Overlay ──")]
    public Image dimOverlay;
    [Range(0f, 1f)] public float dimAlpha = 0.5f;

    [Header("── Buttons cần hiệu ứng punch khi bấm ──")]
    public Button[] punchButtons;

    [Header("── Floating / Bounce Elements ──")]
    public RectTransform[] floatingElements;
    public float floatAmplitude  = 8f;    // pixel
    public float floatDuration   = 1.4f;  // giây một chu kỳ

    [Header("── Heart Icon Pulse ──")]
    public RectTransform heartIcon;
    public float heartPulseScale  = 1.12f;
    public float heartPulsePeriod = 1.8f;

    [Header("── Animation Timing ──")]
    public float panelOpenDuration  = 0.35f;
    public float panelCloseDuration = 0.25f;
    public float panelStartScale    = 0.80f;

    private void Awake()
    {
        // Ẩn panels ngay lập tức (tránh flash)
        HidePanelImmediate(settingsPanelGroup, settingsPanelRect);
        HidePanelImmediate(noHeartsPanelGroup, noHeartsPanelRect);

        if (dimOverlay != null)
        {
            var c = dimOverlay.color; c.a = 0f;
            dimOverlay.color = c;
            dimOverlay.raycastTarget = false;
        }
    }

    private void Start()
    {
        PlayHUDEntryAnimation();
        RegisterPunchButtons();
        StartFloatingAnimations();
        StartHeartPulse();
    }

    public void PlayHUDEntryAnimation()
    {
        if (hudElements == null) return;

        for (int i = 0; i < hudElements.Length; i++)
        {
            var rt = hudElements[i];
            if (rt == null) continue;

            // Lưu vị trí gốc
            Vector3 originPos = rt.anchoredPosition;
            // Bắt đầu ở trên cao
            rt.anchoredPosition = originPos + Vector3.up * hudSlideDist;

            // Lấy / thêm CanvasGroup để fade
            var cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            float delay = i * hudStagger;

            DOTween.Sequence()
                .SetDelay(delay)
                .Append(rt.DOAnchorPos(originPos, hudDuration).SetEase(Ease.OutBack))
                .Join(cg.DOFade(1f, hudDuration * 0.8f).SetEase(Ease.OutQuad));
        }
    }

    public void OpenSettingsPanel()
    {
        OpenPanel(settingsPanelGroup, settingsPanelRect);
    }

    public void CloseSettingsPanel()
    {
        ClosePanel(settingsPanelGroup, settingsPanelRect);
    }

    public void OpenNoHeartsPanel()
    {
        OpenPanel(noHeartsPanelGroup, noHeartsPanelRect);
    }

    public void CloseNoHeartsPanel()
    {
        ClosePanel(noHeartsPanelGroup, noHeartsPanelRect);
    }

    private void OpenPanel(CanvasGroup cg, RectTransform rt)
    {
        if (cg == null) return;

        DOTween.Kill(cg);
        if (rt != null) DOTween.Kill(rt);

        cg.interactable   = false;
        cg.blocksRaycasts = true;   // blok input ngay khi bắt đầu mở

        if (rt != null) rt.localScale = Vector3.one * panelStartScale;

        // Fade dim
        if (dimOverlay != null)
        {
            dimOverlay.raycastTarget = true;
            dimOverlay.DOFade(dimAlpha, panelOpenDuration * 0.6f).SetEase(Ease.OutQuad);
        }

        Sequence seq = DOTween.Sequence();
        seq.Append(cg.DOFade(1f, panelOpenDuration).SetEase(Ease.OutQuad));
        if (rt != null)
            seq.Join(rt.DOScale(1f, panelOpenDuration).SetEase(Ease.OutBack));
        seq.OnComplete(() =>
        {
            cg.interactable   = true;
            cg.blocksRaycasts = true;
        });
    }

    private void ClosePanel(CanvasGroup cg, RectTransform rt)
    {
        if (cg == null) return;

        DOTween.Kill(cg);
        if (rt != null) DOTween.Kill(rt);

        cg.interactable   = false;
        cg.blocksRaycasts = false;

        Sequence seq = DOTween.Sequence();
        seq.Append(cg.DOFade(0f, panelCloseDuration).SetEase(Ease.InQuad));
        if (rt != null)
            seq.Join(rt.DOScale(panelStartScale, panelCloseDuration).SetEase(Ease.InBack));
        seq.OnComplete(() =>
        {
            cg.blocksRaycasts = false;
            HideDim();
        });
    }

    private void HideDim()
    {
        if (dimOverlay == null) return;
        // Chỉ ẩn dim nếu cả 2 panel đều đã đóng
        bool settingsOpen = settingsPanelGroup != null && settingsPanelGroup.alpha > 0.01f;
        bool noHeartsOpen = noHeartsPanelGroup != null && noHeartsPanelGroup.alpha > 0.01f;

        if (!settingsOpen && !noHeartsOpen)
        {
            dimOverlay.DOFade(0f, panelCloseDuration).SetEase(Ease.OutQuad)
                .OnComplete(() => dimOverlay.raycastTarget = false);
        }
    }

    private void HidePanelImmediate(CanvasGroup cg, RectTransform rt)
    {
        if (cg != null)
        {
            cg.alpha          = 0f;
            cg.interactable   = false;
            cg.blocksRaycasts = false;
        }
        if (rt != null)
            rt.localScale = Vector3.one * panelStartScale;
    }

    private void RegisterPunchButtons()
    {
        if (punchButtons == null) return;
        foreach (var btn in punchButtons)
        {
            if (btn == null) continue;
            AddPunchOnClick(btn);
        }
    }

    public void AddPunchOnClick(Button btn)
    {
        if (btn == null) return;
        btn.onClick.AddListener(() =>
        {
            SfxManager.Instance?.PlayButtonClick();
            btn.transform.DOKill(false);
            btn.transform.DOPunchScale(Vector3.one * 0.18f, 0.25f, vibrato: 5, elasticity: 0.5f);
        });
    }

    public void PunchNow(Transform t, float strength = 0.18f)
    {
        if (t == null) return;
        t.DOKill(false);
        t.DOPunchScale(Vector3.one * strength, 0.25f, vibrato: 5, elasticity: 0.5f);
    }

    private void StartFloatingAnimations()
    {
        if (floatingElements == null) return;
        foreach (var rt in floatingElements)
        {
            if (rt == null) continue;

            float randomDelay = Random.Range(0f, floatDuration);
            Vector3 startPos  = rt.anchoredPosition;

            rt.DOAnchorPosY(startPos.y + floatAmplitude, floatDuration)
              .SetEase(Ease.InOutSine)
              .SetLoops(-1, LoopType.Yoyo)
              .SetDelay(randomDelay);
        }
    }

    private void StartHeartPulse()
    {
        if (heartIcon == null) return;

        heartIcon.DOScale(heartPulseScale, heartPulsePeriod * 0.4f)
                 .SetEase(Ease.InOutSine)
                 .SetLoops(-1, LoopType.Yoyo);
    }

    public void PlayHeartChangeEffect()
    {
        if (heartIcon == null) return;
        heartIcon.DOKill(false);
        heartIcon.DOPunchScale(Vector3.one * 0.3f, 0.35f, vibrato: 6)
                 .OnComplete(StartHeartPulse);
    }

    public void PlayCoinChangeEffect(Transform coinTransform)
    {
        if (coinTransform == null) return;
        coinTransform.DOKill(false);
        coinTransform.DOPunchScale(Vector3.one * 0.22f, 0.3f, vibrato: 5);
    }

    private void OnDestroy()
    {
        DOTween.KillAll(false);
    }
}
