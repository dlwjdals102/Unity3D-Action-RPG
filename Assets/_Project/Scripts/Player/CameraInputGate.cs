using UnityEngine;
using Unity.Cinemachine;

/// 일반(프리룩) 카메라의 마우스 입력(CinemachineInputAxisController) 활성 여부를 한 곳에서 결정.
/// 비활성 조건: 락온 중 OR UI 패널 열림. 차단 사유가 늘면 여기 AND 만 추가하면 됨.
public class CameraInputGate : MonoBehaviour
{
    [SerializeField] private LockOnSystem _lockOnSystem;
    [SerializeField] private CinemachineInputAxisController _inputController;

    private void Awake()
    {
        if (_inputController == null)
            _inputController = GetComponent<CinemachineInputAxisController>();
    }

    private void Update()
    {
        if (_inputController == null) return;

        bool lockedOn = _lockOnSystem != null && _lockOnSystem.CurrentTarget != null;
        _inputController.enabled = !lockedOn && !UIInputLock.IsOpen;   // 사유 모아 한 곳에서 결정
    }
}