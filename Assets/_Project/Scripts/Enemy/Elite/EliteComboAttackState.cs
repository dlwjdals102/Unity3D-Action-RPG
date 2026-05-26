using UnityEngine;

/// <summary>
/// 엘리트 적의 확률적 콤보 공격 상태.
/// 1타(항상) → [확률] → 2타 → [확률] → 3타. 각 타 종료 시 다음 타 진행을 확률 판정.
/// 플레이어의 의도적 3타 콤보와 달리 예측 불가 (거울 콘셉트의 변주).
/// 
/// 흐름:
/// - OnEnter: 정지 + 즉시 회전(다크소울, 콤보 중 회전 안 함 → 회피 보상) + 1타 시작.
/// - OnUpdate: 타 종료(IsAttackFinished) 시
///   - 다음 타 존재 + 확률 통과 → 다음 타
///   - 아니면 → 콤보 종료 → 거리 분기 (가까우면 재콤보, 멀면 ToChase)
/// 
/// 데미지는 EliteEnemyAttacker.SetCurrentCombo(index) 로 각 타 시작 시 설정.
/// 실제 타격(PerformHit)은 Animation Event 가 담당.
/// 애니메이션은 Phase 1 에선 3타 같은 클립 (ComboIndex 없음, 시각 폴리싱은 나중).
/// 
/// Config 는 생성자 주입 (베이스 _config 는 EnemyConfig 라 콤보 메서드 접근 불가, RangedEnemyAttackState 패턴).
/// </summary>
public class EliteComboAttackState : EnemyStateBase
{
    private readonly EliteEnemyConfig _eliteConfig;
    private EliteEnemyAttacker _attacker;

    private int _comboIndex;

    // 콤보 세트 종료 후 쿨다운 (펀치 윈도우)
    private bool _isCoolingDown;
    private float _cooldownTimer;

    public EliteComboAttackState(EnemyStateMachineBase stateMachine, EliteEnemyConfig config)
        : base(stateMachine)
    {
        _eliteConfig = config;
    }

    public override void OnEnter()
    {
        // Config 누락 시 콤보 불가 → 안전하게 추격 복귀 (폴백 상황)
        if (_eliteConfig == null)
        {
            Debug.LogError("[EliteComboAttackState] EliteEnemyConfig is null. Returning to chase.");
            _stateMachine.ToChase();
            return;
        }

        // Attacker 캐싱 (첫 진입 시 1회)
        if (_attacker == null)
        {
            _attacker = _stateMachine.GetComponent<EliteEnemyAttacker>();
        }

        // 정지
        _stateMachine.Movement.StopMoving();
        _stateMachine.Animator.SetMoveSpeed(0f);

        // 콤보 시작 시 1회 회전 (다크소울, 콤보 중에는 회전 안 함 → 회피 보상)
        if (_stateMachine.Target != null)
        {
            _stateMachine.Movement.SetRotationImmediate(_stateMachine.Target.position);
        }

        // 콤보 모드로 시작 (쿨다운 아님)
        _isCoolingDown = false;

        // 1타부터 시작
        _comboIndex = 0;
        StartCurrentCombo();
    }

    public override void OnUpdate()
    {
        // Config 누락 가드 (OnEnter 에서 ToChase 되지만 안전망)
        if (_eliteConfig == null) return;

        // === 쿨다운 모드 (콤보 세트 종료 후 대기) ===
        if (_isCoolingDown)
        {
            UpdateCooldown();
            return;
        }

        // 타가 끝나야 다음 행동 결정
        if (!_stateMachine.Animator.IsAttackFinished) return;

        // 다음 타로 진행할지 확률 판정
        if (_comboIndex < _eliteConfig.MaxComboCount - 1)
        {
            float chance = _eliteConfig.GetContinueChance(_comboIndex);
            if (Random.value < chance)
            {
                // 다음 타 진행
                _comboIndex++;
                StartCurrentCombo();
                return;
            }
        }

        // 확률 실패 or 마지막 타 → 콤보 종료
        EndCombo();
    }

    /// <summary>
    /// 현재 _comboIndex 타를 시작. 데미지 설정 + 공격 애니메이션 재생.
    /// </summary>
    private void StartCurrentCombo()
    {
        // 현재 타의 데미지 설정 (PerformHit 이 이 값 사용)
        _attacker?.SetCurrentCombo(_comboIndex);

        // 공격 애니메이션 (IsAttackFinished 리셋)
        _stateMachine.Animator.PlayAttack();
    }

    /// <summary>
    /// 콤보 세트 종료 → 쿨다운 진입 (즉시 새 콤보 대신 대기).
    /// 펀치 윈도우: 플레이어가 반격할 틈을 준다.
    /// </summary>
    private void EndCombo()
    {
        _isCoolingDown = true;
        _cooldownTimer = _stateMachine.AttackCooldown;

        // 이동 정지. Idle 자세 전환은 UpdateCooldown 이 매 프레임 PlayIdle 로 처리
        _stateMachine.Movement.StopMoving();
    }

    /// <summary>
    /// 쿨다운 중 매 프레임 처리.
    /// - Target 조준 (LookAt, 부드러운 회전)
    /// - 멀어지면 즉시 추격 (쿨다운 무시)
    /// - 쿨다운 끝 + 가까우면 새 콤보
    /// </summary>
    private void UpdateCooldown()
    {
        if (_stateMachine.Target == null)
        {
            _stateMachine.ToChase();
            return;
        }

        // 대기 중 Idle 자세 (매 프레임 호출로 댐핑 수렴) + Target 조준
        _stateMachine.Animator.PlayIdle();
        _stateMachine.Movement.LookAt(_stateMachine.Target.position);

        _cooldownTimer -= Time.deltaTime;

        float distance = _stateMachine.Movement.DistanceTo(_stateMachine.Target.position);

        // 멀어지면 쿨다운 무시하고 추격 (추격 우선)
        if (distance > _stateMachine.AttackRange)
        {
            _stateMachine.ToChase();
            return;
        }

        // 쿨다운 끝 + 가까움 → 새 콤보 (재조준 후 1타부터)
        if (_cooldownTimer <= 0f)
        {
            _stateMachine.Movement.SetRotationImmediate(_stateMachine.Target.position);
            _isCoolingDown = false;
            _comboIndex = 0;
            StartCurrentCombo();
        }
    }
}