using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 락온 카메라 제어. LockOnSystem 의 현재 타겟 상태를 보고 락온 전용 카메라를 켜고 끈다.
/// (LockOnSystem 은 "누구를 락온" 만 담당, 이 컨트롤러가 "그걸 카메라로" 담당 - 책임 분리)
/// 
/// 락온 중: 락온 카메라의 LookAt = 타겟, Priority 를 활성값으로 올림 → Cinemachine 자동 전환.
/// 해제 중: Priority 를 0 으로, LookAt 해제 → 일반 카메라로 복귀.
/// </summary>
public class LockOnCameraController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("락온 타겟을 관리하는 LockOnSystem")]
    [SerializeField] private LockOnSystem _lockOnSystem;

    [Tooltip("락온 전용 Cinemachine 카메라 (CM_LockOnCamera)")]
    [SerializeField] private CinemachineCamera _lockOnCamera;

    [Tooltip("플레이어+적을 함께 담는 Target Group (LockOnTargetGroup)")]
    [SerializeField] private CinemachineTargetGroup _targetGroup;

    [Tooltip("평소 일반 Cinemachine 카메라 (CM_PlayerCamera)")]
    [SerializeField] private CinemachineCamera _normalCamera;

    [Header("Priority")]
    [Tooltip("락온 중 락온 카메라 우선순위 (일반 카메라보다 높게)")]
    [SerializeField] private int _activePriority = 20;

    [Tooltip("락온 해제 시 우선순위 (일반 카메라보다 낮게)")]
    [SerializeField] private int _inactivePriority = -10;

    private Transform _lastTarget;
    private CinemachineOrbitalFollow _normalOrbital;
    //private CinemachineInputAxisController _normalInputController;

    private void Awake()
    {
        // 시작 시 락온 카메라 비활성 상태로
        if (_lockOnCamera != null)
        {
            _lockOnCamera.Priority = _inactivePriority;
        }

        // 일반 카메라의 Orbital Follow + Input Axis Controller 캐싱
        if (_normalCamera != null)
        {
            _normalOrbital = _normalCamera.GetComponent<CinemachineOrbitalFollow>();
            //_normalInputController = _normalCamera.GetComponent<CinemachineInputAxisController>();
        }
    }

    private void Update()
    {
        if (_lockOnSystem == null || _lockOnCamera == null) return;

        Transform target = _lockOnSystem.CurrentTarget;
        // 타겟 상태가 바뀐 경우에만 갱신 (매 프레임 불필요한 설정 방지)
        if (target == _lastTarget) return;

        Transform previousTarget = _lastTarget;  // 그룹에서 제거할 이전 적
        _lastTarget = target;

        if (target != null)
        {
            // 락온 시작/타겟 변경: 이전 적이 그룹에 있으면 제거 후 새 적 추가
            RemoveTargetFromGroup(previousTarget);
            AddTargetToGroup(target);
            _lockOnCamera.Priority = _activePriority;
        }
        else
        {
            // 락온 해제: 일반 카메라를 "락온 중 보던 방향"으로 맞춰 시점이 끊기지 않게 한 뒤
            // 우선순위를 내려 일반 카메라로 복귀.
            // (적은 그룹에서 빼지 않는다 - 빼면 락온 카메라가 플레이어 중앙으로 틀어지며
            //  그 움직임이 블렌드에 섞여 어색하다. 이전 적 정리는 다음 락온 시작 시 수행.)
            SyncNormalCameraYaw();
            _lockOnCamera.Priority = _inactivePriority;
        }
    }

    /// <summary>
    /// 일반 카메라의 Orbital 수평 각도를 현재 메인 카메라가 보는 yaw 로 맞춘다.
    /// 락온 해제 시 호출 → 락온 카메라가 보던 방향에서 끊김 없이 이어짐.
    /// </summary>
    private void SyncNormalCameraYaw()
    {
        if (_normalOrbital == null) return;

        // 현재 활성(락온) 카메라가 보고 있는 월드 yaw
        if (Camera.main != null)
        {
            float currentYaw = Camera.main.transform.eulerAngles.y;
            _normalOrbital.HorizontalAxis.Value = currentYaw;
        }
    }

    /// <summary>
    /// 적을 Target Group 에 추가 (이미 있으면 무시). 카메라가 플레이어와 함께 담는다.
    /// </summary>
    private void AddTargetToGroup(Transform target)
    {
        if (_targetGroup == null || target == null) return;
        if (_targetGroup.FindMember(target) >= 0) return;  // 이미 멤버면 skip
        _targetGroup.AddMember(target, 1f, 1f);  // weight 1, radius 1
    }

    /// <summary>
    /// 적을 Target Group 에서 제거 (없으면 무시).
    /// </summary>
    private void RemoveTargetFromGroup(Transform target)
    {
        if (_targetGroup == null || target == null) return;
        if (_targetGroup.FindMember(target) < 0) return;  // 멤버 아니면 skip
        _targetGroup.RemoveMember(target);
    }
}