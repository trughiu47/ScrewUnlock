using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class VictorySequenceController : MonoBehaviour
{
    [Header("── Phase 1: Overlay & Flash ──")]
    public CanvasGroup dimOverlayGroup;
    public Image whiteFlashImage;

    [Header("── Phase 1: Logo (Sprite Only) ──")]
    public RectTransform logoRect;
    public CanvasGroup logoGroup;
    public Image logoImage;
    public Image logoGlowImage;
    public ParticleSystem logoSparklePS;

    [Header("── Phase 1: Effects ──")]
    public FireworkController fireworkController;
    public ConfettiController confettiController;

    [Header("── Phase 2: Reward Panel ──")]
    public UIRewardPanel rewardPanel;

    [Header("── Camera ──")]
    public CameraShaker cameraShaker;

    [Header("── Audio ──")]
    public AudioSource audioSource;
    public AudioClip victoryImpactClip;
    public AudioClip fireworkPopClip;
    public AudioClip crowdCheerClip;
    public AudioClip logoAppearClip;

    [Header("── Timing ──")]
    public float slowMotionScale = 0.25f;
    public float slowMotionDuration = 0.18f;
    public float phase1Duration = 1.8f;

    private bool _isPlaying;

    public void TriggerVictory(int totalCoins, string levelName)
    {
        if (_isPlaying) return;
        _isPlaying = true;
        StartCoroutine(VictoryCoroutine(totalCoins, levelName));
    }

    public void ResetState()
    {
        StopAllCoroutines();

        if (dimOverlayGroup != null)
        {
            dimOverlayGroup.DOKill(false);
            dimOverlayGroup.alpha = 0f;
            dimOverlayGroup.interactable = false;
            dimOverlayGroup.blocksRaycasts = false;
        }
        if (logoGroup != null)
        {
            logoGroup.DOKill(false);
            logoGroup.alpha = 0f;
        }
        if (logoRect != null)
        {
            logoRect.DOKill(false);
            logoRect.localScale = Vector3.zero;
        }
        if (logoGlowImage != null)
        {
            logoGlowImage.DOKill(false);
            var c = logoGlowImage.color; c.a = 0f;
            logoGlowImage.color = c;
        }
        if (whiteFlashImage != null)
        {
            whiteFlashImage.DOKill(false);
            var c = whiteFlashImage.color; c.a = 0f;
            whiteFlashImage.color = c;
            whiteFlashImage.gameObject.SetActive(false);
        }

        if (fireworkController != null) fireworkController.StopFireworks();
        if (confettiController != null) confettiController.Stop();
        if (logoSparklePS != null) logoSparklePS.Stop();

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        _isPlaying = false;
    }

    private IEnumerator VictoryCoroutine(int totalCoins, string levelName)
    {
        yield return StartCoroutine(SlowMotionFlash());

        if (cameraShaker != null) cameraShaker.Shake(0.3f, 0.22f);
        PlaySound(victoryImpactClip, 1f);

        yield return StartCoroutine(Phase1_CelebrationIntro());

        HideLogo();
        yield return new WaitForSeconds(0.35f);

        if (rewardPanel != null)
            rewardPanel.Show(totalCoins, levelName, OnClaimPressed);

        DOVirtual.DelayedCall(1f, () =>
        {
            if (fireworkController != null) fireworkController.StopFireworks();
        });
    }

    private IEnumerator SlowMotionFlash()
    {
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * slowMotionScale;

        if (whiteFlashImage != null)
        {
            whiteFlashImage.gameObject.SetActive(true);
            Color c = whiteFlashImage.color; c.a = 0f;
            whiteFlashImage.color = c;

            DOTween.Sequence()
                .Append(whiteFlashImage.DOFade(0.95f, 0.05f).SetUpdate(true))
                .Append(whiteFlashImage.DOFade(0f, 0.25f).SetUpdate(true).SetEase(Ease.OutQuad))
                .SetUpdate(true);
        }

        float elapsed = 0f;
        while (elapsed < slowMotionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        DOTween.To(
            () => Time.timeScale,
            x => { Time.timeScale = x; Time.fixedDeltaTime = 0.02f * x; },
            1f, 0.3f
        ).SetUpdate(true).SetEase(Ease.OutQuad);

        yield return new WaitForSecondsRealtime(0.3f);
    }

    private IEnumerator Phase1_CelebrationIntro()
    {
        if (dimOverlayGroup != null)
        {
            // Overlay chỉ là visual — không block raycast bao giờ
            dimOverlayGroup.blocksRaycasts = false;
            dimOverlayGroup.interactable = false;
            dimOverlayGroup.DOFade(0.45f, 0.4f).SetEase(Ease.OutQuad);
        }

        yield return new WaitForSeconds(0.25f);

        ShowLogo();
        PlaySound(logoAppearClip, 1f);

        yield return new WaitForSeconds(0.15f);

        if (fireworkController != null) fireworkController.StartFireworks();
        if (confettiController != null) confettiController.Play();

        StartCoroutine(StaggeredFireworkSounds());

        if (cameraShaker != null)
            cameraShaker.SoftZoomIn(duration: 0.8f, amount: 0.08f);

        DOVirtual.DelayedCall(0.4f, () => PlaySound(crowdCheerClip, 0.55f));

        yield return new WaitForSeconds(phase1Duration);
    }

    private void ShowLogo()
    {
        if (logoRect == null) return;

        logoRect.localScale = Vector3.zero;
        if (logoGroup != null) { logoGroup.DOKill(false); logoGroup.alpha = 1f; }
        if (logoImage != null) { logoImage.gameObject.SetActive(true); logoImage.color = Color.white; }

        Sequence s = DOTween.Sequence();
        s.Append(logoRect.DOScale(1.25f, 0.45f).SetEase(Ease.OutBack, overshoot: 2.5f));
        s.Append(logoRect.DOScale(1.00f, 0.18f).SetEase(Ease.OutQuad));
        s.Join(logoRect.DOScaleX(1.12f, 0.22f).SetEase(Ease.OutQuad).SetDelay(0.4f));
        s.Append(logoRect.DOScaleX(1.00f, 0.12f).SetEase(Ease.OutQuad));
        s.OnComplete(() =>
        {
            logoRect.DOLocalMoveY(logoRect.localPosition.y + 14f, 1.4f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
        });

        if (logoGlowImage != null)
        {
            logoGlowImage.gameObject.SetActive(true);
            logoGlowImage.color = new Color(1f, 1f, 0.5f, 0f);
            logoGlowImage.DOFade(0.65f, 0.6f).SetDelay(0.35f).SetEase(Ease.OutQuad);
            DOVirtual.DelayedCall(0.5f, () =>
            {
                logoGlowImage.DOFade(0.25f, 0.9f)
                             .SetEase(Ease.InOutSine)
                             .SetLoops(-1, LoopType.Yoyo);
            });
        }

        if (logoSparklePS != null)
            DOVirtual.DelayedCall(0.35f, () => logoSparklePS.Play());
    }

    private void HideLogo()
    {
        if (logoRect != null) logoRect.DOKill(false);
        if (logoGroup != null) logoGroup.DOFade(0f, 0.5f).SetEase(Ease.InQuad);
        if (logoGlowImage != null) { logoGlowImage.DOKill(false); logoGlowImage.DOFade(0f, 0.3f); }
        if (logoSparklePS != null) logoSparklePS.Stop();
    }

    private IEnumerator StaggeredFireworkSounds()
    {
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(Random.Range(0.05f, 0.55f));
            PlaySound(fireworkPopClip, Random.Range(0.5f, 0.9f));
        }
    }

    private void OnClaimPressed()
    {
        StartCoroutine(ClaimAndTransition());
    }

    private IEnumerator ClaimAndTransition()
    {
        yield return new WaitForSeconds(1.8f);

        if (confettiController != null) confettiController.Stop();
        if (fireworkController != null) fireworkController.StopFireworks();
        if (logoGroup != null) logoGroup.DOFade(0f, 0.4f);
        if (cameraShaker != null) cameraShaker.ResetZoom(0.4f);

        yield return new WaitForSeconds(0.5f);

        if (LevelManager.Instance != null && PlayerDataManager.Instance != null)
        {
            int currentIdx = LevelManager.Instance.GetCurrentLevelIndex();
            int totalLevels = LevelManager.Instance.GetTotalLevels();
            int nextIdx = (currentIdx + 1 < totalLevels) ? currentIdx + 1 : currentIdx;
            PlayerDataManager.Instance.SetLevel(nextIdx); // SetLevel đã tự gọi SaveData()
        }
        else if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.SaveData();
        }

        if (dimOverlayGroup != null)
        {
            dimOverlayGroup.DOKill(false);
            dimOverlayGroup.blocksRaycasts = false;
            dimOverlayGroup.interactable = false;
            dimOverlayGroup.DOFade(1f, 0.5f).SetEase(Ease.InQuad).OnComplete(() =>
            {
                LoadingPanelController.SkipNextLoading = true;
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
            });
        }
        else
        {
            LoadingPanelController.SkipNextLoading = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
        }
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip, volume);
    }
}