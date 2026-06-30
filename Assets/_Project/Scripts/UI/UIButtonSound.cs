using UnityEngine;
using UnityEngine.UI;

/// <summary>이 버튼 클릭 시 UI 클릭 효과음 재생. Button 오브젝트에 부착.</summary>
[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(PlayClick);
    }

    private void PlayClick()
    {
        var am = AudioManager.Instance;
        am?.PlaySound(am.Library.UIClick);
    }
}