using UnityEngine;

/// <summary>
/// 데미지 팝업 자동 생성 시스템.
/// 씬의 HitBox들의 OnHit 이벤트를 구독하여 자동으로 팝업을 띄웁니다.
/// 
/// [사용법]
/// HUD_Canvas 또는 별도 매니저 오브젝트에 부착합니다.
/// Damage Popup Prefab 연결 필수.
/// </summary>
public class DamagePopupSpawner : Singleton<DamagePopupSpawner>
{
    [Header("Prefab")]
    [SerializeField] private GameObject _popupPrefab;

    [Header("Spawn Offset")]
    [Tooltip("타격 지점에서의 Y 오프셋 (머리 위로)")]
    [SerializeField] private float _yOffset = 1.5f;

    /// <summary>
    /// 특정 위치에 데미지 팝업을 생성합니다.
    /// HitBox 등에서 호출.
    /// </summary>
    public void SpawnPopup(float damage, Vector3 worldPos, DamagePopup.DamageType type = DamagePopup.DamageType.Normal)
    {
        if (_popupPrefab == null) return;

        Vector3 spawnPos = worldPos + Vector3.up * _yOffset;
        GameObject popupObj = Instantiate(_popupPrefab, spawnPos, Quaternion.identity);

        var popup = popupObj.GetComponent<DamagePopup>();
        if (popup != null)
            popup.Initialize(damage, spawnPos, type);
        else
            Debug.LogError("[DamagePopupSpawner] Popup Prefab에 DamagePopup 컴포넌트가 없습니다.");
    }
}