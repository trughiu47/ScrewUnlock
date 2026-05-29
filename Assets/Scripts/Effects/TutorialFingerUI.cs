using UnityEngine;
using DG.Tweening;

public class TutorialFingerUI : MonoBehaviour
{
    public static TutorialFingerUI Instance { get; private set; }

    [Header("── Root ──")]
    public RectTransform fingerRoot;

    public CanvasGroup fingerGroup;

    [Header("── Bounce Animation ──")]
    public float bounceDistance = 14f;
    public float bounceDuration = 0.55f;
    public float offsetY = -60f;
    public float offsetX = 0f;

    [Header("── Fade Settings ──")]
    public float fadeInDuration  = 0.35f;
    public float fadeOutDuration = 0.25f;

    private bool _isVisible = false;
    private Tween _bounceTween;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Ẩn ngay từ đầu
        ForceHide();
    }

    private void OnDestroy()
    {
        _bounceTween?.Kill();
    }

    public void Show(RectTransform target)
    {
        if (_isVisible || fingerRoot == null || target == null) return;
        _isVisible = true;

        // Bật GameObject TRƯỚC để RectTransform layout hoạt động đúng
        gameObject.SetActive(true);

        // Lấy canvas gốc
        Canvas canvas = fingerRoot.GetComponentInParent<Canvas>();
        if (canvas == null) { gameObject.SetActive(false); _isVisible = false; return; }

        // Xác định camera UI (Overlay = null, Camera/World = worldCamera)
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        // Chuyển vị trí world của target sang screen point
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, target.position);

        // Chuyển screen point sang local point trong parent của fingerRoot
        RectTransform parentRect = fingerRoot.parent as RectTransform;
        if (parentRect == null) parentRect = canvas.GetComponent<RectTransform>();

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, screenPoint, uiCamera, out localPoint);

        // Áp dụng offset
        fingerRoot.anchoredPosition = new Vector2(localPoint.x + offsetX, localPoint.y + offsetY);

        // Fade in rồi bắt đầu bounce
        if (fingerGroup != null)
        {
            fingerGroup.alpha = 0f;
            fingerGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad)
                .OnComplete(StartBounce);
        }
        else
        {
            StartBounce();
        }
    }

    public void Hide()
    {
        if (!_isVisible) return;
        _isVisible = false;

        // Dừng bounce
        _bounceTween?.Kill();
        _bounceTween = null;

        if (fingerGroup != null)
        {
            fingerGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad)
                .OnComplete(() => gameObject.SetActive(false));
        }
        else
        {
            if (fingerRoot != null)
            {
                fingerRoot.DOScale(0f, fadeOutDuration).SetEase(Ease.InBack)
                    .OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void StartBounce()
    {
        if (fingerRoot == null) return;

        _bounceTween?.Kill();

        // Lưu vị trí gốc
        Vector2 basePos = fingerRoot.anchoredPosition;

        // Bounce lên/xuống loop vô hạn
        _bounceTween = fingerRoot
            .DOAnchorPosY(basePos.y + bounceDistance, bounceDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void ForceHide()
    {
        gameObject.SetActive(false);
        if (fingerGroup != null) fingerGroup.alpha = 0f;
        if (fingerRoot  != null) fingerRoot.localScale = Vector3.one;
    }
}
