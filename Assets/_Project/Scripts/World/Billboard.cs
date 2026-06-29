using UnityEngine;

/// <summary>월드 텍스트/스프라이트가 항상 카메라를 향하게 (빌보드).</summary>
public class Billboard : MonoBehaviour
{
    private Camera _cam;

    private void Start() => _cam = Camera.main;

    private void LateUpdate()
    {
        if (_cam == null) return;
        // 카메라의 forward 방향으로 정렬 (텍스트가 항상 정면)
        transform.forward = _cam.transform.forward;
    }
}