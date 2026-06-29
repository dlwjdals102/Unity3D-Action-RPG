using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 리스폰 매니저 (싱글톤). 화톳불 휴식 시 모든 적을 초기 상태로 되돌린다.
/// 죽어서 비활성된 적도 ResetToInitial 로 부활시킨다.
/// 
/// 씬 시작 시 모든 적을 수집해 보관. (죽은 적은 비활성이라 런타임 FindObjects 로는
/// 못 찾으므로, 시작 시점에 미리 등록해두는 것이 핵심.)
/// </summary>
public class EnemyRespawnManager : MonoBehaviour
{
    public static EnemyRespawnManager Instance { get; private set; }

    private readonly List<EnemyStateMachineBase> _enemies = new List<EnemyStateMachineBase>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 씬의 모든 적을 수집 (시작 시점엔 다 활성이라 전부 등록됨)
        // 이후 죽어서 비활성돼도 이 목록으로 부활 가능
        var found = FindObjectsByType<EnemyStateMachineBase>(FindObjectsSortMode.None);
        _enemies.AddRange(found);
    }

    /// <summary> 모든 적을 초기 상태로 (화톳불 휴식 시 호출). </summary>
    public void RespawnAll()
    {
        foreach (var enemy in _enemies)
        {
            if (enemy == null) continue;
            if (!enemy.ParticipatesInRespawn) continue;   // ← 보스 제외
            enemy.ResetToInitial();
        }
    }

    /// <summary> 전투 중(IsInCombat)인 적이 하나라도 있는지. 화톳불 휴식 가능 판단에 사용. </summary>
    public bool AnyEnemyInCombat()
    {
        foreach (var enemy in _enemies)
        {
            if (enemy != null && enemy.gameObject.activeSelf && enemy.IsInCombat)
            {
                return true;
            }
        }
        return false;
    }
}