using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIRewardPanel : MonoBehaviour
{
    [Header("── Panel ──")]
    public CanvasGroup panelGroup;
    public RectTransform panelRect;

    [Header("── Title ──")]
    public TextMeshProUGUI titleText;

    [Header("── Level Name ──")]
    public TextMeshProUGUI levelNameText;

    [Header("── Reward Coin ──")]
    public RectTransform coinIconRect;
    public Image coinGlowImage;
    public TextMeshProUGUI coinCountText;

    [Header("── Claim Button ──")]
    public Button claimButton;
    public RectTransform claimButtonRect;
    public Image claimSweepImage;

    [Header("── Coin Fly ──")]
    public CoinFlyEffect coinFlyEffect;
    public int coinFlyCount = 10;
    public int hudCurrentCoins = 0;

    [Header("── Audio ──")]
    public AudioSource audioSource;
    public AudioClip panelInClip;
    public AudioClip countUpTickClip;
    public AudioClip claimPressClip;

    [Header("── Timing ──")]
    public float panelInDuration = 0.5f;
    public float countUpDuration = 1.2f;
    public float claimSweepInterval = 2.8f;

    private int _rewardCoins;
    private System.Action _onClaimCallback;
    private Coroutine _claimSweepRoutine;
    private bool _claimed;

    private void Awake()
    {
        HidePanelImmediate();

        if (claimButton != null)
            claimButton.onClick.AddListener(OnClaimClicked);
    }

    public void Show(int rewardCoins, string levelName, System.Action onClaim)
    {
        // ✅ Reset hoàn toàn trước mỗi lần Show
        ResetPanel();

        _rewardCoins = rewardCoins;
        _onClaimCallback = onClaim;

        if (PlayerDataManager.Instance != null)
        {
            hudCurrentCoins = PlayerDataManager.Instance.Data.coins;
        }
        else
        {
            hudCurrentCoins = 0;
        }

        if (levelNameText != null) levelNameText.text = levelName;

        gameObject.SetActive(true);
        StartCoroutine(ShowSequence());
    }

    private void ResetPanel()
    {
        _claimed = false;

        // Kill tất cả tween đang chạy trên panel
        if (panelRect != null) panelRect.DOKill(false);
        if (panelGroup != null) panelGroup.DOKill(false);
        if (coinIconRect != null) coinIconRect.DOKill(false);
        if (coinGlowImage != null) coinGlowImage.DOKill(false);
        if (claimButtonRect != null) claimButtonRect.DOKill(false);
        if (claimSweepImage != null) claimSweepImage.DOKill(false);

        // Reset về trạng thái ẩn
        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }
        if (panelRect != null)
            panelRect.localScale = Vector3.one * 0.7f;

        // ✅ Reset nút claim
        if (claimButton != null)
            claimButton.interactable = true;
        if (claimButtonRect != null)
            claimButtonRect.localScale = Vector3.one;

        // Reset coin display
        if (coinCountText != null)
            coinCountText.text = "0";

        // Reset coin spin
        if (coinIconRect != null)
        {
            coinIconRect.localRotation = Quaternion.identity;
            coinIconRect.localScale = Vector3.one;
        }

        // Reset glow
        if (coinGlowImage != null)
        {
            var c = coinGlowImage.color; c.a = 0f;
            coinGlowImage.color = c;
        }

        // Reset sweep
        if (claimSweepImage != null)
        {
            var c = claimSweepImage.color; c.a = 0f;
            claimSweepImage.color = c;
        }

        // Stop coroutines từ lần trước
        StopAllCoroutines();
    }

    private void HidePanelImmediate()
    {
        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }
        if (panelRect != null)
            panelRect.localScale = Vector3.one * 0.7f;
        gameObject.SetActive(false);
    }

    private IEnumerator ShowSequence()
    {
        PlaySound(panelInClip);

        Sequence panelIn = DOTween.Sequence();
        if (panelGroup != null)
            panelIn.Append(panelGroup.DOFade(1f, panelInDuration).SetEase(Ease.OutQuad));
        if (panelRect != null)
            panelIn.Join(panelRect.DOScale(1f, panelInDuration).SetEase(Ease.OutBack, overshoot: 1.6f));

        yield return panelIn.WaitForCompletion();

        if (panelGroup != null)
        {
            panelGroup.interactable = true;
            panelGroup.blocksRaycasts = true;
            Debug.Log("[UIRewardPanel] panelGroup.interactable = true — claim button có thể bấm");

            // Scan tất cả parent CanvasGroup — nếu có cái nào interactable=false
            // thì claim button bên trong sẽ KHÔNG bao giờ bấm được.
            CanvasGroup[] parents = panelGroup.GetComponentsInParent<CanvasGroup>(true);
            foreach (var cg in parents)
            {
                if (!cg.interactable)
                {
                    Debug.LogWarning($"[UIRewardPanel] Parent CanvasGroup '{cg.gameObject.name}' đang chặn tương tác! → Tự động sửa thành interactable=true");
                    cg.interactable = true;
                }
                if (!cg.blocksRaycasts)
                {
                    Debug.LogWarning($"[UIRewardPanel] Parent CanvasGroup '{cg.gameObject.name}' blocksRaycasts=false → Tự động sửa");
                    cg.blocksRaycasts = true;
                }
            }
        }

        // Đảm bảo nút claim luôn có thể bấm được
        if (claimButton != null)
            claimButton.interactable = true;

        // Title punch
        if (titleText != null)
        {
            titleText.transform.localScale = Vector3.one;
            titleText.transform.DOPunchScale(Vector3.one * 0.18f, 0.45f, vibrato: 4, elasticity: 0.5f)
                               .SetEase(Ease.OutElastic);
        }

        yield return new WaitForSeconds(0.25f);

        // Coin spin loop
        if (coinIconRect != null)
        {
            coinIconRect.DOLocalRotate(new Vector3(0f, 360f, 0f), 1.5f, RotateMode.FastBeyond360)
                        .SetLoops(-1, LoopType.Restart)
                        .SetEase(Ease.InOutSine);
        }

        // Glow pulse
        if (coinGlowImage != null)
        {
            coinGlowImage.DOFade(0.2f, 0.8f)
                         .SetEase(Ease.InOutSine)
                         .SetLoops(-1, LoopType.Yoyo);
        }

        yield return StartCoroutine(CountUpCoins());

        StartClaimButtonIdle();
        _claimSweepRoutine = StartCoroutine(ClaimSweepLoop());
    }

    private IEnumerator CountUpCoins()
    {
        float elapsed = 0f;
        int lastTick = -1;
        AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        while (elapsed < countUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / countUpDuration);
            int displayed = Mathf.RoundToInt(curve.Evaluate(t) * _rewardCoins);

            if (coinCountText != null)
                coinCountText.text = displayed.ToString("N0");

            int step = Mathf.Max(1, _rewardCoins / 15);
            if (displayed != lastTick && displayed % step == 0)
            {
                PlaySound(countUpTickClip, Random.Range(0.3f, 0.7f));
                lastTick = displayed;
            }

            yield return null;
        }

        if (coinCountText != null)
            coinCountText.text = _rewardCoins.ToString("N0");

        if (coinIconRect != null)
            coinIconRect.DOPunchScale(Vector3.one * 0.25f, 0.35f, vibrato: 5, elasticity: 0.5f);
    }

    private void StartClaimButtonIdle()
    {
        if (claimButtonRect == null) return;
        claimButtonRect.DOScale(1.05f, 0.65f)
                       .SetEase(Ease.InOutSine)
                       .SetLoops(-1, LoopType.Yoyo);
    }

    private IEnumerator ClaimSweepLoop()
    {
        if (claimSweepImage == null) yield break;

        RectTransform sr = claimSweepImage.rectTransform;
        float btnW = claimButtonRect != null ? claimButtonRect.rect.width : 260f;

        while (!_claimed)
        {
            yield return new WaitForSeconds(claimSweepInterval + Random.Range(-0.2f, 0.4f));
            if (_claimed) yield break;

            sr.anchoredPosition = new Vector2(-btnW * 0.7f, sr.anchoredPosition.y);
            claimSweepImage.color = new Color(1f, 1f, 1f, 0f);

            Sequence s = DOTween.Sequence();
            s.Append(claimSweepImage.DOFade(0.75f, 0.08f));
            s.Join(sr.DOAnchorPosX(btnW * 0.7f, 0.35f).SetEase(Ease.OutQuad));
            s.Append(claimSweepImage.DOFade(0f, 0.08f));
        }
    }

    private void OnClaimClicked()
    {
        Debug.Log("[UIRewardPanel] OnClaimClicked — NÚT CLAIM ĐÃ ĐƯỢC BẤM!");
        if (_claimed) return;
        _claimed = true;

        PlaySound(claimPressClip);

        if (claimButton != null) claimButton.interactable = false;
        if (claimButtonRect != null) claimButtonRect.DOKill(false);
        if (_claimSweepRoutine != null) StopCoroutine(_claimSweepRoutine);

        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.AddCoins(_rewardCoins);
        }

        Sequence click = DOTween.Sequence();
        click.Append(claimButtonRect.DOScale(0.92f, 0.08f).SetEase(Ease.InQuad));
        click.Append(claimButtonRect.DOScale(1.12f, 0.18f).SetEase(Ease.OutBack));
        click.Append(claimButtonRect.DOScale(1.00f, 0.12f).SetEase(Ease.OutQuad));
        click.OnComplete(() =>
        {
            if (coinFlyEffect != null)
            {
                coinFlyEffect.FlyCoins(
                    panelRect,
                    coinFlyCount,
                    hudCurrentCoins,
                    hudCurrentCoins + _rewardCoins,
                    () => { FadeOutPanel(); _onClaimCallback?.Invoke(); }
                );
            }
            else
            {
                FadeOutPanel();
                _onClaimCallback?.Invoke();
            }
        });
    }

    private void FadeOutPanel()
    {
        if (panelGroup == null) return;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        if (coinIconRect != null) coinIconRect.DOKill(false);

        Sequence s = DOTween.Sequence();
        s.Append(panelRect.DOScale(0.88f, 0.25f).SetEase(Ease.InBack));
        s.Join(panelGroup.DOFade(0f, 0.3f).SetEase(Ease.InQuad));
        s.OnComplete(() => gameObject.SetActive(false));
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip, volume);
    }
}