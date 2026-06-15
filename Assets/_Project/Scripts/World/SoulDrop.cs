using UnityEngine;

/// <summary>
/// 떨어진 영혼 (월드 오브젝트). 사망 시 SoulDropManager 가 사망 위치에 생성하며,
/// 플레이어가 접촉하면 보관한 영혼을 전액 회수하고 소멸한다 (접촉 자동 회수).
/// 프리팹: 트리거 콜라이더 필요 (화톳불/상점과 같은 감지 패턴).
/// </summary>
public class SoulDrop : MonoBehaviour
{
    [Tooltip("보관 영혼량 (매니저가 SetAmount 로 설정. 씬 배치 테스트 시 직접 입력 가능)")]
    [SerializeField] private int _amount = 0;

    /// <summary>보관 중인 영혼량 (세이브 저장용).</summary>
    public int Amount => _amount;

    /// <summary>드롭 생성 시 영혼량 설정 (SoulDropManager 가 호출).</summary>
    public void SetAmount(int amount)
    {
        _amount = amount;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어만 회수 가능 (적/투사체 등은 PlayerSouls 가 없어 무시됨)
        var souls = other.GetComponentInParent<PlayerSouls>();
        if (souls == null) return;

        // 사망 직후 보호: 드롭이 사망 위치(시체와 겹침)에 생성되므로,
        // 죽어있는 플레이어가 즉시 회수해버리는 것을 방지 (부활 후에만 회수 가능)
        var health = souls.GetComponent<PlayerHealth>();
        if (health != null && health.IsDead) return;

        // 회수: 전액 복구 후 소멸 + 매니저 참조 정리
        souls.Add(_amount);
        SoulDropManager.Instance?.NotifyRecovered(this);
        Destroy(gameObject);
    }
}