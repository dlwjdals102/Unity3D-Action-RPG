using UnityEngine;

/// <summary>
/// 락온 마커 UI. 락온 중인 적의 화면 위치에 마커를 표시한다.
/// 적의 월드 위치를 WorldToScreenPoint 로 화면 좌표로 변환해 마커(UI)를 이동시킨다.
/// 락온이 없으면 마커를 숨긴다.
/// </summary>
public class LockOnMarker : MonoBehaviour
{
    [Header("References")]
    [Tooltip("락온 타겟을 관리하는 LockOnSystem")]
    [SerializeField] private LockOnSystem _lockOnSystem;

    [Tooltip("마커 UI (이 오브젝트의 RectTransform). 비우면 자기 자신 사용")]
    [SerializeField] private RectTransform _marker;

    [Header("Offset")]
    [Tooltip("타겟 위치에서 위로 올릴 높이(m). 적 가슴~머리에 맞춤")]
    [SerializeField] private float _heightOffset = 1.2f;

    private Camera _camera;

    private void Awake()
    {
        _camera = Camera.main;
        if (_marker == null) _marker = GetComponent<RectTransform>();

        // 시작 시 숨김
        SetVisible(false);
    }

    private void LateUpdate()
    {
        // LateUpdate: 적 이동/카메라 갱신 후 마커 위치를 계산 (한 프레임 지연 방지)
        if (_lockOnSystem == null || _camera == null) return;

        Transform target = _lockOnSystem.CurrentTarget;

        if (target == null)
        {
            SetVisible(false);
            return;
        }

        // 타겟 월드 위치(+높이) → 화면 좌표
        Vector3 worldPos = target.position + Vector3.up * _heightOffset;
        Vector3 screenPos = _camera.WorldToScreenPoint(worldPos);

        // 카메라 뒤(z<0)면 숨김 (화면 뒤 적)
        if (screenPos.z < 0f)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        _marker.position = screenPos;  // 마커를 화면 위치로
    }

    private void SetVisible(bool visible)
    {
        if (_marker != null && _marker.gameObject.activeSelf != visible)
        {
            _marker.gameObject.SetActive(visible);
        }
    }
}