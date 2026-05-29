using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Camera mainCam;

    [Header("Settings")]
    [SerializeField] float dragSensitivity = 1.0f;
    [SerializeField] float startDragPixels = 8f;

    BlockController dragging;
    Vector3         lastWorldPos;
    Vector2         pressScreenPos;
    bool            isDragging;

    int             activeFingerId = -1;

    void Awake()
    {
        if (mainCam == null) mainCam = Camera.main;
    }

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouse();
#else
        HandleTouch();
#endif
    }

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

    void HandleTouch()
    {
        int count = Input.touchCount;
        if (count == 0) return;

        if (activeFingerId >= 0)
        {
            Touch? activeTouch = FindTouch(activeFingerId);

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
                MoveDelta(t.deltaPosition);
            }

            return;
        }

        for (int i = 0; i < count; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Began)
            {
                if (IsPointerOverUI(t.fingerId))
                    break;

                pressScreenPos = t.position;
                activeFingerId = t.fingerId;
                TryPickBlock(t.position);
                break; // Chi quan tam ngon tay dau tien cham vao
            }
        }
    }

    void TryPickBlock(Vector2 screenPos)
    {
        dragging   = null;
        isDragging = false;

        Ray ray = mainCam.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        dragging     = hit.collider.GetComponentInParent<BlockController>();
        lastWorldPos = ScreenToWorldPlane(screenPos);
    }

    void MoveBlock(Vector2 screenPos)
    {
        if (dragging == null) return;

        Vector3 current = ScreenToWorldPlane(screenPos);
        Vector3 delta   = (current - lastWorldPos) * dragSensitivity;
        lastWorldPos    = current;

        dragging.DragTo(delta);
    }

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

    Touch? FindTouch(int fingerId)
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).fingerId == fingerId)
                return Input.GetTouch(i);
        }
        return null;
    }

    Vector3 ScreenToWorldPlane(Vector2 screenPos)
    {
        Ray   ray   = mainCam.ScreenPointToRay(screenPos);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        if (plane.Raycast(ray, out float dist))
            return ray.GetPoint(dist);
        return lastWorldPos;
    }
}
