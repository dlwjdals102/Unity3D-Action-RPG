using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 사망 시 리스폰 처리. PlayerHealth.OnDeath 를 구독해,
/// 잠깐 후 마지막 체크포인트(BonfireManager)로 이동 + 체력 복구 + 상태 복귀.
/// 
/// 체크포인트가 없으면(아직 화톳불 휴식 전) 게임 시작 위치로 리스폰.
/// CharacterController 는 transform 직접 이동이 막히므로, 끄고→이동→켜는 방식 사용.
/// </summary>
public class PlayerRespawn : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth _health;
    [SerializeField] private PlayerStateMachine _stateMachine;
    [SerializeField] private CharacterController _characterController;

    [Header("Respawn")]
    [Tooltip("사망 후 리스폰까지 대기 시간(초)")]
    [SerializeField] private float _respawnDelay = 1.5f;

    /// <summary>부활 완료 시 발행 (YOU DIED 화면 숨김 등). OnDeath 와 쌍.</summary>
    public event Action OnRespawned;

    private Vector3 _startPosition;       // 체크포인트 없을 때 사용 (게임 시작 위치)
    private Quaternion _startRotation;

    private void Awake()
    {
        if (_health == null) _health = GetComponent<PlayerHealth>();
        if (_stateMachine == null) _stateMachine = GetComponent<PlayerStateMachine>();
        if (_characterController == null) _characterController = GetComponent<CharacterController>();

        // 게임 시작 위치 기억 (체크포인트 없을 때의 폴백)
        _startPosition = transform.position;
        _startRotation = transform.rotation;
    }

    private void OnEnable()
    {
        if (_health != null) _health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (_health != null) _health.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        // 사망 연출 시간 (DeathState 가 입력 차단 중)
        yield return new WaitForSeconds(_respawnDelay);

        // 리스폰 위치 결정 (체크포인트 우선, 없으면 시작 위치)
        Vector3 respawnPos = _startPosition;
        Quaternion respawnRot = _startRotation;
        if (BonfireManager.Instance != null && BonfireManager.Instance.HasCheckpoint)
        {
            respawnPos = BonfireManager.Instance.CheckpointPosition;
            respawnRot = BonfireManager.Instance.CheckpointRotation;
        }

        // 위치 이동 (CharacterController 끄고 → 이동 → 켜기)
        TeleportTo(respawnPos, respawnRot);

        // 체력 복구
        if (_health != null) _health.ResetHealth();

        // 상태 복귀 (Idle)
        if (_stateMachine != null) _stateMachine.ChangeState(_stateMachine.IdleState);

        // 부활 완료 알림 (YOU DIED 숨김 등)
        OnRespawned?.Invoke();
    }

    /// <summary>CharacterController 충돌을 피해 안전하게 순간이동. (리스폰/로드 공용)</summary>
    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        if (_characterController != null) _characterController.enabled = false;

        transform.position = position;
        transform.rotation = rotation;

        if (_characterController != null) _characterController.enabled = true;
    }
}