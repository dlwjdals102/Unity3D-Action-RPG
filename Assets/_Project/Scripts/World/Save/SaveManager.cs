using System.IO;
using UnityEngine;

/// <summary>
/// 세이브 데이터의 JSON 파일 저장/불러오기 담당 (싱글톤).
/// 파일 입출력 + JSON 변환만 책임지고, 데이터 수집/적용은 호출하는 쪽이 한다.
/// 
/// 저장 위치: Application.persistentDataPath (플랫폼별 안전한 저장 폴더).
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>SaveData 를 JSON 파일로 저장.</summary>
    public void Save(SaveData data)
    {
        if (data == null) return;

        try
        {
            string json = JsonUtility.ToJson(data, true);  // true = 보기 좋게(들여쓰기)
            File.WriteAllText(SavePath, json);
            Debug.Log($"[Save] 저장 완료: {SavePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Save] 저장 실패: {e.Message}");
        }
    }

    /// <summary>JSON 파일에서 SaveData 불러오기 (없으면 null).</summary>
    public SaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[Save] 저장 파일 없음 (새 게임)");
            return null;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("[Save] 불러오기 완료");
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Save] 불러오기 실패: {e.Message}");
            return null;
        }
    }

    /// <summary>저장 파일이 있는지.</summary>
    public bool HasSave()
    {
        return File.Exists(SavePath);
    }

    /// <summary>저장 파일 삭제 (새 게임 등).</summary>
    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("[Save] 저장 파일 삭제");
        }
    }
}