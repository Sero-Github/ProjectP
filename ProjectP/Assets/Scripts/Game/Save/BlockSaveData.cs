using System;
using System.Collections.Generic;
using UnityEngine;

// 가지고 있는 블록 하나의 저장 정보. JsonUtility로 직렬화하므로 필드는 public이다.
// 블록 종류, 스탯, 스킬 등 enum 값은 순서가 바뀌어도 깨지지 않도록 이름으로 저장한다.
// 공통 정보는 From이 쓰고, 종류별 정보(core, stat, skill, targetPriority, activeSkill)는 각 블록 클래스가 쓴다.
[Serializable]
public class BlockSaveData
{
    // 블록 클래스(Stat, Skill, ActiveSkill). 이 필드가 없던 옛 저장 파일은 core로 판단한다.
    public string kind;
    // 블록 종류 번호. 0이면 병합 블록이며 pieces 모양으로 다시 조립한다.
    public int blockId;
    // 스탯 블록은 0, 스킬 블록은 속성 번호(1~7)이다.
    public int core;
    public int mergeCount;
    public string stat;
    // 옛 저장 파일의 스탯 수치. 새로 저장할 때는 쓰지 않고, 원본 점수가 없던 옛 파일을 변환할 때만 읽는다.
    public int statValue;
    public string skill;
    // 옛 저장 파일의 스킬 점수(칸 수의 합). 새로 저장할 때는 쓰지 않고, 원본 점수가 없던 옛 파일을 변환할 때만 읽는다.
    public int skillScore;
    // 스킬 블록의 타격 우선순위. 이 필드가 없던 옛 저장 파일은 가까운 적으로 읽는다.
    public string targetPriority;
    // 액티브 블록의 액티브 스킬.
    public string activeSkill;
    // 강화하지 않은 원본 블록 점수와 강. 최종 점수는 불러올 때 다시 계산한다.
    // 이 필드가 없던 옛 저장 파일은 0으로 읽히며, BlockManager가 옛 값으로 바꿔 읽는다.
    public int baseScore;
    public int enhanceLevel;
    // 코어 칸을 (0,0)으로 하는 각 조각의 칸 오프셋. y는 아래쪽이 +이다.
    public List<Vector2Int> pieces = new List<Vector2Int>();

    // 가방에 배치되어 있으면 true이며, cell은 코어 조각이 놓인 가방 칸이다. false면 창고에 있다.
    public bool inBackpack;
    public Vector2Int cell;

    public static BlockSaveData From(Block block, bool inBackpack, Vector2Int cell)
    {
        var data = new BlockSaveData
        {
            kind = block.Kind.ToString(),
            blockId = block.BlockId,
            mergeCount = block.MergeCount,
            baseScore = block.BaseScore,
            enhanceLevel = block.EnhanceLevel,
            pieces = new List<Vector2Int>(block.GetPieceOffsets()),
            inBackpack = inBackpack,
            cell = cell,
        };
        block.WriteKindData(data);
        return data;
    }

    // kind가 없는 옛 저장 파일에는 스탯 블록(core 0)과 스킬 블록(core 1~7)만 있었다.
    public BlockKind Kind => string.IsNullOrEmpty(kind)
        ? (core == 0 ? BlockKind.Stat : BlockKind.Skill)
        : ParseEnum(kind, BlockKind.Stat);
    public StatType StatType => ParseEnum(stat, StatType.None);
    public SkillType SkillType => ParseEnum(skill, SkillType.None);
    public TargetPriority TargetPriority => ParseEnum(targetPriority, TargetPriority.Nearest);
    public ActiveSkillType ActiveSkillType => ParseEnum(activeSkill, ActiveSkillType.None);

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
