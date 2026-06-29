using UnityEngine;

/// <summary>
/// 단일 사운드 정의 (ScriptableObject). 클립(여러 변형) + 볼륨 + 피치 랜덤 보유.
/// 각 시스템은 문자열이 아니라 이 SO 참조를 들고 AudioManager 에 넘긴다 (타입 안전).
/// ItemData 와 동일한 SO 데이터 주도 패턴 - SO 는 순수 데이터, 재생은 AudioManager.
/// </summary>
[CreateAssetMenu(fileName = "Sound_", menuName = "Audio/Sound Definition")]
public class SoundDefinition : ScriptableObject
{
    [Tooltip("재생할 클립들. 여러 개면 매번 랜덤 선택 (반복음의 기계적 느낌 방지)")]
    [SerializeField] private AudioClip[] _clips;

    [Tooltip("볼륨")]
    [Range(0f, 1f)]
    [SerializeField] private float _volume = 1f;

    [Tooltip("피치 랜덤 범위 (1 = 원음). 살짝 흔들어 다양성 부여")]
    [SerializeField] private float _pitchMin = 1f;
    [SerializeField] private float _pitchMax = 1f;

    public bool HasClip => _clips != null && _clips.Length > 0;
    public float Volume => _volume;

    /// <summary>랜덤 클립 1개 (없으면 null).</summary>
    public AudioClip GetClip()
    {
        if (!HasClip) return null;
        return _clips[Random.Range(0, _clips.Length)];
    }

    /// <summary>랜덤 피치 (min~max).</summary>
    public float GetPitch() => Random.Range(_pitchMin, _pitchMax);
}