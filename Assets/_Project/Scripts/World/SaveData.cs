using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 저장 데이터 구조 (JSON 직렬화용). SO 참조 대신 ID(문자열), 위치는 float 로.
/// SaveManager 가 이 구조를 JSON 으로 저장/불러온다.
/// 
/// [Serializable] + public 필드: Unity JsonUtility 직렬화 규칙.
/// </summary>
[Serializable]
public class SaveData
{
    // 영혼
    public int souls;

    // 인벤토리 (아이템 ID + 개수)
    public List<SavedItem> inventory = new List<SavedItem>();

    // 장비 (부위 + 아이템 ID)
    public List<SavedEquipment> equipment = new List<SavedEquipment>();

    // 체크포인트
    public bool hasCheckpoint;
    public float checkpointX, checkpointY, checkpointZ;
    public float checkpointRotY;  // 회전은 Y축만 (캐릭터 방향)

    // 떨어진 영혼 (회수 전 사망 드롭 - 껐다 켜도 유지)
    public bool hasSoulDrop;
    public float soulDropX, soulDropY, soulDropZ;
    public int soulDropAmount;
}

/// <summary>인벤토리 한 칸 저장 (아이템 ID + 개수).</summary>
[Serializable]
public class SavedItem
{
    public string itemId;
    public int count;
}

/// <summary>장비 한 부위 저장 (슬롯 + 아이템 ID).</summary>
[Serializable]
public class SavedEquipment
{
    public string slot;     // EquipmentSlot 을 문자열로
    public string itemId;
}