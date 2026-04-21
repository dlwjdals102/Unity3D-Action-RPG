using UnityEngine;
using System;

/// <summary>
/// Animator 파라미터 제어를 전담하는 브릿지 클래스.
/// PlayerController와 Animator 사이의 중간 계층으로,
/// 나중에 FSM에서 상태별 애니메이션 전환을 이 클래스를 통해 제어합니다.
/// 
/// [역할]
/// - 애니메이션 파라미터 캐싱 및 업데이트
/// - 상태 전환 트리거 발행
/// - 애니메이션 이벤트 수신 (공격 히트 타이밍 등)
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    // ── 캐싱 ──────────────────────────────────────────
    private Animator _animator;

    // ── 상태 추적 ────────────────────────────────────
    private bool _isInTransition;
    private int _currentStateHash;

    // ── 이벤트 (전투 시스템에서 구독) ──────────────────
    /// <summary>공격 애니메이션의 히트 타이밍에 발생합니다.</summary>
    public event Action OnAttackHitFrame;

    /// <summary>공격 애니메이션에서 다음 콤보 입력 가능 타이밍에 발생합니다.</summary>
    public event Action OnComboWindowOpen;

    /// <summary>공격 애니메이션 종료 시 발생합니다.</summary>
    public event Action OnAttackEnd;

    /// <summary>회피 애니메이션에서 무적 시작 시 발생합니다.</summary>
    public event Action OnDodgeInvincibleStart;

    /// <summary>회피 애니메이션에서 무적 종료 시 발생합니다.</summary>
    public event Action OnDodgeInvincibleEnd;

    /// <summary>피격 애니메이션 종료 시 발생합니다.</summary>
    public event Action OnHitEnd;

    // ════════════════════════════════════════════════════
    //  초기화
    // ════════════════════════════════════════════════════

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    // ════════════════════════════════════════════════════
    //  파라미터 업데이트 (PlayerController에서 호출)
    // ════════════════════════════════════════════════════

    /// <summary>이동 속도를 Animator에 전달합니다. (0~1 정규화)</summary>
    public void SetSpeed(float normalizedSpeed)
    {
        _animator.SetFloat(
            Define.AnimParam.Speed,
            normalizedSpeed,
            0.1f,              // damping — 부드러운 전환
            Time.deltaTime
        );
    }

    /// <summary>착지 상태를 Animator에 전달합니다.</summary>
    public void SetGrounded(bool isGrounded)
    {
        _animator.SetBool(Define.AnimParam.IsGrounded, isGrounded);
    }

    /// <summary>무기 장착 상태를 Animator에 전달합니다.</summary>
    public void SetArmed(bool isArmed)
    {
        _animator.SetBool(Define.AnimParam.IsArmed, isArmed);
    }

    // ════════════════════════════════════════════════════
    //  상태 전환 트리거 (FSM에서 호출)
    // ════════════════════════════════════════════════════

    /// <summary>공격 애니메이션을 재생합니다.</summary>
    /// <param name="comboIndex">콤보 인덱스 (0, 1, 2)</param>
    public void PlayAttack(int comboIndex)
    {
        _animator.SetInteger(Define.AnimParam.AttackIndex, comboIndex);
        _animator.SetTrigger(Define.AnimParam.Attack);
    }

    /// <summary>스킬 애니메이션을 재생합니다.</summary>
    /// <param name="skillIndex">스킬 인덱스</param>
    public void PlaySkill(int skillIndex)
    {
        _animator.SetInteger(Define.AnimParam.SkillIndex, skillIndex);
        _animator.SetTrigger(Define.AnimParam.Skill);
    }

    /// <summary>회피 애니메이션을 재생합니다.</summary>
    public void PlayDodge()
    {
        _animator.SetTrigger(Define.AnimParam.Dodge);
    }

    /// <summary>피격 애니메이션을 재생합니다.</summary>
    public void PlayHit()
    {
        _animator.SetTrigger(Define.AnimParam.Hit);
    }

    /// <summary>사망 애니메이션을 재생합니다.</summary>
    public void PlayDie()
    {
        _animator.SetTrigger(Define.AnimParam.Die);
    }

    // ════════════════════════════════════════════════════
    //  상태 조회
    // ════════════════════════════════════════════════════

    /// <summary>현재 재생 중인 애니메이션 상태의 정규화 시간 (0~1).</summary>
    public float GetCurrentStateNormalizedTime()
    {
        var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.normalizedTime % 1f;
    }

    /// <summary>현재 전환(Transition) 중인지 확인합니다.</summary>
    public bool IsInTransition()
    {
        return _animator.IsInTransition(0);
    }

    /// <summary>특정 태그의 애니메이션이 재생 중인지 확인합니다.</summary>
    public bool IsPlayingTag(string tag)
    {
        return _animator.GetCurrentAnimatorStateInfo(0).IsTag(tag);
    }

    // ════════════════════════════════════════════════════
    //  애니메이션 이벤트 수신 메서드
    //  (Animator Controller의 Animation Clip에서
    //   Event로 이 메서드 이름을 등록합니다)
    // ════════════════════════════════════════════════════

    /// <summary>
    /// 공격 애니메이션에서 무기가 적에 닿는 타이밍.
    /// Animation Event에서 "OnHitFrame" 으로 등록합니다.
    /// </summary>
    private void OnHitFrame()
    {
        OnAttackHitFrame?.Invoke();
    }

    /// <summary>
    /// 콤보 입력 윈도우가 열리는 타이밍.
    /// Animation Event에서 "OnComboOpen" 으로 등록합니다.
    /// </summary>
    private void OnComboOpen()
    {
        OnComboWindowOpen?.Invoke();
    }

    /// <summary>
    /// 공격 애니메이션이 끝나는 타이밍.
    /// Animation Event에서 "OnAttackFinish" 로 등록합니다.
    /// </summary>
    private void OnAttackFinish()
    {
        OnAttackEnd?.Invoke();
    }

    /// <summary>
    /// 회피 무적 프레임 시작.
    /// Animation Event에서 "OnInvincibleStart" 로 등록합니다.
    /// </summary>
    private void OnInvincibleStart()
    {
        OnDodgeInvincibleStart?.Invoke();
    }

    /// <summary>
    /// 회피 무적 프레임 종료.
    /// Animation Event에서 "OnInvincibleEnd" 로 등록합니다.
    /// </summary>
    private void OnInvincibleEnd()
    {
        OnDodgeInvincibleEnd?.Invoke();
    }

    /// <summary>
    /// 피격 애니메이션 종료.
    /// Animation Event에서 "OnHitFinish" 로 등록합니다.
    /// </summary>
    private void OnHitFinish()
    {
        OnHitEnd?.Invoke();
    }
}
