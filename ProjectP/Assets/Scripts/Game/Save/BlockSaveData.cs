using System;
using System.Collections.Generic;
using UnityEngine;

// 가지고 있는 블록 하나의 저장 정보. JsonUtility로 직렬화하므로 필드는 public이다.
// 스탯, 스킬 종류는 enum 순서가 바뀌어도 깨지지 않도록 이름으로 저장한다.
[Serializable]
public class BlockSaveData
{
    // 블록 종류 번호. 0이면 병합 블록이며 pieces 모양으로 다시 조립한다.
    public int blockId;
    public int core;
    public int grade = 1;
    public int mergeCount;
    public string stat;
    public int statValue;
    public string skill;
    public int skillScore;
    // 코어 칸을 (0,0)으로 하는 각 조각의 칸 오프셋. y는 아래쪽이 +이다.
    public List<Vector2Int> pieces = new List<Vector2Int>();

    // 가방에 배치되어 있으면 true이며, cell은 코어 조각이 놓인 가방 칸이다. false면 창고에 있다.
    public bool inBackpack;
    public Vector2Int cell;

    public static BlockSaveData From(Block block, bool inBackpack, Vector2Int cell)
    {
        return new BlockSaveData
        {
            blockId = block.BlockId,
            core = block.Core,
            grade = block.Grade,
            mergeCount = block.MergeCount,
            stat = block.Stat.Type.ToString(),
            statValue = block.Stat.Value,
            skill = block.Skill.ToString(),
            skillScore = block.SkillScore,
            pieces = new List<Vector2Int>(block.GetPieceOffsets()),
            inBackpack = inBackpack,
            cell = cell,
        };
    }

    public StatType StatType => ParseEnum(stat, StatType.None);
    public SkillType SkillType => ParseEnum(skill, SkillType.None);

    private static T ParseEnum<T>(string value, T fallback) where T : struct, Enum
    {
        if (string.IsNullOrEmpty(value))
        {
            return fallback;
        }

        if (Enum.TryParse(value, out T result) && Enum.IsDefined(typeof(T), result))
        {
            return result;
        }

        Debug.LogWarning($"저장된 {typeof(T).Name} '{value}'를 찾지 못해 {fallback}(으)로 불러옵니다.");
        return fallback;
    }
}
