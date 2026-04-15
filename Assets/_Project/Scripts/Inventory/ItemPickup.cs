using UnityEngine;

/// <summary>
/// 바닥에 떨어진 아이템. 플레이어가 접근하면 획득합니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    [Header("Item Info")]
    [SerializeField] private ItemData _itemData;
    [SerializeField] private int _amount = 1;

    [Header("Pickup Settings")]
    [SerializeField] private float _pickupDelay = 0.5f;
    [SerializeField] private float _pickupRange = 1.5f;
    [SerializeField] private float _floatSpeed = 1f;
    [SerializeField] private float _floatHeight = 0.2f;

    private float _spawnTime;
    private bool _isPickedUp = false;
    private Vector3 _basePosition;
    private bool _isGrounded = false;
    private Transform _playerTransform;
    private InventorySystem _playerInventory;

    /// <summary>드롭 시스템에서 호출하여 초기화합니다.</summary>
    public void Initialize(ItemData data, int amount)
    {
        _itemData = data;
        _amount = amount;
        _spawnTime = Time.time;
    }

    private void Start()
    {
        if (_spawnTime == 0f)
            _spawnTime = Time.time;

        // 플레이어 캐싱
        GameObject player = GameObject.FindGameObjectWithTag(Define.Tag.Player);
        if (player != null)
        {
            _playerTransform = player.transform;
            _playerInventory = player.GetComponent<InventorySystem>();
        }
    }

    private void Update()
    {
        // 착지 후 둥둥 떠다니는 연출
        if (_isGrounded)
        {
            float newY = _basePosition.y +
                Mathf.Sin(Time.time * _floatSpeed) * _floatHeight;
            transform.position = new Vector3(
                transform.position.x,
                newY,
                transform.position.z
            );

            // 천천히 회전
            transform.Rotate(Vector3.up, 30f * Time.deltaTime);

            // 거리 기반 획득 체크
            CheckPickup();
        }
    }

    private void CheckPickup()
    {
        if (_isPickedUp) return;
        if (_playerTransform == null || _playerInventory == null) return;
        if (Time.time - _spawnTime < _pickupDelay) return;

        float dist = Vector3.Distance(transform.position, _playerTransform.position);
        if (dist > _pickupRange) return;

        if (_playerInventory.AddItem(_itemData, _amount))
        {
            _isPickedUp = true;
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 바닥에 착지하면 Rigidbody를 비활성화하고 떠다니기 시작
        if (!_isGrounded && collision.gameObject.layer == Define.Layer.Ground)
        {
            _isGrounded = true;
            _basePosition = transform.position;

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
    }
}