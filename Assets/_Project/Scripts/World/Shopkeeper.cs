using UnityEngine;

/// <summary>
/// 상점 오브젝트(상인). 플레이어가 트리거 범위에서 F(상호작용)하면 상점 UI를 연다.
/// 화톳불(Bonfire)과 같은 상호작용 패턴.
/// 범위를 벗어나거나 다시 F를 누르면 닫는다.
/// 
/// 필요: Trigger Collider, 플레이어 감지.
/// </summary>
public class Shopkeeper : MonoBehaviour
{
    [Tooltip("이 상인이 여는 상점 UI")]
    [SerializeField] private ShopUI _shopUI;

    [Tooltip("이 상인의 판매 목록")]
    [SerializeField] private ShopData _shopData;

    private PlayerController _playerInRange;
    private bool _isOpen;

    private void OnTriggerEnter(Collider other)
    {
        var controller = other.GetComponentInParent<PlayerController>();
        if (controller != null)
        {
            _playerInRange = controller;
            Debug.Log("[상점] 상호작용 가능 (F)");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var controller = other.GetComponentInParent<PlayerController>();
        if (controller != null && controller == _playerInRange)
        {
            _playerInRange = null;

            // 범위 벗어나면 상점 닫기
            if (_isOpen) CloseShop();
        }
    }

    private void Update()
    {
        if (_playerInRange == null) return;

        if (_playerInRange.InteractRequested)
        {
            // F 토글: 열려있으면 닫고, 닫혀있으면 열기
            if (_isOpen) CloseShop();
            else OpenShop();
        }
    }

    private void OpenShop()
    {
        if (_shopUI == null) return;
        _shopUI.Open(_shopData);
        _isOpen = true;
    }

    private void CloseShop()
    {
        if (_shopUI == null) return;
        _shopUI.Close();
        _isOpen = false;
    }
}