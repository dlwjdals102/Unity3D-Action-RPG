using System.Collections;
using UnityEngine;

/// <summary>
/// 테스트용 허수아비 적.
/// IDamageable을 구현하여 히트 판정 테스트에 사용합니다.
/// 데미지를 받으면 색이 바뀌고 로그를 출력합니다.
/// 
/// [사용법]
/// 1. Capsule 또는 캐릭터 모델에 이 스크립트 부착
/// 2. Layer를 Enemy로 설정
/// 3. 자식에 Collider 추가 (HitBox의 Trigger와 충돌용)
/// </summary>
public class EnemyDummy : MonoBehaviour, IDamageable
{
    // ════════════════════════════════════════════════════
    //  설정
    // ════════════════════════════════════════════════════

    [Header("Stats")]
    [SerializeField] private float _maxHp = 100f;
    [SerializeField] private float _currentHp;

    [Header("Visual Feedback")]
    [SerializeField] private float _flashDuration = 0.15f;

    // ════════════════════════════════════════════════════
    //  IDamageable 구현
    // ════════════════════════════════════════════════════

    public float CurrentHp => _currentHp;
    public float MaxHp => _maxHp;
    public bool IsAlive => _currentHp > 0f;

    // ════════════════════════════════════════════════════
    //  내부
    // ════════════════════════════════════════════════════

    private Renderer _renderer;
    private Color _originalColor;
    private Coroutine _flashCoroutine;

    private void Awake()
    {
        _currentHp = _maxHp;
        _renderer = GetComponentInChildren<Renderer>();

        if (_renderer != null)
            _originalColor = _renderer.material.color;
    }

    public float TakeDamage(DamageData data)
    {
        if (!IsAlive) return 0f;

        // 데미지 적용
        float actualDamage = Mathf.Min(data.Amount, _currentHp);
        _currentHp -= actualDamage;

        // 로그 출력
        Debug.Log(
            $"[EnemyDummy] 피격! 데미지: {actualDamage:F0} | " +
            $"남은 HP: {_currentHp:F0}/{_maxHp:F0} | " +
            $"타입: {data.Type} | " +
            $"공격자: {data.Attacker?.name ?? "Unknown"}"
        );

        // 넉백 적용
        if (data.KnockbackForce > 0f)
        {
            Vector3 knockback = data.KnockbackDirection * data.KnockbackForce;
            transform.position += knockback * Time.deltaTime * 10f;
        }

        // 피격 플래시
        if (_renderer != null)
        {
            if (_flashCoroutine != null)
                StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashRoutine());
        }

        // 사망 체크
        if (!IsAlive)
        {
            OnDeath();
        }

        return actualDamage;
    }

    private IEnumerator FlashRoutine()
    {
        // 빨간색으로 변경
        _renderer.material.color = Color.red;

        yield return new WaitForSeconds(_flashDuration);

        // 원래 색으로 복귀 (살아있으면)
        if (_renderer != null)
            _renderer.material.color = IsAlive ? _originalColor : Color.gray;

        _flashCoroutine = null;
    }

    private void OnDeath()
    {
        Debug.Log($"[EnemyDummy] {gameObject.name} 사망!");

        // 색상을 회색으로 변경
        if (_renderer != null)
            _renderer.material.color = Color.gray;

        // GameManager에 적 처치 알림
        if (GameManager.HasInstance)
            GameManager.Instance.NotifyEnemyKilled(gameObject);

        // 3초 후 HP 리셋 (허수아비는 부활)
        Invoke(nameof(Respawn), 3f);
    }

    private void Respawn()
    {
        _currentHp = _maxHp;

        if (_renderer != null)
            _renderer.material.color = _originalColor;

        Debug.Log($"[EnemyDummy] {gameObject.name} 부활!");
    }
}