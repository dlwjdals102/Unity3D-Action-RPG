using UnityEngine;

/// <summary>
/// 체크포인트(화톳불) 위치를 보관하는 싱글톤. 소울 루프의 중심.
/// 화톳불에서 휴식하면 그 위치를 체크포인트로 등록하고, 사망 시 리스폰 시스템이 이 위치를 조회한다.
/// 
/// 현재는 런타임 위치만 보관 (게임 세션 중 기억). 파일 저장(JSON)은 [3순위] 세이브/로드에서.
/// </summary>
public class BonfireManager : MonoBehaviour
{
    public static BonfireManager Instance { get; private set; }

    private Vector3 _checkpointPosition;
    private Quaternion _checkpointRotation;
    private bool _hasCheckpoint;

    /// <summary>체크포인트가 한 번이라도 등록되었는지 (게임 시작 직후엔 false).</summary>
    public bool HasCheckpoint => _hasCheckpoint;

    /// <summary>현재 체크포인트 위치 (리스폰 지점).</summary>
    public Vector3 CheckpointPosition => _checkpointPosition;

    /// <summary>현재 체크포인트에서 바라볼 방향.</summary>
    public Quaternion CheckpointRotation => _checkpointRotation;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>화톳불 휴식 시 호출. 해당 화톳불 위치를 리스폰 지점으로 등록.</summary>
    public void SetCheckpoint(Vector3 position, Quaternion rotation)
    {
        _checkpointPosition = position;
        _checkpointRotation = rotation;
        _hasCheckpoint = true;

        Debug.Log($"[Bonfire] 체크포인트 등록: {position}");
    }
}