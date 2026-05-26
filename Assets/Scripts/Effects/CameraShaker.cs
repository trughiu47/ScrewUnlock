using UnityEngine;
using DG.Tweening;

/// <summary>
/// CameraShaker — gắn lên Main Camera GameObject.
/// Cung cấp shake và soft zoom.
/// </summary>
public class CameraShaker : MonoBehaviour
{
    private Vector3 _originPos;
    private float _originOrthoSize;
    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        _originPos = transform.localPosition;
        _originOrthoSize = _cam != null ? _cam.orthographicSize : 5f;
    }

    /// <summary>Rung camera trong <paramref name="duration"/> giây.</summary>
    public void Shake(float duration, float strength)
    {
        transform.DOShakePosition(duration, strength, vibrato: 18, randomness: 90)
                 .SetEase(Ease.OutQuad);
    }

    /// <summary>Zoom nhẹ vào tâm trong <paramref name="duration"/> giây.</summary>
    public void SoftZoomIn(float duration, float amount)
    {
        if (_cam == null) return;
        float target = _originOrthoSize * (1f - amount);
        DOTween.To(() => _cam.orthographicSize,
                   x => _cam.orthographicSize = x,
                   target, duration).SetEase(Ease.OutQuad);
    }

    /// <summary>Trả camera về orthographicSize ban đầu.</summary>
    public void ResetZoom(float duration = 0.4f)
    {
        if (_cam == null) return;
        DOTween.To(() => _cam.orthographicSize,
                   x => _cam.orthographicSize = x,
                   _originOrthoSize, duration).SetEase(Ease.OutQuad);
    }
}