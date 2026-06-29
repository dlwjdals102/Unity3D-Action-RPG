using UnityEngine;

/// <summary>플레이어가 일정 거리 안에 있을 때만 대상(라벨 등)을 표시.</summary>
public class DistanceVisibility : MonoBehaviour
{
    [Tooltip("이 거리 안이면 표시")]
    [SerializeField] private float _showDistance = 15f;

    [Tooltip("켜고 끌 대상 (비우면 자기 자신)")]
    [SerializeField] private GameObject _target;

    private Transform _player;

    private void Awake()
    {
        if (_target == null) _target = gameObject;
    }

    private void Start()
    {
        // 플레이어 한 번만 캐싱 (매 프레임 Find 금지)
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _player = player.transform;
    }

    private void Update()
    {
        if (_player == null) return;

        float sqr = (_player.position - transform.position).sqrMagnitude;
        bool visible = sqr <= _showDistance * _showDistance;

        if (_target.activeSelf != visible)
            _target.SetActive(visible);   // 상태 바뀔 때만 (불필요한 SetActive 방지)
    }
}