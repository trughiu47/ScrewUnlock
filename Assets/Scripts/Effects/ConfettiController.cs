using System.Collections;
using UnityEngine;

public class ConfettiController : MonoBehaviour
{
    [Header("── Burst Settings ──")]
    public int particleCount = 180;
    public int burstCount = 3;
    public float burstInterval = 0.18f;

    [Header("── Motion ──")]
    public float minSpeed = 4f;
    public float maxSpeed = 12f;
    public float gravity = -6f;
    public float lifetime = 2.2f;

    [Header("── Size ──")]
    public float minSize = 0.08f;
    public float maxSize = 0.22f;

    [Header("── Colours ──")]
    public Color[] palette = new Color[]
    {
        new Color(1.00f, 0.30f, 0.30f),  // coral red
        new Color(1.00f, 0.75f, 0.10f),  // golden yellow
        new Color(0.20f, 0.80f, 0.40f),  // mint green
        new Color(0.20f, 0.60f, 1.00f),  // sky blue
        new Color(0.85f, 0.30f, 1.00f),  // violet
        new Color(1.00f, 0.55f, 0.10f),  // orange
    };

    private ParticleSystem _ps;

    private void Awake()
    {
        BuildParticleSystem();
    }

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(BurstRoutine());
    }

    public void Stop()
    {
        StopAllCoroutines();
        if (_ps != null)
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void BuildParticleSystem()
    {
        var psGO = new GameObject("ConfettiPS"); // hoặc "FireworkPS"
        psGO.transform.SetParent(transform, false);
        _ps = psGO.AddComponent<ParticleSystem>();

        _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = _ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = particleCount * burstCount + 10;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = 0.15f;
        main.gravityModifier = gravity / -9.81f;

        var emission = _ps.emission; emission.enabled = false;
        var shape = _ps.shape; shape.enabled = false;

        var rend = _ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.material = CreateMaterial();
        rend.sortingOrder = 40;

        var main2 = _ps.main;
        main2.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        rend.bounds = new Bounds(Vector3.zero, Vector3.one * 9999f);

        _ps.Play();
    }

    private IEnumerator BurstRoutine()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        for (int b = 0; b < burstCount; b++)
        {
            EmitBurst(cam);
            yield return new WaitForSeconds(burstInterval);
        }
    }

    private void EmitBurst(Camera cam)
    {
        float topY = cam.orthographicSize * 0.85f;
        float halfX = cam.orthographicSize * cam.aspect * 0.9f;

        for (int i = 0; i < particleCount; i++)
        {
            float spawnX = Random.Range(-halfX, halfX);
            float spawnY = topY + Random.Range(0f, cam.orthographicSize * 0.3f);
            Vector3 pos = new Vector3(spawnX, spawnY, 0f);

            float angle = Random.Range(-160f, -20f) * Mathf.Deg2Rad;
            float speed = Random.Range(minSpeed, maxSpeed);
            Vector3 vel = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed, 0f);

            Color col = palette.Length > 0
                ? palette[Random.Range(0, palette.Length)]
                : Color.white;

            _ps.Emit(new ParticleSystem.EmitParams
            {
                position = pos,
                velocity = vel,
                startSize = Random.Range(minSize, maxSize),
                startLifetime = Random.Range(lifetime * 0.6f, lifetime),
                startColor = col,
                rotation = Random.Range(0f, 360f),
                angularVelocity = Random.Range(-300f, 300f),
                applyShapeToPosition = false,
            }, 1);
        }
    }

    private static Material CreateMaterial()
    {
        Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
        return new Material(sh) { name = "ConfettiMat" };
    }
}