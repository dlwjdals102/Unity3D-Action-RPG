using UnityEngine;

/// <summary>
/// ScriptableObject 기반 적 데이터 정의.
/// 적 종류별로 에셋을 만들어 수치를 조정합니다.
/// 
/// [사용법]
/// Project 창 → 우클릭 → Create → DarkBlade → Enemy Data
/// </summary>
[CreateAssetMenu(fileName = "NewEnemy", menuName = "DarkBlade/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Basic Info")]
    public string enemyName = "Enemy";

    [Header("Stats")]
    public float maxHp = 100f;
    public float attackDamage = 10f;
    public float attackRange = 2f;
    [Tooltip("이 거리 안에서는 추적을 멈춥니다. attackRange보다 약간 짧게 설정 권장")]
    public float stopChaseRange = 1.5f;
    public float attackCooldown = 2f;
    public float moveSpeed = 3f;
    public float knockbackForce = 3f;

    [Header("Detection")]
    public float detectRange = 10f;
    public float loseRange = 15f;

    [Header("Patrol")]
    public float patrolRadius = 5f;
    public float patrolWaitTime = 2f;
}