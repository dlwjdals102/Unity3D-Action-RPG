using System.Collections;
using UnityEngine;

/// <summary>
/// 히트스톱 (Hit Stop) 매니저 - 싱글톤.
/// 타격 적중 순간 Time.timeScale 을 짧게 낮춰 "묵직한 타격감" 을 준다.
/// (DamageTextManager 와 같은 싱글톤 패턴)
/// 
/// 핵심 주의:
/// - 대기는 WaitForSecondsRealtime (timeScale 영향 안 받게). WaitForSeconds 쓰면
///   timeScale 이 낮아 대기가 길어진다.
/// - 연타로 중복 호출 시 코루틴 재시작 (timeScale 복원 꼬임 방지).
/// </summary>
public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    [Header("Hit Stop")]
    [Tooltip("히트스톱 지속 시간(초, 실시간)")]
    [SerializeField] private float _duration = 0.08f;

    [Tooltip("히트스톱 중 시간 배율 (0 = 완전 정지, 0.05 = 거의 정지)")]
    [SerializeField] private float _timeScale = 0.05f;

    private Coroutine _routine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 기본 설정값으로 히트스톱 발동. 타격 적중 시 호출.
    /// </summary>
    public void Trigger()
    {
        Trigger(_duration, _timeScale);
    }

    /// <summary>
    /// 지정 길이/배율로 히트스톱 발동. (공격 종류별로 다른 강도 줄 때)
    /// 이미 진행 중이면 재시작.
    /// </summary>
    public void Trigger(float duration, float timeScale)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(HitStopRoutine(duration, timeScale));
    }

    private IEnumerator HitStopRoutine(float duration, float timeScale)
    {
        Time.timeScale = timeScale;

        // 실시간 대기 (timeScale 영향 안 받음)
        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;
        _routine = null;
    }

    private void OnDisable()
    {
        // 안전: 비활성/파괴 시 timeScale 복원 (멈춘 채 남는 것 방지)
        if (Instance == this) Time.timeScale = 1f;
    }
}