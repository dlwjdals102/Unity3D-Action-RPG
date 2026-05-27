using System.Collections;
using UnityEngine;

/// <summary>
/// 적 피격 시 흰색 플래시 (타격감 - 맞은 것이 눈에 보이게).
/// EnemyHealth.OnDamaged 를 구독해, 모든 자식 Renderer 의 _BaseColor 를
/// 잠깐 흰색으로 바꿨다가 원래 색으로 복원한다.
/// 
/// 캐릭터가 여러 메시(Joints/Surface 등)로 나뉠 수 있어 자식의 모든 Renderer 를 처리.
/// 머티리얼은 renderer.material(인스턴스)을 사용 → 다른 적/원본 에셋에 영향 없음.
/// URP Lit 이라 색 프로퍼티는 _BaseColor.
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class EnemyHitFlash : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [Header("Flash")]
    [Tooltip("피격 시 번쩍일 색")]
    [SerializeField] private Color _flashColor = Color.white;

    [Tooltip("플래시 지속 시간(초)")]
    [SerializeField] private float _flashDuration = 0.1f;

    private EnemyHealth _health;
    private Renderer[] _renderers;
    private Color[] _originalColors;
    private Coroutine _routine;

    private void Awake()
    {
        _health = GetComponent<EnemyHealth>();

        // 자식의 모든 Renderer 수집 + 각 원본 _BaseColor 저장
        _renderers = GetComponentsInChildren<Renderer>();
        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            // material(인스턴스) 접근 → 그 적만의 복제본 (다른 적/에셋 영향 없음)
            if (_renderers[i].material.HasProperty(BaseColorId))
            {
                _originalColors[i] = _renderers[i].material.GetColor(BaseColorId);
            }
            else
            {
                _originalColors[i] = Color.white; // 폴백
            }
        }
    }

    private void OnEnable()
    {
        if (_health != null) _health.OnDamaged += Flash;
    }

    private void OnDisable()
    {
        if (_health != null) _health.OnDamaged -= Flash;
    }

    /// <summary>피격 시 호출 (OnDamaged 구독). 플래시 코루틴 시작/재시작.</summary>
    private void Flash()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // 모든 Renderer 흰색으로
        SetColor(_flashColor);

        yield return new WaitForSeconds(_flashDuration);

        // 원본 색 복원
        RestoreColors();
        _routine = null;
    }

    private void SetColor(Color color)
    {
        foreach (var r in _renderers)
        {
            if (r.material.HasProperty(BaseColorId))
                r.material.SetColor(BaseColorId, color);
        }
    }

    private void RestoreColors()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i].material.HasProperty(BaseColorId))
                _renderers[i].material.SetColor(BaseColorId, _originalColors[i]);
        }
    }
}