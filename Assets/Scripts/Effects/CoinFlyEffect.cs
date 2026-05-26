using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class CoinFlyEffect : MonoBehaviour
{
    [Header("── Prefab & Pool ──")]
    [Tooltip("UI Image prefab coin (~40x40 px)")]
    public Image coinIconPrefab;
    [Tooltip("Canvas Screen Space Overlay chứa coin bay")]
    public Canvas targetCanvas;
    public int poolSize = 20;

    [Header("── HUD Target ──")]
    [Tooltip("RectTransform của HUD coin icon")]
    public RectTransform hudCoinRect;
    [Tooltip("TextMeshPro hiện số coin trong HUD")]
    public TextMeshProUGUI hudCoinText;

    [Header("── Motion ──")]
    public float flyDuration = 0.7f;
    public float staggerDelay = 0.08f;
    [Tooltip("Độ cao đỉnh cung Bezier (pixel)")]
    public float arcHeight = 280f;
    [Tooltip("Spread ngẫu nhiên từ điểm xuất phát (pixel)")]
    public float spreadRadius = 60f;

    [Header("── Trail Glow ──")]
    public bool useTrailGlow = true;
    public Image trailImagePrefab;

    [Header("── Audio ──")]
    public AudioSource audioSource;
    public AudioClip coinLandClip;

    private Queue<Image> _pool = new Queue<Image>();
    private int _hudDisplayedCoins;
    private int _hudTargetCoins;

    private void Awake()
    {
        PrewarmPool();
    }

    private void Start()
    {
        if (PlayerDataManager.Instance != null && hudCoinText != null)
        {
            hudCoinText.text = PlayerDataManager.Instance.Data.coins.ToString("N0");
        }
    }

    public void FlyCoins(RectTransform sourceRect, int count,
                         int startDisplayCount, int targetCount,
                         System.Action onComplete = null)
    {
        _hudDisplayedCoins = startDisplayCount;
        _hudTargetCoins = targetCount;
        UpdateHudText();

        StartCoroutine(SpawnCoinRoutine(sourceRect, count, onComplete));
    }

    private IEnumerator SpawnCoinRoutine(RectTransform source, int count,
                                          System.Action onComplete)
    {
        int landed = 0;

        for (int i = 0; i < count; i++)
        {
            yield return new WaitForSeconds(staggerDelay);

            Vector2 srcScreen = GetScreenCenter(source);
            Vector2 from = srcScreen + (Vector2)(Random.insideUnitCircle * spreadRadius);
            Vector2 to = GetScreenCenter(hudCoinRect);

            float midX = (from.x + to.x) * 0.5f + Random.Range(-80f, 80f);
            float midY = Mathf.Max(from.y, to.y) + arcHeight + Random.Range(-40f, 60f);
            Vector2 ctrl = new Vector2(midX, midY);

            Image coin = GetFromPool();
            if (coin == null) continue;

            coin.rectTransform.position = from;
            coin.rectTransform.localScale = Vector3.one * Random.Range(0.75f, 1.1f);
            coin.color = Color.white;
            coin.gameObject.SetActive(true);

            // Spin
            coin.rectTransform
                .DOLocalRotate(new Vector3(0f, 360f, 0f), flyDuration, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear);

            // Fly
            BezierFly(coin, from, ctrl, to, flyDuration, () =>
            {
                coin.rectTransform.DOKill(false);
                landed++;
                IncrementHud(count, landed);
                OnCoinLanded(coin);
                if (landed >= count) onComplete?.Invoke();
            });
        }
    }

    private void BezierFly(Image coin, Vector2 from, Vector2 control,
                            Vector2 to, float duration, System.Action onComplete)
    {
        int steps = 14;
        Vector3[] path = new Vector3[steps];
        for (int k = 0; k < steps; k++)
        {
            float t = k / (float)(steps - 1);
            float it = 1f - t;
            Vector2 pt = (it * it) * from + (2f * it * t) * control + (t * t) * to;
            path[k] = pt;
        }

        coin.rectTransform
            .DOPath(path, duration, PathType.CatmullRom)
            .SetEase(Ease.InQuad)
            .SetOptions(closePath: false)
            .OnComplete(() => onComplete?.Invoke());

        coin.DOFade(1f, 0.1f);
        DOVirtual.DelayedCall(duration * 0.75f, () =>
        {
            if (coin != null && coin.gameObject.activeSelf)
                coin.DOFade(0f, duration * 0.25f).SetEase(Ease.InQuad);
        });

        if (useTrailGlow && trailImagePrefab != null)
            SpawnTrail(coin, duration);
    }

    private void OnCoinLanded(Image coin)
    {
        ReturnToPool(coin);

        if (audioSource != null && coinLandClip != null)
            audioSource.PlayOneShot(coinLandClip, Random.Range(0.55f, 0.9f));

        if (hudCoinRect != null)
        {
            hudCoinRect.DOKill(true); 
            hudCoinRect.localScale = Vector3.one;
            hudCoinRect.DOPunchScale(Vector3.one * 0.15f, 0.25f, vibrato: 3, elasticity: 0.5f)
                       .SetEase(Ease.OutElastic);
        }
    }

    private void IncrementHud(int total, int landed)
    {
        int perCoin = Mathf.Max(1, Mathf.RoundToInt(
            (_hudTargetCoins - _hudDisplayedCoins) / (float)total));
        _hudDisplayedCoins = Mathf.Min(_hudDisplayedCoins + perCoin, _hudTargetCoins);
        if (landed >= total) _hudDisplayedCoins = _hudTargetCoins;
        UpdateHudText();
    }

    private void UpdateHudText()
    {
        if (hudCoinText != null)
            hudCoinText.text = _hudDisplayedCoins.ToString("N0");
    }

    private void PrewarmPool()
    {
        if (coinIconPrefab == null || targetCanvas == null) return;
        for (int i = 0; i < poolSize; i++)
        {
            Image img = Instantiate(coinIconPrefab, targetCanvas.transform);
            img.gameObject.SetActive(false);
            _pool.Enqueue(img);
        }
    }

    private Image GetFromPool()
    {
        if (_pool.Count > 0) return _pool.Dequeue();
        if (coinIconPrefab != null && targetCanvas != null)
        {
            Image img = Instantiate(coinIconPrefab, targetCanvas.transform);
            img.gameObject.SetActive(false);
            return img;
        }
        return null;
    }

    private void ReturnToPool(Image img)
    {
        img.DOKill(false);
        img.gameObject.SetActive(false);
        _pool.Enqueue(img);
    }

    private void SpawnTrail(Image followTarget, float duration)
    {
        Image trail = Instantiate(trailImagePrefab, targetCanvas.transform);
        trail.color = new Color(1f, 0.9f, 0.3f, 0.5f);
        trail.rectTransform.sizeDelta = followTarget.rectTransform.sizeDelta * 1.6f;
        StartCoroutine(TrailFollow(trail, followTarget, duration));
    }

    private IEnumerator TrailFollow(Image trail, Image leader, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && leader != null && leader.gameObject.activeSelf)
        {
            trail.rectTransform.position = Vector3.Lerp(
                trail.rectTransform.position, leader.rectTransform.position, 0.25f);
            trail.color = new Color(trail.color.r, trail.color.g, trail.color.b,
                                    Mathf.Lerp(0.5f, 0f, elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (trail != null) Destroy(trail.gameObject);
    }

    private Vector2 GetScreenCenter(RectTransform rt)
    {
        if (rt == null) return Vector2.zero;
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return new Vector2((corners[0].x + corners[2].x) * 0.5f,
                           (corners[0].y + corners[2].y) * 0.5f);
    }
}