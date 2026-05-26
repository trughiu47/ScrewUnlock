using UnityEngine;
using DG.Tweening;

public class ScrewController : MonoBehaviour
{
    public bool IsLocked { get; private set; } = true;

    [Header("Pop & Spin")]
    [SerializeField] float popHeight = 0.6f;   // chieu cao bat len
    [SerializeField] float popDuration = 0.20f;  // thoi gian bat len
    [SerializeField] float spinSpeed = 1080f;  // do/giay xoay quanh truc Y

    [Header("Fall")]
    [SerializeField] float holdDuration = 0.08f;  // tam dung nho o dinh
    [SerializeField] float fallDepth = 3.0f;   // chieu sau roi xuong
    [SerializeField] float fallDuration = 0.30f;  // thoi gian roi

    [Header("Trail")]
    [SerializeField] float trailTime = 0.18f;  // TrailRenderer.time
    [SerializeField] float trailStartWidth = 0.08f;
    [SerializeField] float trailEndWidth = 0.0f;

    [Header("Drag Spin")]
    [SerializeField] float dragDegreesPerUnit = 360f; // bao nhieu do xoay / 1 don vi keo

    // Runtime
    TrailRenderer trail;
    bool spinning = false;  // spin tu do khi unlock
    float dragAngleY = 0f;     // goc xoay tich luy khi block dang bi keo

    // ── Drag-driven rotation (goi tu BlockController) ─────────────────────
    /// <summary>
    /// Goi moi frame khi block dang bi keo. dragDelta1D = do dich chuyen
    /// doc theo slide axis (world units, co dau).
    /// Screw xoay tai cho theo chieu kim dong ho khi keo ra (+).
    /// </summary>
    public void OnDragUpdate(float dragDelta1D)
    {
        if (!IsLocked) return;
        dragAngleY += dragDelta1D * dragDegreesPerUnit;
        transform.localRotation = Quaternion.Euler(0f, dragAngleY, 0f);
    }

    /// <summary>Goi khi nguoi choi tha tay — screw dung lai tai cho.</summary>
    public void OnDragRelease()
    {
        if (!IsLocked) return;
        // Giu nguyen goc hien tai, khong lam gi them
    }

    void Update()
    {
        if (spinning)
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
    }

    public void Unlock()
    {
        if (!IsLocked) return;
        IsLocked = false;

        // Tach khoi board parent de bay tu do trong world space
        transform.SetParent(null);

        // Tao TrailRenderer dong va lay mau tu MeshRenderer
        SetupTrail();

        Vector3 startPos = transform.position;
        float topY = startPos.y + popHeight;

        // Cu ly bay nhẹ nhàng huớng về camera để giữ screw luôn nằm trong tầm nhìn
        float randomXOffset = Random.Range(-0.4f, 0.4f);
        float targetZPop = startPos.z - 0.15f;    // Nhích nhẹ về phía camera khi nảy lên
        float targetZFall = startPos.z - 1.80f;   // Lao nhẹ về phía camera khi rơi xuống để nằm trong màn hình

        Sequence seq = DOTween.Sequence();

        // 1. Bat len va nhich nhe ve phia camera (pop)
        seq.Append(transform.DOMoveY(topY, popDuration).SetEase(Ease.OutQuad));
        seq.Join(transform.DOMoveZ(targetZPop, popDuration).SetEase(Ease.OutQuad));
        seq.Join(transform.DOMoveX(startPos.x + randomXOffset * 0.15f, popDuration).SetEase(Ease.OutQuad));

        // 2. Tam dung o dinh de tao cam giac lo xo / pop luc luc
        seq.AppendInterval(holdDuration);

        // 3. Roi xuong: lao tu tu ve phia Z am (camera/duoi man hinh) va roi xuong duoi Y- (co trong luc)
        seq.Append(transform.DOMoveY(startPos.y - fallDepth, fallDuration).SetEase(Ease.InQuad));
        seq.Join(transform.DOMoveZ(targetZFall, fallDuration).SetEase(Ease.InQuad));
        seq.Join(transform.DOMoveX(startPos.x + randomXOffset * 0.8f, fallDuration).SetEase(Ease.OutQuad));

        // 4. Scale nho ve 0 trong qua trinh roi
        seq.Join(
            transform.DOScale(Vector3.zero, fallDuration * 0.6f)
                     .SetEase(Ease.InBack)
                     .SetDelay(fallDuration * 0.4f)
        );

        // Bat xoay tu do quanh truc self
        spinning = true;

        // Tat xoay truoc khi destroy
        seq.OnComplete(() =>
        {
            spinning = false;
            Destroy(gameObject);
        });
    }

    void SetupTrail()
    {
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = trailTime;
        trail.startWidth = trailStartWidth;
        trail.endWidth = trailEndWidth;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.generateLightingData = false;

        Color screwColor = Color.white;
        var meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
            screwColor = meshRenderer.sharedMaterial.color;

        var mat = new Material(Shader.Find("Sprites/Default"));
        trail.material = mat;

        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(screwColor, 0f),
                new GradientColorKey(screwColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.85f, 0f),
                new GradientAlphaKey(0f,    1f)
            }
        );
        trail.colorGradient = gradient;
    }
}