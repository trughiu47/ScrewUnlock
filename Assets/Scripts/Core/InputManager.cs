using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Xu ly input chuot (Editor/PC) va touch (mobile).
/// Su dung Old Input System (Project Settings > Player > Active Input Handling = Input Manager).
/// Truyen toan bo delta 2D (XZ) sang BlockController, de Block tu loc theo truc cua no.
/// </summary>
public class InputManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Camera mainCam;

    [Header("Settings")]
    [SerializeField] float dragSensitivity = 1.0f;
    [Tooltip("Nguong pixel de bat dau tinh la drag (tranh click nham)")]
    [SerializeField] float startDragPixels = 8f;

    // ── State ──────────────────────────────────────────────────────────────
    BlockController dragging;
    Vector3         lastWorldPos;
    Vector2         pressScreenPos;
    bool            isDragging;

    // Track finger ID de tranh nham ngon tay khi multi-touch
    int             activeFingerId = -1;

    // ── Unity ──────────────────────────────────────────────────────────────
    void Awake()
    {
        if (mainCam == null) mainCam = Camera.main;

        // Old Input System + StandaloneInputModule:
        // simulateMouseWithTouches = true (mặc định Unity) → EventSystem nhận được touch
        // và kích hoạt Button.onClick bình thường trên mobile.
        // KHÔNG đặt = false ở đây, vì sẽ làm mất khả năng bấm UI Button.
    }

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouse();
#else
        HandleTouch();
#endif
    }

    // ── Kiem tra co dang cham vao UI khong ─────────────────────────────────
    // Neu dung => InputManager se bo qua, de EventSystem xu ly UI Button.
    static bool IsPointerOverUI(int fingerId = -1)
    {
        if (EventSystem.current == null) return false;
#if UNITY_EDITOR || UNITY_STANDALONE
        return EventSystem.current.IsPointerOverGameObject();
#else
        if (fingerId >= 0)
            return EventSystem.current.IsPointerOverGameObject(fingerId);
        // fallback cho mouse-simulated touch
        return EventSystem.current.IsPointerOverGameObject(-1);
#endif
    }

    // ── Mouse (Editor / PC only) ───────────────────────────────────────────
    void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI()) return;
            pressScreenPos = Input.mousePosition;
            TryPickBlock(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && dragging != null)
        {
            if (!isDragging)
            {
                float dist = Vector2.Distance(Input.mousePosition, pressScreenPos);
                if (dist > startDragPixels)
                {
                    isDragging = true;
                    dragging.OnPickUp();
                    lastWorldPos = ScreenToWorldPlane(Input.mousePosition);
                }
            }
            if (isDragging) MoveBlock(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            ReleaseBlock();
        }
    }

    // ── Touch (Mobile) ─────────────────────────────────────────────────────
    void HandleTouch()
    {
        int count = Input.touchCount;
        if (count == 0) return;

        // --- Xu ly ngon tay dang active ---
        if (activeFingerId >= 0)
        {
            Touch? activeTouch = FindTouch(activeFingerId);

            // Ngon tay nhat len hoac bi mat (Ended / Canceled / khong con trong danh sach)
            if (activeTouch == null ||
                activeTouch.Value.phase == TouchPhase.Ended ||
                activeTouch.Value.phase == TouchPhase.Canceled)
            {
                ReleaseBlock();
                activeFingerId = -1;
                return;
            }

            Touch t = activeTouch.Value;

            if (!isDragging)
            {
                float dist = Vector2.Distance(t.position, pressScreenPos);
                if (dist > startDragPixels)
                {
                    isDragging   = true;
                    dragging?.OnPickUp();
                    lastWorldPos = ScreenToWorldPlane(t.position);
                }
            }

            if (isDragging && t.phase == TouchPhase.Moved)
            {
                // Dung deltaPosition cua touch thay vi tinh lai tu world
                // deltaPosition duoc Unity accumulate tu tat ca sub-frame samples
                // => mượt hon nhieu tren man hinh 90/120Hz
                MoveDelta(t.deltaPosition);
            }

            return;
        }

        // --- Bat dau ngon tay moi (chua co active finger) ---
        for (int i = 0; i < count; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Began)
            {
                // Neu cham vao UI (Button, Panel...) thi bo qua,
                // de EventSystem xu ly click binh thuong.
                if (IsPointerOverUI(t.fingerId))
                    break;

                pressScreenPos = t.position;
                activeFingerId = t.fingerId;
                TryPickBlock(t.position);
                break; // Chi quan tam ngon tay dau tien cham vao
            }
        }
    }

    // ── Core logic ─────────────────────────────────────────────────────────
    void TryPickBlock(Vector2 screenPos)
    {
        dragging   = null;
        isDragging = false;

        Ray ray = mainCam.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        dragging     = hit.collider.GetComponentInParent<BlockController>();
        lastWorldPos = ScreenToWorldPlane(screenPos);
    }

    /// <summary>Di chuyen block dua tren vi tri man hinh moi (dung cho Mouse).</summary>
    void MoveBlock(Vector2 screenPos)
    {
        if (dragging == null) return;

        Vector3 current = ScreenToWorldPlane(screenPos);
        Vector3 delta   = (current - lastWorldPos) * dragSensitivity;
        lastWorldPos    = current;

        dragging.DragTo(delta);
    }

    /// <summary>Di chuyen block dua tren delta pixel (dung cho Touch - muot hon).</summary>
    void MoveDelta(Vector2 screenDelta)
    {
        if (dragging == null) return;

        // Chuyen doi delta pixel -> delta world tren mat phang Y=0
        // Bang cach dich chuyen lastWorldPos theo screenDelta roi tinh su chenh lech
        Vector3 anchorWorld = ScreenToWorldPlane(mainCam.WorldToScreenPoint(lastWorldPos));
        Vector3 newWorld    = ScreenToWorldPlane(mainCam.WorldToScreenPoint(lastWorldPos) + (Vector3)screenDelta);
        Vector3 delta       = (newWorld - anchorWorld) * dragSensitivity;
        lastWorldPos        = newWorld;

        dragging.DragTo(delta);
    }

    void ReleaseBlock()
    {
        if (dragging != null) dragging.OnRelease();
        dragging   = null;
        isDragging = false;
    }

    // ── Helper: tim touch theo fingerId ───────────────────────────────────
    Touch? FindTouch(int fingerId)
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).fingerId == fingerId)
                return Input.GetTouch(i);
        }
        return null;
    }

    // ── Helper: screen -> world tren mat phang Y = 0 ──────────────────────
    Vector3 ScreenToWorldPlane(Vector2 screenPos)
    {
        Ray   ray   = mainCam.ScreenPointToRay(screenPos);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        if (plane.Raycast(ray, out float dist))
            return ray.GetPoint(dist);
        return lastWorldPos;
    }
}
