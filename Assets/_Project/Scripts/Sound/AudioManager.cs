using UnityEngine;

/// <summary>
/// 전역 효과음 재생 Singleton. 중앙 SoundLibrary 를 보유하고, SoundDefinition 을
/// 받아 랜덤 클립/피치/볼륨으로 재생. AudioSource 풀(라운드로빈)로 피치 독립. 2D(YAGNI).
/// 호출부: AudioManager.Instance.PlaySound(AudioManager.Instance.Library.SwordImpact)
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Tooltip("모든 사운드를 모은 중앙 라이브러리")]
    [SerializeField] private SoundLibrary _library;

    [Tooltip("동시 재생 가능한 효과음 수 (라운드로빈)")]
    [SerializeField] private int _voiceCount = 8;

    private AudioSource[] _voices;
    private int _next;

    /// <summary>중앙 사운드 라이브러리 (호출부가 사운드 참조에 사용).</summary>
    public SoundLibrary Library => _library;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _voices = new AudioSource[_voiceCount];
        for (int i = 0; i < _voiceCount; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;   // 2D
            _voices[i] = src;
        }
    }

    /// <summary>사운드 정의 재생 (랜덤 클립/피치, 지정 볼륨). null 이면 무시.</summary>
    public void PlaySound(SoundDefinition sound)
    {
        if (sound == null || !sound.HasClip) return;

        AudioSource src = _voices[_next];
        _next = (_next + 1) % _voices.Length;

        src.pitch = sound.GetPitch();
        src.PlayOneShot(sound.GetClip(), sound.Volume);
    }
}