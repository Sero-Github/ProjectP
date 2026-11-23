using UnityEngine;

// 액티브 블록. 스탯, 공격 스킬, 속성을 가지지 않고 액티브 스킬 하나만 가진다. ✨ 액티브 스킬 문서 2장을 따른다.
// 기획이 초안이라 지금은 종류와 저장만 있다. 4·5칸 모양, 보스 처치 보상(드롭), 발동은 액티브 스킬 작업 때 넣는다.
public class ActiveSkillBlock : Block
{
    [SerializeField] private ActiveSkillType activeSkill;

    public ActiveSkillType ActiveSkill => activeSkill;

    public override BlockKind Kind => BlockKind.ActiveSkill;
    // 액티브 블록 색은 기획에서 정해지지 않아 스탯 블록과 같은 0번 색을 쓴다.
    public override int ColorIndex => 0;

    public void SetActiveSkill(ActiveSkillType activeSkillType)
    {
        activeSkill = activeSkillType;
    }

    // 병합 가능 여부가 기획에서 정해지지 않아(✨ 액티브 스킬 2장 작성 예정) 지금은 병합하지 않는다.
    public override MergeMismatch GetMergeMismatch(Block material)
    {
        return MergeMismatch.NotMergeable;
    }

    public override void CopyKindDataFrom(Block source)
    {
        if (source is ActiveSkillBlock other)
        {
            activeSkill = other.activeSkill;
        }
    }

    public override void WriteKindData(BlockSaveData data)
    {
        data.activeSkill = activeSkill.ToString();
    }

    public override void ReadKindData(BlockSaveData data)
    {
        activeSkill = data.ActiveSkillType;
    }
}
