using System;
using UnityEngine;

// 스킬 블록. 공격 스킬 한 가지와 속성 한 가지, 타격 우선순위 하나를 가진다. 🧱 블록 문서 2장을 따른다.
// 스킬 점수는 따로 두지 않고 블록 점수(Score)를 쓴다(SkillManager가 같은 스킬·속성 블록의 점수를 더한다).
public class SkillBlock : Block
{
    // 드롭할 때 고를 스킬 종류와 타격 우선순위. 스킬 None은 제외한다.
    private static readonly SkillType[] SkillTypes = Array.FindAll((SkillType[])Enum.GetValues(typeof(SkillType)), type => type != SkillType.None);
    private static readonly TargetPriority[] TargetPriorities = (TargetPriority[])Enum.GetValues(typeof(TargetPriority));

    [SerializeField] private Element element;
    [SerializeField] private SkillType skill;
    // 드롭할 때 정해지며 플레이어는 바꿀 수 없다. ⚔️ 스킬 문서 4장을 따른다.
    [SerializeField] private TargetPriority targetPriority;

    public Element Element => element;
    public SkillType Skill => skill;
    public TargetPriority TargetPriority => targetPriority;

    public override BlockKind Kind => BlockKind.Skill;
    // 속성 색은 코어 색상 목록의 속성 번호(1~7)이다.
    public override int ColorIndex => (int)element;

    public static SkillType GetRandomSkillType()
    {
        return SkillTypes[UnityEngine.Random.Range(0, SkillTypes.Length)];
    }

    // 옵션별 드롭 확률이 정해지기 전까지는 같은 확률로 고른다(🗺️ 개발 계획 7장 결정 7).
    public static TargetPriority GetRandomTargetPriority()
    {
        return TargetPriorities[UnityEngine.Random.Range(0, TargetPriorities.Length)];
    }

    public void SetSkill(Element blockElement, SkillType skillType, TargetPriority priority)
    {
        element = blockElement;
        skill = skillType;
        targetPriority = priority;
    }

    // 지금은 속성과 스킬이 모두 같아야 병합한다. 기획(속성만 같으면 병합)은 M1-7에서 반영한다.
    // 타격 우선순위는 병합 조건이 아니며, 결과 블록은 병합할 블록의 우선순위를 따른다.
    public override MergeMismatch GetMergeMismatch(Block material)
    {
        if (material is not SkillBlock other)
        {
            return MergeMismatch.DifferentKind;
        }

        if (other.element != element)
        {
            return MergeMismatch.DifferentElement;
        }

        return other.skill == skill ? MergeMismatch.None : MergeMismatch.DifferentSkill;
    }

    public override void CopyKindDataFrom(Block source)
    {
        if (source is SkillBlock other)
        {
            SetSkill(other.element, other.skill, other.targetPriority);
        }
    }

    // 옛 저장 파일과 맞추기 위해 속성은 core 번호로 쓴다.
    public override void WriteKindData(BlockSaveData data)
    {
        data.core = (int)element;
        data.skill = skill.ToString();
        data.targetPriority = targetPriority.ToString();
    }

    public override void ReadKindData(BlockSaveData data)
    {
        SetSkill((Element)data.core, data.SkillType, data.TargetPriority);
    }
}
