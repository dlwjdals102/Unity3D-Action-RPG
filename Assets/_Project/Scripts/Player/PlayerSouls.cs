using System;
using UnityEngine;

/// <summary>
/// 플레이어 영혼(재화) 관리. 적 처치 시 획득하고 상점에서 소비한다.
/// 변경 시 OnSoulsChanged 발행 → UI 갱신 (이벤트 기반).
/// 
/// (사망 시 떨구기/회수는 보류 - 핵심 통화/상점 루프 먼저)
/// </summary>
public class PlayerSouls : MonoBehaviour
{
    [SerializeField] private int _souls = 0;

    /// <summary>영혼 변경 시 발행 (현재 보유량 전달). HUD/상점 UI 갱신.</summary>
    public event Action<int> OnSoulsChanged;

    /// <summary>현재 보유 영혼.</summary>
    public int Souls => _souls;

    /// <summary>영혼 획득 (적 처치 등).</summary>
    public void Add(int amount)
    {
        if (amount <= 0) return;
        _souls += amount;
        OnSoulsChanged?.Invoke(_souls);
    }

    /// <summary>
    /// 영혼 소비 시도 (상점 구매 등). 보유가 충분하면 차감하고 true,
    /// 부족하면 차감 없이 false.
    /// </summary>
    public bool TrySpend(int amount)
    {
        if (amount <= 0) return false;
        if (_souls < amount) return false;  // 부족

        _souls -= amount;
        OnSoulsChanged?.Invoke(_souls);
        return true;
    }
}