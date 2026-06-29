using UnityEngine;

/// <summary>
/// 테스트용 적. IDamageable 구현.
/// 공격 시스템 검증 목적.
/// 단일 책임: 데미지 받기 + 체력 추적.
/// </summary>
public class TargetDummy : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int _maxHealth = 100;

    [Header("Damage Text")]
    [Tooltip("데미지 텍스트가 표시될 머리 위 높이")]
    [SerializeField] private float _damageTextHeight = 2f;

    private int _currentHealth;

    // === Public Properties ===
    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;
    public bool IsDead => _currentHealth <= 0;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    /// <summary>
    /// IDamageable 인터페이스 구현.
    /// 양수 데미지만 처리하며, 이미 사망 상태면 무시한다.
    /// </summary>
    public void TakeDamage(DamageInfo info)
    {
        if (IsDead) return;
        if (info.Amount <= 0) return;

        _currentHealth -= info.Amount;
        if (_currentHealth < 0) _currentHealth = 0;

        // 머리 위에 데미지 텍스트 생성 (콤보 시 가독성을 위한 표준 패턴)
        Vector3 textPosition = transform.position + Vector3.up * _damageTextHeight;
        DamageTextManager.Instance?.Spawn(info.Amount, textPosition);

        if (IsDead) gameObject.SetActive(false);
    }
}