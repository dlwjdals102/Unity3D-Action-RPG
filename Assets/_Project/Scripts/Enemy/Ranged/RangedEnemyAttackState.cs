using UnityEngine;

/// <summary>
/// 원거리 적의 공격 상태 (발사).
/// 근접 MeleeEnemyAttackState 와 다름:
/// - 즉시 회전(다크소울) 대신 LookAt 조준 (멀리서 천천히 조준 → 회피 여지).
/// - 연속 공격 대신 발사 쿨다운 (Config.AttackCooldown).
/// 
/// 흐름:
/// - OnEnter: 정지 + 발사 애니메이션 + 쿨다운 타이머 시작.
///   발사체 생성은 Animation Event (OnAttackHit → RangedAttacker.PerformHit) 가 담당.
/// - OnUpdate: Target 조준 (LookAt) + 발사 애니메이션 종료 후 거리/쿨다운 분기.
///   - 발사 거리 밖: ToChase (다시 접근)
///   - 발사 거리 안 + 쿨다운 끝: 재발사
///   - 발사 거리 안 + 쿨다운 중: 조준하며 대기
/// 
/// _stateMachine 은 베이스 타입. 전환은 ToChase 의도 (원거리 파생이 EnemyChaseState 로).
/// </summary>
public class RangedEnemyAttackState : EnemyStateBase
{
    private bool _isCoolingDown;
    private RangedEnemyAttacker _attacker;

    public RangedEnemyAttackState(EnemyStateMachineBase stateMachine)
        : base(stateMachine)
    {
    }

    public override void OnEnter()
    {
        _stateMachine.Movement.StopMoving();
        _stateMachine.Animator.SetMoveSpeed(0f);

        // 쿨다운 준비됐으면 즉시 발사, 아니면 조준하며 대기 (전투 진입 비트)
        if (_stateMachine.IsAttackReady)
        {
            FireOnce();
        }
        else
        {
            _isCoolingDown = true;
            _stateMachine.Animator.PlayIdle();
        }
    }

    public override void OnUpdate()
    {
        if (_stateMachine.Target == null) return;

        // 쿨다운 대기 모드 (안 쏘고 진입했거나, 발사 애니메이션 종료 후)
        if (_isCoolingDown)
        {
            UpdateCooldown();
            return;
        }

        // 발사 애니메이션 진행 중: 끝날 때까지 대기 (이 동안 LookAt 안 함 = 조준 고정 유지)
        if (!_stateMachine.Animator.IsAttackFinished) return;

        // 발사 애니메이션 종료 → 쿨다운 대기 모드로 전환
        _isCoolingDown = true;
    }

    /// <summary>
    /// 발사 1회 시작. 발사 애니메이션 재생 + 쿨다운 타이머 리셋.
    /// 실제 발사체 생성은 Animation Event (OnAttackHit) 가 RangedAttacker.PerformHit 호출.
    /// </summary>
    private void FireOnce()
    {
        // Attacker 캐싱 (첫 호출 1회) - EliteChargeState 패턴
        if (_attacker == null)
        {
            _attacker = _stateMachine.GetComponent<RangedEnemyAttacker>();
        }

        // Target 향함 (발사 순간 1회 즉시 회전 → 정확한 발사 방향)
        if (_stateMachine.Target != null)
        {
            _stateMachine.Movement.SetRotationImmediate(_stateMachine.Target.position);
        }

        // 발사 방향을 이 시점에 고정 (윈드업~릴리즈 동안 안 바뀜 = 회피 보상)
        _attacker?.LockAim();

        // 발사 애니메이션 (IsAttackFinished 리셋)
        _stateMachine.Animator.PlayAttack();

        // 쿨다운 시작
        _stateMachine.StartAttackCooldown();   // 쿨다운 시작 (발사 순간 기준)
        _isCoolingDown = false;                // 발사 애니메이션 진행 모드로
    }

    /// <summary>
    /// 쿨다운 중 매 프레임: 조준(LookAt) + 거리/시야 체크 + 준비되면 재발사.
    /// 조준은 여기(대기 중)서만 → 발사 애니메이션 중엔 발사 시작 방향 고정(회피 보상).
    /// </summary>
    private void UpdateCooldown()
    {
        // 다음 발사를 위한 조준 (대기 중 Target 추적, 부드러운 회전)
        _stateMachine.Movement.LookAt(_stateMachine.Target.position);

        // 공격 불가(사거리 밖 or 시야 차단)면 추격 복귀 - 진입과 같은 술어(대칭)
        if (!_stateMachine.CanAttackTarget())
        {
            _stateMachine.ToChase();
            return;
        }

        // 쿨다운 끝 → 재발사
        if (_stateMachine.IsAttackReady)
        {
            FireOnce();
        }
        else
        {
            _stateMachine.Animator.PlayIdle();
        }
    }
}