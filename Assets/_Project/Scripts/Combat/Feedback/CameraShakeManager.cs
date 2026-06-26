using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 카메라 셰이크 매니저 - 싱글톤.
/// Cinemachine Impulse Source 를 감싸, 호출부(공격/피격 등)가 Cinemachine 을 직접
/// 몰라도 Shake() 한 번으로 화면 흔들림을 발생시킨다. (HitStopManager 와 같은 패턴)
/// 
/// 같은 GameObject 의 CinemachineImpulseSource 를 사용한다.
/// Impulse Listener 가 붙은 CinemachineCamera 가 이 신호를 받아 흔들린다.
/// </summary>
[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance { get; private set; }

    [Tooltip("기본 셰이크 강도 배율")]
    [SerializeField] private float _defaultForce = 1f;

    private CinemachineImpulseSource _source;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _source = GetComponent<CinemachineImpulseSource>();
    }

    /// <summary>기본 강도로 카메라 셰이크. 타격 적중 등에서 호출.</summary>
    public void Shake()
    {
        Shake(_defaultForce);
    }

    /// <summary>
    /// 지정 강도로 카메라 셰이크. (보스 강공격은 강하게, 잡몹은 약하게 등)
    /// </summary>
    public void Shake(float force)
    {
        if (_source == null) return;
        _source.GenerateImpulse(force);
    }
}