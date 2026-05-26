using System.Collections;
using UnityEngine;
using DG.Tweening;

public class FireworkController : MonoBehaviour
{
    [Header("── Spawn Settings ──")]
    [Tooltip("Số burst mỗi salvo")]
    public int burstsPerSalvo = 3;
    [Tooltip("Thời gian (giây) giữa các salvo")]
    public float salvoInterval = 0.65f;
    [Tooltip("Tổng số salvo (0 = vô hạn đến khi StopFireworks)")]
    public int totalSalvos = 0;
    [Tooltip("Số particle mỗi burst")]
    public int particlesPerBurst = 80;

    [Header("── Spread ──")]
    [Range(0.2f, 1.0f)]
    public float spreadX = 0.85f;
    public float spawnYMin = 0.30f;
    public float spawnYMax = 0.85f;

    [Header("── Particle Motion ──")]
    public float minSpeed = 3.5f;
    public float maxSpeed = 9.5f;
    public float gravity = -4f;
    public float particleLifetime = 1.8f;
    public float minSize = 0.04f;
    public float maxSize = 0.18f;

    [Header("── Colours ──")]
    public Color[] palette = new Color[]
    {
        new Color(1.00f, 0.90f, 0.20f),  // gold
        new Color(1.00f, 0.35f, 0.35f),  // red
        new Color(0.30f, 0.85f, 1.00f),  // cyan
        new Color(1.00f, 0.55f, 0.10f),  // orange
        new Color(0.75f, 0.30f, 1.00f),  // purple
        new Color(0.25f, 1.00f, 0.55f),  // mint
    };

    [Header("── Light Flash ──")]
    [Tooltip("Spawn flash ánh sáng mỗi burst")]
    public bool spawnFlash = true;
    public float flashDuration = 0.25f;

    private ParticleSystem _ps;
    private bool _running;
    private Coroutine _mainRoutine;
    private int _salvoCount;

    private void Awake()
    {
        BuildParticleSystem();
    }

    public void StartFireworks()
    {
        if (_running) return;
        _running = true;
        _salvoCount = 0;
        _mainRoutine = StartCoroutine(FireworkLoop());
    }

    public void StopFireworks()
    {
        _running = false;
        if (_mainRoutine != null) StopCoroutine(_mainRoutine);
    }

    private IEnumerator FireworkLoop()
    {
        while (_running)
        {
            FireSalvo();
            _salvoCount++;

            if (totalSalvos > 0 && _salvoCount >= totalSalvos)
            {
                _running = false;
                yield break;
            }

            yield return new WaitForSeconds(salvoInterval + Random.Range(-0.15f, 0.25f));
        }
    }

    private void FireSalvo()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        for (int i = 0; i < burstsPerSalvo; i++)
        {
            float stagger = i * Random.Range(0.04f, 0.18f);
            DOVirtual.DelayedCall(stagger, () => EmitBurst(cam));
        }
    }

    private void EmitBurst(Camera cam)
    {
        if (_ps == null) return;

        float halfX = cam.orthographicSize * cam.aspect * spreadX;
        float screenH = cam.orthographicSize * 2f;
        float spawnX = Random.Range(-halfX, halfX);
        float spawnY = -cam.orthographicSize + screenH * Random.Range(spawnYMin, spawnYMax);
        Vector3 origin = cam.transform.position + new Vector3(spawnX, spawnY, 0f);
        origin.z = 0f;

        Color burstColor = palette.Length > 0
            ? palette[Random.Range(0, palette.Length)]
            : Color.white;

        Color.RGBToHSV(burstColor, out float h, out float s, out float v);
        Color secondColor = Color.HSVToRGB((h + 0.08f) % 1f, s, v);

        for (int p = 0; p < particlesPerBurst; p++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float speed = Random.Range(minSpeed, maxSpeed);
            Vector3 vel = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed, 0f);
            Color col = Random.value > 0.4f ? burstColor : secondColor;

            _ps.Emit(new ParticleSystem.EmitParams
            {
                position = origin,
                velocity = vel,
                startSize = Random.Range(minSize, maxSize),
                startLifetime = Random.Range(particleLifetime * 0.6f, particleLifetime),
                startColor = col,
                rotation = Random.Range(0f, 360f),
                angularVelocity = Random.Range(-220f, 220f),
                applyShapeToPosition = false,
            }, 1);
        }

        if (spawnFlash) SpawnLightFlash(origin, burstColor);
    }

    private void SpawnLightFlash(Vector3 pos, Color col)
    {
        GameObject flashGO = new GameObject("FireworkFlash");
        flashGO.transform.position = pos;
        SpriteRenderer sr = flashGO.AddComponent<SpriteRenderer>();
        sr.color = new Color(col.r, col.g, col.b, 0.75f);
        sr.sortingOrder = 50;
        flashGO.transform.localScale = Vector3.one * 0.8f;

        Sequence fs = DOTween.Sequence();
        fs.Append(flashGO.transform.DOScale(2.5f, flashDuration * 0.5f).SetEase(Ease.OutQuad));
        fs.Join(DOTween.To(() => sr.color, c => sr.color = c,
                           new Color(col.r, col.g, col.b, 0f), flashDuration).SetEase(Ease.OutQuad));
        fs.OnComplete(() => Destroy(flashGO));
    }

    private void BuildParticleSystem()
    {
        var psGO = new GameObject("FireworkPS");
        psGO.transform.SetParent(transform, false);
        _ps = psGO.AddComponent<ParticleSystem>();
        _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = _ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = particlesPerBurst * burstsPerSalvo * 6 + 50;
        main.startLifetime = particleLifetime;
        main.startSpeed = 0f;
        main.startSize = 0.12f;
        main.gravityModifier = gravity / -9.81f;

        var emission = _ps.emission; emission.enabled = false;
        var shape = _ps.shape; shape.enabled = false;

        // Fade out cuối vòng đời
        var col = _ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f),
                                     new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f),
                                     new GradientAlphaKey(1f, 0.7f),
                                     new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // Thu nhỏ cuối vòng đời
        var sizeOL = _ps.sizeOverLifetime;
        sizeOL.enabled = true;
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.6f, 0.85f), new Keyframe(1f, 0.2f)));

        var rend = _ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.material = BuildMaterial();
        rend.sortingOrder = 40;

        _ps.Play(); 
    }

    private static Material BuildMaterial()
    {
        Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
        return new Material(sh) { name = "FireworkMat" };
    }
}