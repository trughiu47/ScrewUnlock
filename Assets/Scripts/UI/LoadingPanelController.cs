using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LoadingPanelController : MonoBehaviour
{
    public static bool SkipNextLoading = false;

    [Header("── Panel ──")]
    [SerializeField] private CanvasGroup panelGroup;

    [Header("── Slider ──")]
    [SerializeField] private Slider loadingSlider;

    [Header("── Timing ──")]
    [Tooltip("Tổng thời gian fill slider (giây)")]
    [SerializeField] private float loadDuration = 2.5f;

    [Tooltip("Dừng lại bao lâu sau khi slider đầy trước khi fade out")]
    [SerializeField] private float holdAfterFull = 0.3f;

    [Tooltip("Thời gian fade out panel")]
    [SerializeField] private float fadeOutDuration = 0.4f;

    private void Awake()
    {
        if (panelGroup != null)
        {
            panelGroup.alpha = 1f;
            panelGroup.interactable = true;
            panelGroup.blocksRaycasts = true;
        }

        if (loadingSlider != null)
            loadingSlider.value = 0f;
    }

    private void Start()
    {
        if (SkipNextLoading)
        {
            SkipNextLoading = false;
            HideImmediate();
            return;
        }

        StartCoroutine(PlayLoading());
    }

    private void HideImmediate()
    {
        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }

    private IEnumerator PlayLoading()
    {
        float elapsed = 0f;

        while (elapsed < loadDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / loadDuration);

            float smoothT = t * t * (3f - 2f * t);

            if (loadingSlider != null)
                loadingSlider.value = smoothT;

            yield return null;
        }

        if (loadingSlider != null)
            loadingSlider.value = 1f;

        yield return new WaitForSecondsRealtime(holdAfterFull);

        if (panelGroup != null)
        {
            panelGroup.DOFade(0f, fadeOutDuration)
                .SetUpdate(true)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    panelGroup.interactable = false;
                    panelGroup.blocksRaycasts = false;
                    gameObject.SetActive(false);
                });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
