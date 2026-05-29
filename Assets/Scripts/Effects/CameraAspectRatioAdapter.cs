using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAspectRatioAdapter : MonoBehaviour
{
    [Header("Design Settings")]
    [Tooltip("Tỷ lệ khung hình thiết kế gốc (ví dụ: 16:9 = 1.78f lúc thiết kế block trên màn hình ngang)")]
    [SerializeField] float targetAspectRatio = 1.78f; 

    Camera cam;
    float defaultFov;
    float defaultOrthographicSize;
    float lastWidth;
    float lastHeight;

    void Awake()
    {
        cam = GetComponent<Camera>();
        defaultFov = cam.fieldOfView;
        defaultOrthographicSize = cam.orthographicSize;
        
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        
        AdjustCamera();
    }

    void Update()
    {
        // Chỉ chạy điều chỉnh lại nếu kích thước màn hình thay đổi (tối ưu hóa hiệu năng)
        if (Mathf.Approximately(Screen.width, lastWidth) && Mathf.Approximately(Screen.height, lastHeight))
            return;

        lastWidth = Screen.width;
        lastHeight = Screen.height;
        AdjustCamera();
    }

    void AdjustCamera()
    {
        float currentAspectRatio = (float)Screen.width / Screen.height;

        // Nếu màn hình hiện tại hẹp hơn tỷ lệ thiết kế (ví dụ: đang xoay dọc Portrait)
        if (currentAspectRatio < targetAspectRatio)
        {
            if (cam.orthographic)
            {
                // Đối với Camera Orthographic: Điều chỉnh Orthographic Size để giữ nguyên chiều rộng vùng nhìn
                float differenceInSize = targetAspectRatio / currentAspectRatio;
                cam.orthographicSize = defaultOrthographicSize * differenceInSize;
            }
            else
            {
                // Đối với Camera Perspective: Điều chỉnh Field of View (FOV) theo phương ngang (Horizontal FOV)
                float radAngle = defaultFov * Mathf.Deg2Rad;
                float radHovAngle = 2f * Mathf.Atan(Mathf.Tan(radAngle / 2f) * targetAspectRatio);
                float horizontalFov = radHovAngle * Mathf.Rad2Deg;

                float radAngleV = 2f * Mathf.Atan(Mathf.Tan(horizontalFov * Mathf.Deg2Rad / 2f) / currentAspectRatio);
                cam.fieldOfView = radAngleV * Mathf.Rad2Deg;
            }
        }
        else
        {
            // Trả về mặc định nếu màn hình rộng hơn hoặc bằng tỷ lệ thiết kế
            if (cam.orthographic)
                cam.orthographicSize = defaultOrthographicSize;
            else
                cam.fieldOfView = defaultFov;
        }
    }
}
