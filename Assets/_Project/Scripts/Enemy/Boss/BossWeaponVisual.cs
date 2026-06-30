using UnityEngine;

/// <summary>보스의 동작별 무기 표시. 상태가 필요한 무기만 켠다(검/활/방패).</summary>
public class BossWeaponVisual : MonoBehaviour
{
    [SerializeField] private GameObject _sword;
    [SerializeField] private GameObject _bow;
    [SerializeField] private GameObject _shield;

    private void Awake() => HideAll();

    public void ShowSword() { HideAll(); if (_sword != null) _sword.SetActive(true); }
    public void ShowBow() { HideAll(); if (_bow != null) _bow.SetActive(true); }
    public void ShowShield() { HideAll(); if (_shield != null) _shield.SetActive(true); }

    public void HideAll()
    {
        if (_sword != null) _sword.SetActive(false);
        if (_bow != null) _bow.SetActive(false);
        if (_shield != null) _shield.SetActive(false);
    }
}